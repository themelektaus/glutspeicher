using System.Collections.Generic;
using System.Linq;

namespace Glutspeicher.Client;

public static class AutoType_KeyCodeCollection
{
    static List<AutoType_KeyCode> items;
    public static List<AutoType_KeyCode> Items => items ??= [
        new("BREAK", 3),
        new("BACKSPACE", 8),
        new("BKSP", 8),
        new("BS", 8),
        new("TAB", 9),
        new("CLEAR", 12),
        new("ENTER", 13),
        new("CAPSLOCK", 20),
        new("ESC", 27),
        new("ESCAPE", 27),
        new("SPACE", 32),
        new("PGUP", 33),
        new("PGDN", 34),
        new("END", 35),
        new("HOME", 36),
        new("LEFT", 37),
        new("UP", 38),
        new("RIGHT", 39),
        new("DOWN", 40),
        new("DEL", 46),
        new("DELETE", 46),
        new("PRTSC", 44),
        new("INS", 45),
        new("INSERT", 45),
        new("HELP", 47),
        new("WIN", 91),
        new("LWIN", 91),
        new("RWIN", 92),
        new("APPS", 93),
        new("NUMPAD0", 96),
        new("NUMPAD1", 97),
        new("NUMPAD2", 98),
        new("NUMPAD3", 99),
        new("NUMPAD4", 100),
        new("NUMPAD5", 101),
        new("NUMPAD6", 102),
        new("NUMPAD7", 103),
        new("NUMPAD8", 104),
        new("NUMPAD9", 105),
        new("MULTIPLY", 106),
        new("ADD", 107),
        new("SUBTRACT", 109),
        new("DIVIDE", 111),
        new("F1", 112),
        new("F2", 113),
        new("F3", 114),
        new("F4", 115),
        new("F5", 116),
        new("F6", 117),
        new("F7", 118),
        new("F8", 119),
        new("F9", 120),
        new("F10", 121),
        new("F11", 122),
        new("F12", 123),
        new("NUMLOCK", 144),
        new("SCROLLLOCK", 145),
    ];

    static Dictionary<string, AutoType_KeyCode> codes;
    public static AutoType_KeyCode Get(string code)
        => (codes ??= Items.ToDictionary(x => x.code, x => x))
            .TryGetValue(code.ToUpperInvariant(), out var si) ? si : null;

    static Dictionary<char, int> charsToKeys;
    static Dictionary<char, int> charsToKeys_Always;
    static void EnsureCharsToKeys()
    {
        if (charsToKeys is not null)
        {
            return;
        }

        var d = new Dictionary<char, int>
        {
            ['\u0008'] = 8,
            ['\t'] = 9,
            ['\n'] = 10,
            ['\r'] = 13,
            ['\u001B'] = 27,
            [' '] = 32,
            ['\u007F'] = 46
        };

        charsToKeys_Always = new Dictionary<char, int>(d);

        for (char c = '0'; c <= '9'; ++c)
        {
            d[c] = c - '0' + 48;
        }

        for (char c = 'a'; c <= 'z'; ++c)
        {
            d[c] = c - 'a' + 65;
        }

        charsToKeys = d;
    }

    public static int CharToVKey(char c, bool force)
    {
        EnsureCharsToKeys();
        return (force ? charsToKeys_Always : charsToKeys)
            .TryGetValue(c, out var vKey) ? vKey : 0;
    }
}
