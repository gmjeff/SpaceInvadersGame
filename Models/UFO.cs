using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SpaceInvaders.Models;

public class UFO : GameObject
{
    public const double UFOWidth = 48;
    public const double UFOHeight = 20;
    public const double Speed = 2.5;

    public int Points { get; }
    public int Direction { get; } // 1 = right, -1 = left

    private static readonly int[] PointValues = { 50, 100, 150, 200, 300 };
    private static readonly Random Random = new();

    public UFO(double canvasWidth, int direction)
        : base(direction > 0 ? -UFOWidth : canvasWidth, 25, UFOWidth, UFOHeight)
    {
        Direction = direction;
        Points = PointValues[Random.Next(PointValues.Length)];

        // Classic UFO/flying saucer shape
        var geometry = new GeometryGroup();

        // Main dome (top)
        geometry.Children.Add(new EllipseGeometry(new Point(24, 8), 12, 8));

        // Body (middle ellipse)
        geometry.Children.Add(new EllipseGeometry(new Point(24, 12), 24, 6));

        // Bottom details
        geometry.Children.Add(new RectangleGeometry(new Rect(8, 14, 8, 4)));
        geometry.Children.Add(new RectangleGeometry(new Rect(20, 14, 8, 4)));
        geometry.Children.Add(new RectangleGeometry(new Rect(32, 14, 8, 4)));

        Visual = new Path
        {
            Fill = Brushes.Red,
            Data = geometry
        };
    }

    public override void Update()
    {
        X += Speed * Direction;
    }

    public bool IsOffScreen(double canvasWidth)
    {
        return (Direction > 0 && X > canvasWidth) || (Direction < 0 && X < -UFOWidth);
    }
}
