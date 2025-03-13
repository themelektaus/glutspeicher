namespace Glutspeicher.Client;

public sealed class AutoType_KeyCode(string code, int vKey)
{
    public readonly string code = string.IsNullOrEmpty(code) ? " " : code;
    public readonly int vKey = vKey;
}
