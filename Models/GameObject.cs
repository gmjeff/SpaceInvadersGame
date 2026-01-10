using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SpaceInvaders.Models;

public abstract class GameObject
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsActive { get; set; } = true;
    public Shape? Visual { get; protected set; }

    protected GameObject(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public abstract void Update();

    public void Draw(Canvas canvas)
    {
        if (Visual == null) return;

        Canvas.SetLeft(Visual, X);
        Canvas.SetTop(Visual, Y);
    }

    public void AddToCanvas(Canvas canvas)
    {
        if (Visual != null)
        {
            canvas.Children.Add(Visual);
            Draw(canvas);
        }
    }

    public void RemoveFromCanvas(Canvas canvas)
    {
        if (Visual != null)
        {
            canvas.Children.Remove(Visual);
        }
    }

    public Rect GetBounds()
    {
        return new Rect(X, Y, Width, Height);
    }

    public bool CollidesWith(GameObject other)
    {
        return GetBounds().IntersectsWith(other.GetBounds());
    }
}
