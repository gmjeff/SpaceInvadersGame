using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SpaceInvaders.Models;

namespace SpaceInvaders;

public enum GameState
{
    Start,
    Playing,
    GameOver,
    Victory
}

public class GameEngine
{
    private readonly Canvas _canvas;
    private readonly DispatcherTimer _gameTimer;
    private readonly Random _random = new();
    private readonly SoundManager _soundManager = new();

    private Player? _player;
    private readonly List<Alien> _aliens = new();
    private readonly List<Bullet> _bullets = new();
    private readonly List<Shield> _shields = new();
    private UFO? _ufo;

    private double _alienDirection = 1;
    private double _alienSpeed = 1.0;
    private int _alienMoveCounter = 0;
    private const int AlienMoveInterval = 30;

    private int _ufoSpawnCounter = 0;
    private int _ufoSpawnInterval;

    private const int ExtraLifeThreshold = 1500;
    private bool _extraLifeAwarded = false;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Wave { get; private set; }
    public GameState State { get; private set; } = GameState.Start;

    public event Action? OnScoreChanged;
    public event Action? OnLivesChanged;
    public event Action? OnWaveChanged;
    public event Action<GameState>? OnGameStateChanged;

    public GameEngine(Canvas canvas)
    {
        _canvas = canvas;
        _gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _gameTimer.Tick += GameLoop;
        ResetUfoSpawnInterval();
    }

    private void ResetUfoSpawnInterval()
    {
        // UFO spawns every 20-30 seconds (at 60fps)
        _ufoSpawnInterval = _random.Next(1200, 1800);
        _ufoSpawnCounter = 0;
    }

    public void StartGame()
    {
        ResetGame();
        State = GameState.Playing;
        OnGameStateChanged?.Invoke(State);
        _gameTimer.Start();
    }

    public void StopGame()
    {
        _gameTimer.Stop();
    }

    private void ResetGame()
    {
        _canvas.Children.Clear();
        _aliens.Clear();
        _bullets.Clear();
        _shields.Clear();
        _ufo = null;

        Score = 0;
        Lives = 3;
        Wave = 1;
        _extraLifeAwarded = false;
        _alienSpeed = 1.0;
        _alienDirection = 1;
        _alienMoveCounter = 0;
        ResetUfoSpawnInterval();
        _soundManager.StopUfoSiren();
        _soundManager.ResetHeartbeat();

        OnScoreChanged?.Invoke();
        OnLivesChanged?.Invoke();
        OnWaveChanged?.Invoke();

        double canvasWidth = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : 800;
        double canvasHeight = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : 600;

        _player = new Player(canvasWidth, canvasHeight);
        _player.AddToCanvas(_canvas);

        CreateAliens();
        CreateShields(canvasWidth, canvasHeight);
    }

