using System;
using System.Drawing;

namespace Tausi.NativeWindow;

public abstract class Frame : IDisposable
{
    public virtual Rectangle Rect { get; set; }

    public abstract void Dispose();

    public int X
    {
        get => Location.X;
        set
        {
            if (Location.X != value)
            {
                var location = Location;
                location.X = value;
                Location = location;
            }
        }
    }

    public int Y
    {
        get => Location.Y;
        set
        {
            if (Location.Y != value)
            {
                var location = Location;
                location.Y = value;
                Location = location;
            }
        }
    }

    public int Width
    {
        get => Size.Width;
        set
        {
            if (Size.Width != value)
            {
                var size = Size;
                size.Width = value;
                Size = size;
            }
        }
    }

    public int Height
    {
        get => Size.Height;
        set
        {
            if (Size.Height != value)
            {
                var size = Size;
                size.Height = value;
                Size = size;
            }
        }
    }

    public Point Location
    {
        get => Rect.Location;
        set
        {
            if (Rect.Location != value)
            {
                var rect = Rect;
                rect.Location = value;
                Rect = rect;
            }
        }
    }

    public Size Size
    {
        get => Rect.Size;
        set
        {
            if (Rect.Size != value)
            {
                var rect = Rect;
                rect.Size = value;
                Rect = rect;
            }
        }
    }
}
