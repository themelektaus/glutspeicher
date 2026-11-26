using Microsoft.AspNetCore.Builder;
using System.Text;

namespace Glutspeicher.Server;

public static class AppSettings
{
    // Public test key (32 zeros): used by Debug builds, refused by Release builds
    public const string TestCryptoKey = "00000000000000000000000000000000";

    public static short HttpPort { get; private set; }
    public static string ServerVersion { get; private set; }
    public static string ClientVersion { get; private set; }
    public static byte[] CryptoKey { get; private set; }

    public static void LoadConfig(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        HttpPort = short.Parse(config[nameof(HttpPort)]);

        if (HttpPort == 80 && OperatingSystem.IsWindows())
        {
            HttpPort += 5500;
        }

        ServerVersion = config[nameof(ServerVersion)];
        ClientVersion = config[nameof(ClientVersion)];
    }

    public static void LoadEnvironmentVariables()
    {
        CryptoKey = [.. Encoding.UTF8.GetBytes(
            Environment.GetEnvironmentVariable("GLUTSPEICHER_CRYPTO_KEY") ?? string.Empty
        ).Take(32)];
    }

    public static bool IsTestCryptoKey()
    {
        return CryptoKey.SequenceEqual(Encoding.UTF8.GetBytes(TestCryptoKey));
    }
}
