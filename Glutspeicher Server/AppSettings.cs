﻿using Microsoft.AspNetCore.Builder;

namespace Glutspeicher.Server;

public static class AppSettings
{
    public static short HttpPort { get; private set; }
    public static string ServerVersion { get; private set; }
    public static string AgentVersion { get; private set; }

    public static void LoadConfig(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        HttpPort = short.Parse(config[nameof(HttpPort)]);
        ServerVersion = config[nameof(ServerVersion)];
        AgentVersion = config[nameof(AgentVersion)];
    }
}
