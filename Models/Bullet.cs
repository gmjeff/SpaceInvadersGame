using System.Windows.Media;
using System.Windows.Shapes;

namespace SpaceInvaders.Models;

public enum BulletType
{
    Player,
    Alien
}

public class Bullet : GameObject
{
    public const double BulletWidth = 4;
    public const double BulletHeight = 12;
    public const double PlayerBulletSpeed = 8.0;
    public const double AlienBulletSpeed = 4.0;

    public BulletType Type { get; }
    private readonly double _speed;
    private readonly double _canvasHeight;

    public Bullet(double x, double y, BulletType type, double canvasHeight)
        : base(x, y, BulletWidth, BulletHeight)
    {
        Type = type;
        _canvasHeight = canvasHeight;
        _speed = type == BulletType.Player ? -PlayerBulletSpeed : AlienBulletSpeed;

        Visual = new Rectangle
        {
            Width = BulletWidth,
            Height = BulletHeight,
            Fill = type == BulletType.Player ? Brushes.Yellow : Brushes.Red
        };
    }

    public override void Update()
    {
        Y += _speed;

        if (Y < -BulletHeight || Y > _canvasHeight)
        {
            IsActive = false;
        }
    }
}
