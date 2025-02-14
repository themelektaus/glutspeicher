using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Glutspeicher.Agent;

public class RelaySession(string sourceNetwork, string destinationHost, ushort destinationPort)
{
    public ushort Port { get; private set; }

    static SshClient CreateSshClient(string hostname, ushort port, string username, string password)
    {
        return new(hostname, port, username, password);
    }

    public bool Start(string hostname, ushort port, string username, string password, ushort minPort, ushort maxPort)
    {
        if (!SetupPort(minPort, maxPort))
        {
            return false;
        }

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
        CreateNatRule(sshClient);

        return Disconnect(sshClient);
    }

    bool SetupPort(ushort minPort, ushort maxPort)
    {
        var usedPorts = new List<FileInfo>();

        for (var _port = minPort; _port <= maxPort; _port++)
        {
            if (File.Exists(_port.ToString()))
            {
                usedPorts.Add(new(_port.ToString()));
            }
        }

        var port = minPort;

        while (port <= maxPort)
        {
            if (usedPorts.Any(x => x.Name == port.ToString()))
            {
                port++;
                continue;
            }

            Port = port;
            return true;
        }

        if (usedPorts.Count == 0)
        {
            return false;
        }

        var usedPort = usedPorts.OrderBy(x => x.LastWriteTimeUtc).FirstOrDefault();
        Port = ushort.Parse(usedPort.Name);
        usedPort.Delete();
        return true;
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

        using var _ = File.Open(Port.ToString(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        File.SetLastWriteTimeUtc(Port.ToString(), DateTime.UtcNow);
    }

    static void Run(SshClient sshClient, string commandText)
    {
        sshClient.RunCommand(commandText).Dispose();
    }

    static void DeleteNatRule(SshClient sshClient, ushort port)
    {
        Run(sshClient, $"eval \"$(iptables -t nat --list-rules | grep '\\-\\-dport {port}' | sed 's/^-A /iptables -t nat -D /g')\"");

        File.Delete(port.ToString());
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
        }

        Disconnect(sshClient);
    }
}
