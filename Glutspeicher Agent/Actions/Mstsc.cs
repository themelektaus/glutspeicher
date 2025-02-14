using System;
using System.Threading.Tasks;

namespace Glutspeicher.Agent;

public class Mstsc : Relay
{
    public string username;
    public string password;

    protected override async Task OnRun(RelaySession relaySession)
    {
        if (string.IsNullOrEmpty(hostname))
        {
            throw new($"{nameof(hostname)} is null or empty");
        }

        if (!string.IsNullOrEmpty(username))
        {
            var pass = (password ?? string.Empty).Contains('"')
                ? string.Empty
                : $"/pass:\"{(password ?? string.Empty)}\"";

            await Utils.RunAsync(
                Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe"),
                $"/generic:TERMSRV/{hostname} /user:\"{username}\" {pass}",
                hidden: true
            );
        }

        await Utils.RunAsync(
            Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe"),
            $"/v {hostname}:{(port == 0 ? 3389 : port)}",
            hidden: false
        );

        if (!string.IsNullOrEmpty(username))
        {
            await Utils.RunAsync(
                Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe"),
                $"/delete:TERMSRV/{hostname}",
                hidden: true
            );
        }
    }
}
