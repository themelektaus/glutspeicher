using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tausi.NativeWindow;

public static class Gdi32
{
    [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
    public static extern nint CreateSolidBrush(int color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DeleteBrush(nint hbr) => DeleteObject(hbr);

    [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
    public static extern int GetDeviceCaps(nint hdc, DeviceCap index);

    public enum DeviceCap : uint
    {
        HORZRES = 8,
        VERTRES = 10,
        LOGPIXELSX = 88,
        LOGPIXELSY = 90
    }

    [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
    public static extern uint SetBkColor(nint hdc, int color);

    [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
    public static extern uint SetTextColor(nint hdc, int color);

    [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
    public static extern uint SetTextAlign(nint hdc, uint align);

    [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
    public static extern nint SelectObject([In] nint hdc, [In] nint h);

    [DllImport("gdi32.dll", SetLastError = false, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TextOut(nint hdc, int x, int y, string lpString, int c);

    [DllImport("gdi32.dll", SetLastError = false, CharSet = CharSet.Auto)]
    public static extern nint CreateFont(
        [Optional] int cHeight,
        [Optional] int cWidth,
        [Optional] int cEscapement,
        [Optional] int cOrientation,
        [Optional] int cWeight,
        [Optional, MarshalAs(UnmanagedType.Bool)] bool bItalic,
        [Optional, MarshalAs(UnmanagedType.Bool)] bool bUnderline,
        [Optional, MarshalAs(UnmanagedType.Bool)] bool bStrikeOut,
        byte iCharSet = 1,
        byte iOutPrecision = 0,
        byte iClipPrecision = 0,
        byte iQuality = 0,
        uint iPitchAndFamily = 0,
        string pszFaceName = null
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DeleteFont(nint hfont) => DeleteObject(hfont);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(nint hObject);
}
