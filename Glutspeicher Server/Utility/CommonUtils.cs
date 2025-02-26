using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace Glutspeicher.Server.Utility;

public static class CommonUtils
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
    public static readonly Type[] AllTypes = Assembly.GetTypes();

    public static DateTime Now => DateTime.Now;

    public static DateTime Today => Now.Date;

    public static Task WaitAsync() => Task.Delay(17);

    static Aes _Aes;
    static Aes Aes => _Aes ??= Aes.Create();

    public static void Encrypt(ref byte[] data)
    {
        Aes.Key = CryptoKey;
        Aes.GenerateIV();

        using var stream = new MemoryStream();

        stream.Write(Aes.IV, 0, Aes.IV.Length);

        using (var cryptoStream = new CryptoStream(stream, Aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();
        }

        data = stream.ToArray();
    }

    public static void Decrypt(ref byte[] data)
    {
        Aes.Key = CryptoKey;

        var iv = new byte[16];
        Array.Copy(data, 0, iv, 0, iv.Length);
        Aes.IV = iv;

        using var stream = new MemoryStream();

        using (var encryptedStream = new MemoryStream(data, iv.Length, data.Length - iv.Length))
        {
            using var cryptoStream = new CryptoStream(encryptedStream, Aes.CreateDecryptor(), CryptoStreamMode.Read);
            cryptoStream.CopyTo(stream);
        }

        data = stream.ToArray();
    }

    public static void Dispose()
    {
        Aes?.Dispose();
    }
}
