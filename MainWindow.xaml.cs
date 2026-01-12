using System.Windows;
using System.Windows.Input;

namespace SpaceInvaders;

public partial class MainWindow : Window
{
    private readonly GameEngine _gameEngine;
    private bool _leftPressed;
    private bool _rightPressed;

    public MainWindow()
    {
        InitializeComponent();

        _gameEngine = new GameEngine(GameCanvas);
        _gameEngine.OnScoreChanged += UpdateScore;
        _gameEngine.OnLivesChanged += UpdateLives;
        _gameEngine.OnWaveChanged += UpdateWave;
        _gameEngine.OnGameStateChanged += HandleGameStateChanged;

        Loaded += (s, e) => Focus();
    }

    private void UpdateScore()
    {
        ScoreText.Text = $"SCORE: {_gameEngine.Score}";
    }

    private void UpdateLives()
    {
        LivesText.Text = $"LIVES: {_gameEngine.Lives}";
    }

    private void UpdateWave()
    {
        WaveText.Text = $"WAVE: {_gameEngine.Wave}";
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                OverlayPanel.Visibility = Visibility.Collapsed;
                break;
            case GameState.GameOver:
                OverlayPanel.Visibility = Visibility.Visible;
                TitleText.Text = "GAME OVER";
                MessageText.Text = $"Final Score: {_gameEngine.Score}\nPress ENTER to Restart";
                break;
            case GameState.Victory:
                OverlayPanel.Visibility = Visibility.Visible;
                TitleText.Text = "VICTORY!";
                MessageText.Text = $"Final Score: {_gameEngine.Score}\nPress ENTER to Play Again";
                break;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                _leftPressed = true;
                _gameEngine.SetPlayerMovement(_leftPressed, _rightPressed);
                break;
            case Key.Right:
            case Key.D:
                _rightPressed = true;
                _gameEngine.SetPlayerMovement(_leftPressed, _rightPressed);
                break;
            case Key.Space:
                _gameEngine.SetPlayerShooting(true);
                break;
            case Key.Enter:
                if (_gameEngine.State != GameState.Playing)
                {
                    _gameEngine.StartGame();
                }
                break;
        }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                _leftPressed = false;
                _gameEngine.SetPlayerMovement(_leftPressed, _rightPressed);
                break;
            case Key.Right:
            case Key.D:
                _rightPressed = false;
                _gameEngine.SetPlayerMovement(_leftPressed, _rightPressed);
                break;
            case Key.Space:
                _gameEngine.SetPlayerShooting(false);
                break;
        }
    }
}
