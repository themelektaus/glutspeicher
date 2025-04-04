﻿using System.Diagnostics;
using System.Threading.Tasks;

namespace Glutspeicher.Client;

public class Ssh : Relay
{
    public string username;
    public string password;

    protected override async Task OnRun(RelaySession relaySession)
    {
        if (string.IsNullOrEmpty(hostname))
        {
            throw new($"{nameof(hostname)} is null or empty");
        }

        string port = this.port == 0
            ? string.Empty
            : $"-p {this.port} ";

        string username = string.IsNullOrEmpty(this.username)
            ? string.Empty
            : $"{this.username}@";

        var process = Process.Start(
            new ProcessStartInfo(
                "ssh", $"-o StrictHostKeychecking=no -o UserKnownHostsFile=/dev/null {port}{username}{hostname}"
            )
            {
                UseShellExecute = true
            }
        );

        await process.WaitForExitAsync();
    }
}
