using Microsoft.AspNetCore.Builder;
using System.Text;

namespace Glutspeicher.Server;

public static class AppSettings
{
    public static short HttpPort { get; private set; }
    public static string ServerVersion { get; private set; }
    public static string AgentVersion { get; private set; }
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
        AgentVersion = config[nameof(AgentVersion)];
    }

    public static void LoadEnvironmentVariables()
    {
        CryptoKey = [.. Encoding.UTF8.GetBytes(
            Environment.GetEnvironmentVariable("GLUTSPEICHER_CRYPTO_KEY") ?? string.Empty
        ).Take(32)];
    }
}
