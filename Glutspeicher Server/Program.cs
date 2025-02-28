using Glutspeicher.Server.Middleware;
using Glutspeicher.Server.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if RELEASE
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
#endif

var builder = WebApplication.CreateBuilder(args);

LoadConfig(builder);

#if RELEASE
if (OperatingSystem.IsWindows())
{
    Environment.CurrentDirectory = Path.GetDirectoryName(Environment.ProcessPath);

    if (Environment.UserInteractive)
    {
        if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas"
            });
            return 0;
        }

        var serviceName = $"{nameof(Glutspeicher)} {nameof(Glutspeicher.Server)}";

        await Process.Start("sc", $"stop \"{serviceName}\"").WaitForExitAsync();
        await Task.Delay(1000);

        await Process.Start("sc", $"delete \"{serviceName}\"").WaitForExitAsync();
        await Task.Delay(1000);

        var cryptoKey = Environment.GetEnvironmentVariable("GLUTSPEICHER_CRYPTO_KEY", EnvironmentVariableTarget.Machine);
        if (string.IsNullOrEmpty(cryptoKey))
        {
            cryptoKey = Guid.NewGuid().ToString().Replace("-", "");
            Console.WriteLine();
            Console.WriteLine($"Creating new GLUTSPEICHER_CRYPTO_KEY ...");
            Console.WriteLine();
            Environment.SetEnvironmentVariable("GLUTSPEICHER_CRYPTO_KEY", cryptoKey, EnvironmentVariableTarget.Machine);
            cryptoKey = Environment.GetEnvironmentVariable("GLUTSPEICHER_CRYPTO_KEY", EnvironmentVariableTarget.Machine);
        }
        
        await Process.Start("sc", $"create \"{serviceName}\" depend= SharedAccess start= auto DisplayName= \"{serviceName}\" binPath= \"{Environment.ProcessPath}\"").WaitForExitAsync();
        await Task.Delay(1000);

        await Process.Start("sc", $"start \"{serviceName}\"").WaitForExitAsync();
        await Task.Delay(1000);

        Process.Start(new ProcessStartInfo
        {
            FileName = $"http://localhost:{HttpPort}",
            UseShellExecute = true
        });

        return 0;
    }
}
#endif

LoadEnvironmentVariables();

if (CryptoKey.Length != 32)
{
    Console.WriteLine("Environment Variable \"GLUTSPEICHER_CRYPTO_KEY\" => Length != 32");
    return -1;
}

var services = builder.Services;
services.AddWindowsService();
services.AddTransient<LiteDbContext>();

var app = builder.Build();

app.UseApi();
app.UseMiddleware<FrontendMiddleware>();
app.UseMiddleware<NoCacheMiddleware>();

app.Urls.Add($"http://[::]:{HttpPort}");

await app.RunAsync();

Dispose();

return 0;
