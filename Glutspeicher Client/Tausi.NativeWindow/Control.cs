using System;
using Vanara.PInvoke;

namespace Tausi.NativeWindow;

public abstract class Control : IDisposable
{
    public int Id { get; set; }

    public Window Owner { get; set; }

    public HWND Handle { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; } = 30;

    public abstract void Create(Window window);

    public abstract void Dispose();
}
