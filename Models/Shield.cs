using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SpaceInvaders.Models;

public class ShieldBlock
{
    public double X { get; }
    public double Y { get; }
    public Rectangle Visual { get; }
    public bool IsActive { get; set; } = true;

    public const double BlockSize = 4;

    public ShieldBlock(double x, double y)
    {
        X = x;
        Y = y;
        Visual = new Rectangle
        {
            Width = BlockSize,
            Height = BlockSize,
            Fill = Brushes.Lime
        };
    }

    public Rect GetBounds() => new(X, Y, BlockSize, BlockSize);
}

public class Shield
{
    public double X { get; }
    public double Y { get; }
    public List<ShieldBlock> Blocks { get; } = new();

    public const double ShieldWidth = 72;
    public const double ShieldHeight = 52;

    // Classic shield shape pattern (18x13 blocks at 4px each)
    private static readonly int[,] ShieldPattern = new int[,]
    {
        {0,0,0,0,1,1,1,1,1,1,1,1,1,1,0,0,0,0},
        {0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0},
        {0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0},
        {0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,0,0,0,0,0,1,1,1,1,1,1},
        {1,1,1,1,1,0,0,0,0,0,0,0,0,1,1,1,1,1},
        {1,1,1,1,0,0,0,0,0,0,0,0,0,0,1,1,1,1},
        {1,1,1,1,0,0,0,0,0,0,0,0,0,0,1,1,1,1}
    };

    public Shield(double x, double y)
    {
        X = x;
        Y = y;

        int rows = ShieldPattern.GetLength(0);
        int cols = ShieldPattern.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (ShieldPattern[r, c] == 1)
                {
                    double blockX = x + c * ShieldBlock.BlockSize;
                    double blockY = y + r * ShieldBlock.BlockSize;
                    Blocks.Add(new ShieldBlock(blockX, blockY));
                }
            }
        }
    }

    public void AddToCanvas(Canvas canvas)
    {
        foreach (var block in Blocks)
        {
            canvas.Children.Add(block.Visual);
            Canvas.SetLeft(block.Visual, block.X);
            Canvas.SetTop(block.Visual, block.Y);
        }
    }

    public void RemoveFromCanvas(Canvas canvas)
    {
        foreach (var block in Blocks)
        {
            canvas.Children.Remove(block.Visual);
        }
    }

    public bool CheckCollision(Rect bulletBounds, Canvas canvas)
    {
        foreach (var block in Blocks.Where(b => b.IsActive).ToList())
        {
            if (bulletBounds.IntersectsWith(block.GetBounds()))
            {
                // Destroy this block and nearby blocks for explosion effect
                DestroyBlocksNear(block.X, block.Y, canvas);
                return true;
            }
        }
        return false;
    }

    private void DestroyBlocksNear(double x, double y, Canvas canvas)
    {
        const double radius = 8;
        foreach (var block in Blocks.Where(b => b.IsActive).ToList())
        {
            double dx = block.X - x;
            double dy = block.Y - y;
            if (dx * dx + dy * dy <= radius * radius)
            {
                block.IsActive = false;
                canvas.Children.Remove(block.Visual);
            }
        }
    }

    public bool HasActiveBlocks() => Blocks.Any(b => b.IsActive);
}
