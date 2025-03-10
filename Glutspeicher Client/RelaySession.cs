using Renci.SshNet;
using System;
using System.Linq;

namespace Glutspeicher.Client;

public class RelaySession(string sourceNetwork, string destinationHost, ushort destinationPort)
{
    const string WD = "/etc/glutspeicher/relay/ports";

    public ushort Port { get; private set; }

    static SshClient CreateSshClient(string hostname, ushort port, string username, string password)
    {
        return new(hostname, port, username, password);
    }

    public bool Start(string hostname, ushort port, string username, string password, ushort minPort, ushort maxPort)
    {
        var sshClient = CreateSshClient(hostname, port, username, password);
        if (sshClient is null)
        {
            return false;
        }

        if (!Connect(sshClient))
        {
            return false;
        }

        if (!SetupPort(sshClient, minPort, maxPort))
        {
            return false;
        }

        DeleteNatRule(sshClient, Port);
        CreateNatRule(sshClient);

        return Disconnect(sshClient);
    }

    bool SetupPort(SshClient sshClient, ushort minPort, ushort maxPort)
    {
        string output;

        Run(sshClient, $"mkdir -p {WD}");

        using (var command = sshClient.CreateCommand($"ls -tr {WD}"))
        {
            var usedPorts = command.Execute()?
                .Split("\n")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => ushort.TryParse(x, out var y) ? y : 0)
                .Where(x => x != 0) ?? [];

            for (ushort port = minPort; port <= maxPort; port++)
            {
                if (!usedPorts.Contains(port))
                {
                    Port = port;
                    goto Success;
                }
            }
        }

        using (var command = sshClient.CreateCommand($"ls -tr {WD} | head -1"))
        {
            output = command.Execute()?.Trim() ?? string.Empty;
        }

        if (output == string.Empty)
        {
            return false;
        }

        if (!ushort.TryParse(output.Trim(), out var exstingPort))
        {
            return false;
        }

        Port = exstingPort;

    Success:
        Run(sshClient, $"touch {WD}/{Port}");

        return true;
    }

    static void ReleasePort(SshClient sshClient, ushort port)
    {
        Run(sshClient, $"rm {WD}/{port}");
    }

    public bool Stop(string hostname, ushort port, string username, string password)
    {
        var sshClient = CreateSshClient(hostname, port, username, password);
        if (sshClient is null)
        {
            return false;
        }

        if (!Connect(sshClient))
        {
            return false;
        }

        DeleteNatRule(sshClient, Port);
        ReleasePort(sshClient, Port);

        return Disconnect(sshClient);
    }

    static bool Connect(SshClient sshClient)
    {
        sshClient.Connect();

        if (!sshClient.IsConnected)
        {
            return false;
        }

        return true;
    }

    static bool Disconnect(SshClient sshClient)
    {
        if (!sshClient.IsConnected)
        {
            return false;
        }

        sshClient.Disconnect();

        if (sshClient.IsConnected)
        {
            return false;
        }

        return true;
    }

    void CreateNatRule(SshClient sshClient)
    {
        var iptables = "iptables -t nat -A";
        var p = "-p tcp -m tcp";
        var s = sourceNetwork is null ? "" : $"-s {sourceNetwork}";
        var d = $"--dport {Port}";
        var j = $"-j DNAT --to-destination {destinationHost}:{destinationPort}";

        Run(sshClient, $"{iptables} PREROUTING {p} {s} {d} {j}");
        Run(sshClient, $"{iptables} OUTPUT {p} {d} {j}");
    }

    static void DeleteNatRule(SshClient sshClient, ushort port)
    {
        Run(sshClient, $"eval \"$(iptables -t nat --list-rules | grep '\\-\\-dport {port}' | sed 's/^-A /iptables -t nat -D /g')\"");
    }

    static void Run(SshClient sshClient, string commandText)
    {
        sshClient.RunCommand(commandText).Dispose();
    }

    public static void DeleteAllNatRules(string hostname, ushort port, string username, string password, ushort minPort, ushort maxPort)
    {
        var sshClient = CreateSshClient(hostname, port, username, password);
        if (sshClient is null)
        {
            return;
        }

        if (!Connect(sshClient))
        {
            return;
        }

        for (var _port = minPort; _port <= maxPort; _port++)
        {
            DeleteNatRule(sshClient, _port);
            ReleasePort(sshClient, _port);
        }

        Disconnect(sshClient);
    }
}
