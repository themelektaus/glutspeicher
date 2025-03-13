using System.Drawing;

namespace Tausi.NativeWindow;

public class Label : Control
{
    public bool Enabled { get; set; }

    public string Text { get; set; }

    public virtual Color TextColor { get; set; } = SystemColors.Window;

    public virtual Color BackgroundColor { get; set; } = Color.Transparent;

    public int FontSize { get; set; } = 12;

    public Point TextOffset { get; set; } = new(0, -9);

    public bool Bold { get; set; }

    public override void Create(Window window)
    {
        const uint BS_OWNERDRAW = 0x0000000B;

        var style = User32.WindowStyles.WS_CHILD
            | User32.WindowStyles.WS_VISIBLE
            | (User32.WindowStyles) BS_OWNERDRAW;

        if (!Enabled)
        {
            style |= User32.WindowStyles.WS_DISABLED;
        };

        Handle = User32.CreateWindowEx(
            dwExStyle: 0,
            lpClassName: "BUTTON",
            lpWindowName: Text,
            dwStyle: style,
            X: X,
            Y: Y,
            nWidth: Width,
            nHeight: Height,
            hWndParent: Owner.Handle,
            hMenu: Id,
            hInstance: Window.moduleHandle
        );
    }

    public nint CreateFont()
    {
        const int FW_NORMAL = 400;
        const int FW_BOLD = 700;

        var hdc = User32.GetDC();
        var dpi = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.LOGPIXELSY);
        User32.ReleaseDC(hdc);

        return Gdi32.CreateFont(
            cHeight: (int) -(FontSize * dpi / 72f),
            cWidth: 0,
            cEscapement: 0,
            cOrientation: 0,
            cWeight: Bold ? FW_BOLD : FW_NORMAL,
            bItalic: false,
            bUnderline: false,
            bStrikeOut: false
        );
    }

    public override void Dispose()
    {
        Handle = 0;
    }
}
