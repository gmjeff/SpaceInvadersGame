using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SpaceInvaders.Models;

public enum AlienType
{
    Squid,   // Top row - 30 pts
    Crab,    // Middle rows - 20 pts
    Octopus  // Bottom rows - 10 pts
}

public class Alien : GameObject
{
    public const double AlienWidth = 36;
    public const double AlienHeight = 24;

    public int Row { get; }
    public int Column { get; }
    public int Points { get; }
    public AlienType Type { get; }

    private bool _animationFrame;
    private readonly Path _path;

    public Alien(double x, double y, int row, int column)
        : base(x, y, AlienWidth, AlienHeight)
    {
        Row = row;
        Column = column;

        Type = row switch
        {
            0 => AlienType.Squid,
            1 or 2 => AlienType.Crab,
            _ => AlienType.Octopus
        };

        Points = Type switch
        {
            AlienType.Squid => 30,
            AlienType.Crab => 20,
            AlienType.Octopus => 10,
            _ => 10
        };

        Brush color = Type switch
        {
            AlienType.Squid => Brushes.White,
            AlienType.Crab => Brushes.Cyan,
            AlienType.Octopus => Brushes.Lime,
            _ => Brushes.White
        };

        _path = new Path
        {
            Fill = color,
            Data = GetGeometry(false)
        };
        Visual = _path;
    }

    private Geometry GetGeometry(bool frame2)
    {
        return Type switch
        {
            AlienType.Squid => GetSquidGeometry(frame2),
            AlienType.Crab => GetCrabGeometry(frame2),
            AlienType.Octopus => GetOctopusGeometry(frame2),
            _ => GetSquidGeometry(frame2)
        };
    }

    private static Geometry GetSquidGeometry(bool frame2)
    {
        // Classic squid invader shape (top row)
        // 8x8 pixel grid scaled up
        var geometry = new GeometryGroup();
        int[,] pixels = frame2 ? new int[,]
        {
            {0,0,0,1,1,0,0,0},
            {0,0,1,1,1,1,0,0},
            {0,1,1,1,1,1,1,0},
            {1,1,0,1,1,0,1,1},
            {1,1,1,1,1,1,1,1},
            {0,0,1,0,0,1,0,0},
            {0,1,0,1,1,0,1,0},
            {1,0,1,0,0,1,0,1}
        } : new int[,]
        {
            {0,0,0,1,1,0,0,0},
            {0,0,1,1,1,1,0,0},
            {0,1,1,1,1,1,1,0},
            {1,1,0,1,1,0,1,1},
            {1,1,1,1,1,1,1,1},
            {0,1,0,1,1,0,1,0},
            {1,0,0,0,0,0,0,1},
            {0,1,0,0,0,0,1,0}
        };
        AddPixels(geometry, pixels, 4.5);
        return geometry;
    }

    private static Geometry GetCrabGeometry(bool frame2)
    {
        // Classic crab invader shape (middle rows)
        var geometry = new GeometryGroup();
        int[,] pixels = frame2 ? new int[,]
        {
            {0,0,1,0,0,0,0,0,1,0,0},
            {1,0,0,1,0,0,0,1,0,0,1},
            {1,0,1,1,1,1,1,1,1,0,1},
            {1,1,1,0,1,1,1,0,1,1,1},
            {1,1,1,1,1,1,1,1,1,1,1},
            {0,1,1,1,1,1,1,1,1,1,0},
            {0,0,1,0,0,0,0,0,1,0,0},
            {0,1,0,0,0,0,0,0,0,1,0}
        } : new int[,]
        {
            {0,0,1,0,0,0,0,0,1,0,0},
            {0,0,0,1,0,0,0,1,0,0,0},
            {0,0,1,1,1,1,1,1,1,0,0},
            {0,1,1,0,1,1,1,0,1,1,0},
            {1,1,1,1,1,1,1,1,1,1,1},
            {1,0,1,1,1,1,1,1,1,0,1},
            {1,0,1,0,0,0,0,0,1,0,1},
            {0,0,0,1,1,0,1,1,0,0,0}
        };
        AddPixels(geometry, pixels, 3.3);
        return geometry;
    }

    private static Geometry GetOctopusGeometry(bool frame2)
    {
        // Classic octopus invader shape (bottom rows)
        var geometry = new GeometryGroup();
        int[,] pixels = frame2 ? new int[,]
        {
            {0,0,0,0,1,1,1,1,0,0,0,0},
            {0,1,1,1,1,1,1,1,1,1,1,0},
            {1,1,1,1,1,1,1,1,1,1,1,1},
            {1,1,1,0,0,1,1,0,0,1,1,1},
            {1,1,1,1,1,1,1,1,1,1,1,1},
            {0,0,0,1,1,0,0,1,1,0,0,0},
            {0,0,1,1,0,1,1,0,1,1,0,0},
            {1,1,0,0,0,0,0,0,0,0,1,1}
        } : new int[,]
        {
            {0,0,0,0,1,1,1,1,0,0,0,0},
            {0,1,1,1,1,1,1,1,1,1,1,0},
            {1,1,1,1,1,1,1,1,1,1,1,1},
            {1,1,1,0,0,1,1,0,0,1,1,1},
            {1,1,1,1,1,1,1,1,1,1,1,1},
            {0,0,1,1,1,0,0,1,1,1,0,0},
            {0,1,1,0,0,1,1,0,0,1,1,0},
            {0,0,1,1,0,0,0,0,1,1,0,0}
        };
        AddPixels(geometry, pixels, 3.0);
        return geometry;
    }

    private static void AddPixels(GeometryGroup group, int[,] pixels, double scale)
    {
        int rows = pixels.GetLength(0);
        int cols = pixels.GetLength(1);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (pixels[r, c] == 1)
                {
                    group.Children.Add(new RectangleGeometry(
                        new Rect(c * scale, r * scale, scale, scale)));
                }
            }
        }
    }

    public void ToggleAnimation()
    {
        _animationFrame = !_animationFrame;
        _path.Data = GetGeometry(_animationFrame);
    }

    public override void Update()
    {
    }

    public void Move(double dx, double dy)
    {
        X += dx;
        Y += dy;
    }
}
