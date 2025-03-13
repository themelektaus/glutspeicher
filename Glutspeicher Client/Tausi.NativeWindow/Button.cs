using System;
using System.Drawing;

namespace Tausi.NativeWindow;

public class Button : Label
{
    public override Color BackgroundColor { get; set; } = Color.FromArgb(60, 60, 70);

    public event EventHandler Click;
    public void InvokeClick() => Click?.Invoke(Owner, EventArgs.Empty);

    public bool Hover { get; set; }

    public Button()
    {
        Enabled = true;
    }
}
