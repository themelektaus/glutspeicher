using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Glutspeicher.Client;

public abstract class Relay
{
    public string hostname;
    public long port;

    public string relayHostname;
    public long relaySshPort;
    public string relaySshUsername;
    public string relaySshPassword;
    public long relayMinPort;
    public long relayMaxPort;
    public string webCommandLine;

    protected abstract Task OnRun(RelaySession relaySession);

    RelaySession StartSession()
    {
        if (string.IsNullOrEmpty(hostname))
        {
            return null;
        }

        if (string.IsNullOrEmpty(relayHostname))
        {
            return null;
        }

        if (string.IsNullOrEmpty(relaySshUsername))
        {
            return null;
        }

        if (string.IsNullOrEmpty(relaySshPassword))
        {
            return null;
        }

        if (relayMinPort == 0 || relayMaxPort == 0)
        {
            return null;
        }

        string remoteAddress;

        try
        {
            remoteAddress = Dns
                .GetHostAddresses(hostname)
                .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)
                .MapToIPv4()
                .ToString();
        }
        catch
        {
            throw;
        }

        var relaySession = new RelaySession("0.0.0.0/0", remoteAddress, (ushort) (port == 0 ? 22 : port));

        if (relaySession.Start(relayHostname, (ushort) (relaySshPort == 0 ? 22 : relaySshPort), relaySshUsername, relaySshPassword, (ushort) relayMinPort, (ushort) relayMaxPort))
        {
            hostname = relayHostname;
            port = relaySession.Port;
        }

        return relaySession;
    }

    void StopSession(RelaySession relaySession)
    {
        relaySession?.Stop(relayHostname, (ushort) (relaySshPort == 0 ? 22 : relaySshPort), relaySshUsername, relaySshPassword);
    }

    public virtual async Task Run()
    {
        var relaySession = StartSession();

        await OnRun(relaySession);

        StopSession(relaySession);
    }
}
