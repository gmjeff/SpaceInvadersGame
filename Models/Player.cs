using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SpaceInvaders.Models;

public class Player : GameObject
{
    public const double Speed = 5.0;
    public const double PlayerWidth = 52;
    public const double PlayerHeight = 32;

    public bool MovingLeft { get; set; }
    public bool MovingRight { get; set; }
    public bool IsShooting { get; set; }

    private readonly double _minX;
    private readonly double _maxX;

    public Player(double canvasWidth, double canvasHeight)
        : base((canvasWidth - PlayerWidth) / 2, canvasHeight - PlayerHeight - 20, PlayerWidth, PlayerHeight)
    {
        _minX = 10;
        _maxX = canvasWidth - PlayerWidth - 10;

        // Classic cannon shape
        var geometry = new GeometryGroup();

        // Base of cannon (wide rectangle)
        geometry.Children.Add(new RectangleGeometry(new Rect(0, 20, 52, 12)));

        // Middle section
        geometry.Children.Add(new RectangleGeometry(new Rect(8, 12, 36, 8)));

        // Upper section
        geometry.Children.Add(new RectangleGeometry(new Rect(16, 6, 20, 6)));

        // Cannon barrel (top)
        geometry.Children.Add(new RectangleGeometry(new Rect(23, 0, 6, 6)));

        Visual = new Path
        {
            Fill = Brushes.Lime,
            Data = geometry
        };
    }

    public override void Update()
    {
        if (MovingLeft && X > _minX)
        {
            X -= Speed;
        }
        if (MovingRight && X < _maxX)
        {
            X += Speed;
        }
    }

    public void Reset(double canvasWidth, double canvasHeight)
    {
        X = (canvasWidth - PlayerWidth) / 2;
        Y = canvasHeight - PlayerHeight - 20;
        IsActive = true;
    }
}
