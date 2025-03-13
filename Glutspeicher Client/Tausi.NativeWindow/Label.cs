using System.Drawing;
using Vanara.PInvoke;

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
        var style = User32.WindowStyles.WS_CHILD
            | User32.WindowStyles.WS_VISIBLE
            | (User32.WindowStyles) User32.ButtonStyle.BS_OWNERDRAW;

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

    public HFONT CreateFont()
    {
        return Gdi32.CreateFont(
            cHeight: (int) -(FontSize * Window.UseDC(x => Gdi32.GetDeviceCaps(x, Gdi32.DeviceCap.LOGPIXELSY)) / 72f),
            cWidth: 0,
            cEscapement: 0,
            cOrientation: 0,
            cWeight: Bold ? Gdi32.FW_BOLD : Gdi32.FW_NORMAL,
            bItalic: false,
            bUnderline: false,
            bStrikeOut: false,
            iCharSet: CharacterSet.DEFAULT_CHARSET,
            iOutPrecision: 0,
            iClipPrecision: 0,
            iQuality: Gdi32.OutputQuality.DEFAULT_QUALITY,
            iPitchAndFamily: Gdi32.PitchAndFamily.DEFAULT_PITCH
        );
    }

    public override void Dispose()
    {
        Handle = 0;
    }
}
