using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using StringComparison = System.StringComparison;

namespace Glutspeicher.Client;

public static class AutoType_SendInputEx
{
    static readonly Lock locker = new();

    public static void SendKeysWait(string keyString)
    {
        var events = Parse(keyString);
        if (events.Count == 0)
        {
            return;
        }

        locker.Enter();
        
        try
        {
            Fix(events);
            using var engine = new AutoType_Engine();
            Send(engine, events);
        }
        finally
        {
            locker.Exit();
        }
    }

    static List<AutoType_Event> Parse(string keyString)
    {
        var stream = new AutoType_CharStream(keyString);
        var events = new List<AutoType_Event>();
        var eventKeyModifiers = new List<int> { 0 };
        var keyModifiers = 0;

        for (; ; )
        {
            var c = stream.Read();
            if (c == char.MinValue)
                break;

            if ((c == '+') || (c == '^') || (c == '%'))
            {
                if (eventKeyModifiers.Count == 0)
                    break;

                if (c == '+')
                {
                    eventKeyModifiers[^1] |= 65536;
                    continue;
                }

                if (c == '^')
                {
                    eventKeyModifiers[^1] |= 131072;
                    continue;
                }

                if (c == '%')
                {
                    eventKeyModifiers[^1] |= 262144;
                    continue;
                }
            }

            if (c == '(')
            {
                eventKeyModifiers.Add(0);
                continue;
            }

            if (c == ')')
            {
                if (eventKeyModifiers.Count >= 2)
                {
                    eventKeyModifiers.RemoveAt(eventKeyModifiers.Count - 1);
                    eventKeyModifiers[^1] = 0;
                }
                else
                {
                    throw new System.FormatException();
                }
                continue;
            }

            var targetKeyModifiers = 0;
            foreach (var keyModifier in eventKeyModifiers)
                targetKeyModifiers |= keyModifier;

            EnsureKeyModifiers(ref keyModifiers, targetKeyModifiers, events);

            if (c == '{')
            {
                events.AddRange(ParseSpecial(stream));
            }
            else if (c == '}')
            {
                throw new System.FormatException();
            }
            else if (c == '~')
            {
                events.Add(new()
                {
                    type = AutoType_Event.Type.Key,
                    vKey = 13
                });
            }
            else
            {
                events.Add(new()
                {
                    type = AutoType_Event.Type.Char,
                    @char = c
                });
            }

            eventKeyModifiers[^1] = 0;
        }

        EnsureKeyModifiers(ref keyModifiers, 0, events);

        return events;
    }

    static void EnsureKeyModifiers(
        ref int currentKeys,
        int targetKeys,
        List<AutoType_Event> events
    )
    {
        if (currentKeys == targetKeys)
            return;

        if ((currentKeys & 65536) != (targetKeys & 65536))
        {
            events.Add(new()
            {
                type = AutoType_Event.Type.KeyModifier,
                keyModifier = 65536,
                down = ((targetKeys & 65536) != 0)
            });
        }

        if ((currentKeys & 131072) != (targetKeys & 131072))
        {
            events.Add(new()
            {
                type = AutoType_Event.Type.KeyModifier,
                keyModifier = 131072,
                down = ((targetKeys & 131072) != 0)
            });
        }

        if ((currentKeys & 262144) != (targetKeys & 262144))
        {
            events.Add(new()
            {
                type = AutoType_Event.Type.KeyModifier,
                keyModifier = 262144,
                down = ((targetKeys & 262144) != 0)
            });
        }

        currentKeys = targetKeys;
    }

