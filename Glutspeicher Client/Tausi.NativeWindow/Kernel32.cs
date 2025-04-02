using System.Runtime.InteropServices;

namespace Tausi.NativeWindow;

public static class Kernel32
{
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern nint GetModuleHandle([Optional] string lpModuleName);

    [DllImport("kernel32")]
    public static extern bool AllocConsole();

    [DllImport("kernel32")]
    public static extern nint GetConsoleWindow();
}