    private void CreateAliens()
    {
        const int rows = 5;
        const int cols = 11;
        const double startX = 80;
        const double baseStartY = 80;
        const double spacingX = 50;
        const double spacingY = 40;

        // Each wave, aliens start a bit lower (max 5 rows lower)
        double waveOffset = Math.Min((Wave - 1) * 20, 100);
        double startY = baseStartY + waveOffset;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                double x = startX + col * spacingX;
                double y = startY + row * spacingY;
                var alien = new Alien(x, y, row, col);
                alien.AddToCanvas(_canvas);
                _aliens.Add(alien);
            }
        }
    }

    private void CreateShields(double canvasWidth, double canvasHeight)
    {
        const int numShields = 4;
        double shieldY = canvasHeight - 120;
        double totalWidth = numShields * Shield.ShieldWidth;
        double spacing = (canvasWidth - totalWidth) / (numShields + 1);

        for (int i = 0; i < numShields; i++)
        {
            double x = spacing + i * (Shield.ShieldWidth + spacing);
            var shield = new Shield(x, shieldY);
            shield.AddToCanvas(_canvas);
            _shields.Add(shield);
        }
    }

    private void GameLoop(object? sender, EventArgs e)
    {
        if (State != GameState.Playing) return;

        UpdatePlayer();
        UpdateBullets();
        UpdateAliens();
        UpdateUFO();
        CheckCollisions();
        AlienShooting();
        DrawAll();
    }

    private void UpdatePlayer()
    {
        if (_player == null) return;

        _player.Update();

        // Only allow 1 player bullet at a time (Atari-authentic)
        bool hasPlayerBullet = _bullets.Any(b => b.Type == BulletType.Player);

        if (_player.IsShooting && !hasPlayerBullet)
        {
            var bullet = new Bullet(
                _player.X + Player.PlayerWidth / 2 - Bullet.BulletWidth / 2,
                _player.Y - Bullet.BulletHeight,
                BulletType.Player,
                _canvas.ActualHeight);
            bullet.AddToCanvas(_canvas);
            _bullets.Add(bullet);
            _soundManager.PlayShoot();
        }
    }

    private void UpdateBullets()
    {
        foreach (var bullet in _bullets.ToList())
        {
            bullet.Update();
            if (!bullet.IsActive)
            {
                bullet.RemoveFromCanvas(_canvas);
                _bullets.Remove(bullet);
            }
        }
    }

    private void UpdateAliens()
    {
        _alienMoveCounter++;
        if (_alienMoveCounter < AlienMoveInterval / _alienSpeed) return;
        _alienMoveCounter = 0;

        bool shouldDrop = false;
        double canvasWidth = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : 800;

        foreach (var alien in _aliens.Where(a => a.IsActive))
        {
            if ((_alienDirection > 0 && alien.X + Alien.AlienWidth >= canvasWidth - 20) ||
                (_alienDirection < 0 && alien.X <= 20))
            {
                shouldDrop = true;
                break;
            }
        }

        // Toggle animation for all aliens and play heartbeat
        _soundManager.PlayHeartbeat();
        foreach (var alien in _aliens.Where(a => a.IsActive))
        {
            alien.ToggleAnimation();
        }

        if (shouldDrop)
        {
            foreach (var alien in _aliens.Where(a => a.IsActive))
            {
                alien.Move(0, 15);
            }
            _alienDirection *= -1;
            _alienSpeed = Math.Min(_alienSpeed + 0.1, 3.0);
        }
        else
        {
            foreach (var alien in _aliens.Where(a => a.IsActive))
            {
                alien.Move(_alienDirection * 8, 0);
            }
        }
    }

    private void UpdateUFO()
    {
        double canvasWidth = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : 800;

        if (_ufo == null)
        {
            _ufoSpawnCounter++;
            if (_ufoSpawnCounter >= _ufoSpawnInterval)
            {
                // Spawn UFO from left or right
                int direction = _random.Next(2) == 0 ? 1 : -1;
                _ufo = new UFO(canvasWidth, direction);
                _ufo.AddToCanvas(_canvas);
                _soundManager.StartUfoSiren();
                ResetUfoSpawnInterval();
            }
        }
        else
        {
            _ufo.Update();
            _ufo.Draw(_canvas);

            if (_ufo.IsOffScreen(canvasWidth))
            {
                _ufo.RemoveFromCanvas(_canvas);
                _ufo = null;
                _soundManager.StopUfoSiren();
            }
        }
    }

    private void AlienShooting()
    {
        var activeAliens = _aliens.Where(a => a.IsActive).ToList();
        if (activeAliens.Count == 0) return;

        // Get bottom-most alien in each column (like original game)
        var bottomAliens = activeAliens
            .GroupBy(a => a.Column)
            .Select(g => g.OrderByDescending(a => a.Row).First())
            .ToList();

        if (_random.Next(100) < 2 && bottomAliens.Count > 0)
        {
            var shooter = bottomAliens[_random.Next(bottomAliens.Count)];
            var bullet = new Bullet(
                shooter.X + Alien.AlienWidth / 2 - Bullet.BulletWidth / 2,
                shooter.Y + Alien.AlienHeight,
                BulletType.Alien,
                _canvas.ActualHeight);
            bullet.AddToCanvas(_canvas);
            _bullets.Add(bullet);
        }
    }

    private void CheckCollisions()
    {
        double canvasHeight = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : 600;

        foreach (var bullet in _bullets.ToList())
        {
            if (!bullet.IsActive) continue;

            var bulletBounds = bullet.GetBounds();

            // Check shield collisions
            foreach (var shield in _shields)
            {
                if (shield.CheckCollision(bulletBounds, _canvas))
                {
                    bullet.IsActive = false;
                    bullet.RemoveFromCanvas(_canvas);
                    _bullets.Remove(bullet);
                    break;
                }
            }

            if (!bullet.IsActive) continue;

            if (bullet.Type == BulletType.Player)
            {
                // Check UFO collision
                if (_ufo != null && bullet.CollidesWith(_ufo))
                {
                    bullet.IsActive = false;
                    bullet.RemoveFromCanvas(_canvas);
                    _bullets.Remove(bullet);

                    _soundManager.StopUfoSiren();
                    _soundManager.PlayUfoExplosion();
                    AddScore(_ufo.Points);

                    _ufo.RemoveFromCanvas(_canvas);
                    _ufo = null;
                    continue;
                }

                // Check alien collisions
                foreach (var alien in _aliens.Where(a => a.IsActive))
                {
                    if (bullet.CollidesWith(alien))
                    {
                        bullet.IsActive = false;
                        bullet.RemoveFromCanvas(_canvas);
                        _bullets.Remove(bullet);

                        alien.IsActive = false;
                        alien.RemoveFromCanvas(_canvas);

                        _soundManager.PlayExplosion();
                        AddScore(alien.Points);

                        _alienSpeed = Math.Min(_alienSpeed + 0.02, 3.0);

                        if (_aliens.All(a => !a.IsActive))
                        {
                            Victory();
                        }
                        break;
                    }
                }
            }
            else if (_player != null && bullet.CollidesWith(_player))
            {
                bullet.IsActive = false;
                bullet.RemoveFromCanvas(_canvas);
                _bullets.Remove(bullet);

                _soundManager.PlayPlayerDeath();
                Lives--;
                OnLivesChanged?.Invoke();

                if (Lives <= 0)
                {
                    GameOver();
                }
            }
        }

        // Check if aliens reached bottom or shields
        if (_player != null)
        {
            foreach (var alien in _aliens.Where(a => a.IsActive))
            {
                if (alien.Y + Alien.AlienHeight >= canvasHeight - 60)
                {
                    GameOver();
                    return;
                }
            }
        }
    }

    private void DrawAll()
    {
        _player?.Draw(_canvas);

        if (_ufo != null)
        {
            _ufo.Draw(_canvas);
        }

        foreach (var alien in _aliens.Where(a => a.IsActive))
        {
            alien.Draw(_canvas);
        }
        foreach (var bullet in _bullets)
        {
            bullet.Draw(_canvas);
        }
    }

    private void AddScore(int points)
    {
        int oldScore = Score;
        Score += points;
        OnScoreChanged?.Invoke();

        // Award extra life at threshold (only once per game)
        if (!_extraLifeAwarded && oldScore < ExtraLifeThreshold && Score >= ExtraLifeThreshold)
        {
            _extraLifeAwarded = true;
            Lives++;
            OnLivesChanged?.Invoke();
            // Could add a sound here for extra life
        }
    }

    private void GameOver()
    {
        State = GameState.GameOver;
        _gameTimer.Stop();
        OnGameStateChanged?.Invoke(State);
    }

    private void Victory()
    {
        // Start next wave instead of ending game
        StartNextWave();
    }

    private void StartNextWave()
    {
        Wave++;
        OnWaveChanged?.Invoke();

        // Clear bullets and UFO
        foreach (var bullet in _bullets)
        {
            bullet.RemoveFromCanvas(_canvas);
        }
        _bullets.Clear();

        if (_ufo != null)
        {
            _ufo.RemoveFromCanvas(_canvas);
            _ufo = null;
            _soundManager.StopUfoSiren();
        }

        // Clear old aliens
        foreach (var alien in _aliens)
        {
            alien.RemoveFromCanvas(_canvas);
        }
        _aliens.Clear();

        // Reset alien movement but keep increasing base speed
        _alienDirection = 1;
        _alienMoveCounter = 0;
        // Base speed increases with each wave
        _alienSpeed = 1.0 + (Wave - 1) * 0.2;
        _soundManager.ResetHeartbeat();
        ResetUfoSpawnInterval();

        // Create new aliens (they'll start lower based on wave)
        CreateAliens();
    }

    public void SetPlayerMovement(bool left, bool right)
    {
        if (_player != null)
        {
            _player.MovingLeft = left;
            _player.MovingRight = right;
        }
    }

    public void SetPlayerShooting(bool shooting)
    {
        if (_player != null)
        {
            _player.IsShooting = shooting;
        }
    }
}