    static List<AutoType_Event> ParseSpecial(AutoType_CharStream stream)
    {
        for (; ; )
        {
            var c = stream.Peek();
            if (c == char.MinValue)
                return null;

            if (!char.IsWhiteSpace(c))
                break;

            stream.Read();
        }

        var firstChar = stream.Read();
        if (firstChar == char.MinValue)
            return null;

        int part = 0;

        var nameBuilder = new StringBuilder();
        nameBuilder.Append(firstChar);

        var parametersBuilder = new StringBuilder();

        for (; ; )
        {
            var c = stream.Read();
            if (c == char.MinValue)
                return null;

            if (c == '}')
                break;

            if (part == 0)
            {
                if (char.IsWhiteSpace(c))
                {
                    part++;
                    continue;
                }

                nameBuilder.Append(c);
                continue;
            }

            parametersBuilder.Append(c);
        }

        var name = nameBuilder.ToString();
        var parameters = parametersBuilder.ToString().Trim();

        uint? parameter = null;

        if (parameters.Length > 0)
        {
            if (uint.TryParse(parameters, out var _parameter))
                parameter = _parameter;
        }

        var events = new List<AutoType_Event>();

        AutoType_Event @event;

        if (
            name.Equals("VKEY", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("VKEY-NX", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("VKEY-EX", StringComparison.OrdinalIgnoreCase)
        )
        {
            @event = CreateKeyEvent(parameters);
            if (@event is null)
                return null;

            if (name.EndsWith("-NX", StringComparison.OrdinalIgnoreCase))
                @event.isExtendedKey = false;

            else if (name.EndsWith("-EX", StringComparison.OrdinalIgnoreCase))
                @event.isExtendedKey = true;

            events.Add(@event);

            return events;
        }

        var code = AutoType_KeyCodeCollection.Get(name);

        @event = new();

        if (code is not null)
        {
            @event.type = AutoType_Event.Type.Key;
            @event.vKey = code.vKey;
        }
        else if (name.Length == 1)
        {
            @event.type = AutoType_Event.Type.Char;
            @event.@char = name[0];
        }
        else
        {
            throw new System.FormatException();
        }

        var repeat = parameter.GetValueOrDefault(1);

        for (uint i = 0; i < repeat; ++i)
        {
            events.Add(new()
            {
                type = @event.type,
                vKey = @event.vKey,
                isExtendedKey = @event.isExtendedKey,
                @char = @event.@char
            });
        }

        return events;
    }

    static void Fix(List<AutoType_Event> events)
    {
        foreach (var @event in events)
        {
            if (@event.type != AutoType_Event.Type.Char)
            {
                continue;
            }

            int vKey = AutoType_KeyCodeCollection.CharToVKey(@event.@char, true);
            if (vKey > 0)
            {
                @event.type = AutoType_Event.Type.Key;
                @event.vKey = vKey;
            }
        }
    }

    static void Send(AutoType_Engine engine, List<AutoType_Event> events)
    {
        bool isFirstInput = true;
        var cancel = false;

        foreach (AutoType_Event @event in events)
        {
            if (
                @event.type == AutoType_Event.Type.Key ||
                @event.type == AutoType_Event.Type.KeyModifier ||
                @event.type == AutoType_Event.Type.Char
            )
            {
                if (!isFirstInput)
                    engine.Delay(50);

                isFirstInput = false;
            }

            switch (@event.type)
            {
                case AutoType_Event.Type.Key:
                    engine.SendKey(@event.vKey, @event.isExtendedKey, @event.down);
                    break;

                case AutoType_Event.Type.KeyModifier:
                    if (@event.down.HasValue)
                        engine.SetKeyModifier(@event.keyModifier, @event.down.Value);
                    else { Debug.Assert(false); }
                    break;

                case AutoType_Event.Type.Char:
                    engine.SendChar(@event.@char, @event.down);
                    break;

                default:
                    break;
            }

            if (
                (@event.type == AutoType_Event.Type.Key && @event.vKey == 9) ||
                (@event.type == AutoType_Event.Type.Char && @event.@char == '\t')
            )
            {
                engine.Delay(50);
            }

            if (cancel)
                break;
        }
    }

    static AutoType_Event CreateKeyEvent(string parameters)
    {
        var parameterArray = (parameters ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        if (parameterArray.Length == 0)
            return null;

        var @event = new AutoType_Event { type = AutoType_Event.Type.Key };

        if (!int.TryParse(parameterArray[0], out var k))
            return null;

        if (k < 0)
            return null;

        @event.vKey = k;

        foreach (var parameter in parameterArray)
        {
            if (!parameter.Contains('='))
                continue;

            bool ext = parameter.Contains('E');
            bool nonExt = parameter.Contains('N');

            if (ext && !nonExt)
                @event.isExtendedKey = true;

            if (!ext && nonExt)
                @event.isExtendedKey = false;

            bool down = parameter.Contains('D');
            bool up = parameter.Contains('U');

            if (down && !up)
                @event.down = true;

            if (!down && up)
                @event.down = false;
        }

        return @event;
    }
}
