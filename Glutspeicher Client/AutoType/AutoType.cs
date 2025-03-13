using System.Threading;

namespace Glutspeicher.Client;

public partial class AutoType
{
    static bool PerformIntoCurrentWindow(string keyString)
    {
        Thread.Sleep(100);
        return Perform(keyString);
    }

    static bool Perform(string keyString)
    {
        AutoType_SendInputEx.SendKeysWait(keyString);
        return true;
    }

    static string Escape(string s)
    {
        var open = '\u25A1';
        while (s.Contains(open))
        {
            open++;
        }

        var close = (char) (open + 1);
        while (s.Contains(close))
        {
            close++;
        }

        s = s.Replace('{', open).Replace('}', close);
        s = s.Replace(new string(open, 1), @"{{}");
        s = s.Replace(new string(close, 1), @"{}}");

        return s;
    }

    static string Encode(string s)
    {
        s = s.Replace(@"[", @"{[}");
        s = s.Replace(@"]", @"{]}");
        s = s.Replace(@"+", @"{+}");
        s = s.Replace(@"%", @"{%}");
        s = s.Replace(@"~", @"{~}");
        s = s.Replace(@"(", @"{(}");
        s = s.Replace(@")", @"{)}");
        s = s.Replace(@"^", @"{^}");

        s = s.Replace("\r\n", "\n");
        s = s.Replace("\r", "\n");
        s = s.Replace("\n", @"{ENTER}");

        return s;
    }
}
