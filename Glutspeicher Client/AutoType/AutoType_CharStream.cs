namespace Glutspeicher.Client;

public sealed class AutoType_CharStream(string s)
{
    readonly string s = s;

    int position;

    public char Read()
    {
        return position < s.Length ? s[position++] : char.MinValue;
    }

    public char Peek()
    {
        return position < s.Length ? s[position] : char.MinValue;
    }
}
