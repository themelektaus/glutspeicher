using System.Reflection;

namespace Glutspeicher.Server.Utility;

public static class CommonUtils
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
    public static readonly Type[] AllTypes = Assembly.GetTypes();

    public static DateTime Now => DateTime.Now;

    public static DateTime Today => Now.Date;

    public static Task WaitAsync() => Task.Delay(17);
}
