using System.Runtime.InteropServices;
using System.Text;

namespace Glutspeicher.Client;

public static class AutoType_NativeMethods
{
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU = 0x12;
    public const int VK_CAPITAL = 0x14;
    public const int VK_NUMLOCK = 0x90;
    public const int VK_LSHIFT = 0xA0;
    public const int VK_RSHIFT = 0xA1;
    public const int VK_LCONTROL = 0xA2;
    public const int VK_RCONTROL = 0xA3;
    public const int VK_LMENU = 0xA4;
    public const int VK_RMENU = 0xA5;
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MOUSEINPUT32_WithSkip
    {
        public uint __Unused0;
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KEYBDINPUT32_WithSkip
    {
        public uint __Unused0;
        public ushort VirtualKeyCode;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUT32
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(0)] public MOUSEINPUT32_WithSkip MouseInput;
        [FieldOffset(0)] public KEYBDINPUT32_WithSkip KeyboardInput;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct SpecializedKeyboardINPUT64
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public ushort VirtualKeyCode;
        [FieldOffset(10)] public ushort ScanCode;
        [FieldOffset(12)] public uint Flags;
        [FieldOffset(16)] public uint Time;
        [FieldOffset(24)] public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("User32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("User32.dll", EntryPoint = "SendInput", SetLastError = true)]
    public static extern uint SendInput32(uint nInputs, INPUT32[] pInputs, int cbSize);

    [DllImport("User32.dll", EntryPoint = "SendInput", SetLastError = true)]
    public static extern uint SendInput64Special(uint nInputs, SpecializedKeyboardINPUT64[] pInputs, int cbSize);

    [DllImport("User32.dll")]
    public static extern nint GetMessageExtraInfo();

    [DllImport("User32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("User32.dll")]
    public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, nint hKL);

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public static extern ushort VkKeyScan(char ch);

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public static extern ushort VkKeyScanEx(char ch, nint hKL);

    [DllImport("User32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int ToUnicode(uint wVirtKey, uint wScanCode, nint lpKeyState, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbBuff, int cchBuff, uint wFlags);

    [DllImport("User32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, nint lpKeyState, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbBuff, int cchBuff, uint wFlags, nint hKL);

    [DllImport("User32.dll")]
    public static extern ushort GetKeyState(int vKey);

    [DllImport("User32.dll")]
    public static extern ushort GetAsyncKeyState(int vKey);

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

    [DllImport("User32.dll")]
    public static extern nint GetKeyboardLayout(uint idThread);

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("User32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hWnd, [Out] out uint lpdwProcessId);

    public static uint? GetLastInputTime()
    {
        var lastInputInfo = new LASTINPUTINFO
        {
            cbSize = (uint) Marshal.SizeOf<LASTINPUTINFO>()
        };

        if (GetLastInputInfo(ref lastInputInfo))
        {
            return lastInputInfo.dwTime;
        }

        return null;
    }

    public static uint MapVirtualKey3(uint uCode, uint uMapType, nint hKL)
    {
        if (hKL == nint.Zero)
            return MapVirtualKey(uCode, uMapType);

        return MapVirtualKeyEx(uCode, uMapType, hKL);
    }

    public static ushort VkKeyScan3(char ch, nint hKL)
    {
        if (hKL == nint.Zero)
            return VkKeyScan(ch);

        return VkKeyScanEx(ch, hKL);
    }

    public static string ToUnicode3(int vKey, byte[] pbKeyState, nint hKL)
    {
        var pState = nint.Zero;

        try
        {
            uint uScanCode = MapVirtualKey3((uint) vKey, 0, hKL);

            pState = Marshal.AllocHGlobal(256);
            if (pState == nint.Zero)
                return null;

            if (pbKeyState is null)
                return null;

            if (pbKeyState.Length != 256)
                return null;

            Marshal.Copy(pbKeyState, 0, pState, 256);

            var sbUni = new StringBuilder(32);

            var r = hKL == nint.Zero
                ? ToUnicode((uint) vKey, uScanCode, pState, sbUni, 30, 0)
                : ToUnicodeEx((uint) vKey, uScanCode, pState, sbUni, 30, 0, hKL);

            if (r < 0)
                return string.Empty;

            if (r == 0)
                return null;

            var str = sbUni.ToString();

            if (string.IsNullOrEmpty(str))
                return null;

            if (r < str.Length)
                str = str[..r];

            return str;
        }
        catch
        {
            throw;
        }
        finally
        {
            if (pState != nint.Zero)
                Marshal.FreeHGlobal(pState);
        }
    }
}
