namespace Glutspeicher.Client;

public sealed class AutoType_Event
{
    public enum Type { None, Key, KeyModifier, Char }
    public Type type;
    public int vKey;
    public bool? isExtendedKey;
    public int keyModifier;
    public char @char = char.MinValue;
    public bool? down;
    public string text;
}
