using Glutspeicher.Server.Middleware;
using Glutspeicher.Server.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

LoadConfig(builder);

if (CryptoKey.Length != 32)
{
    Console.WriteLine("Environment Variable \"CRYPTO_KEY\" => Length != 32");
    return -1;
}

var services = builder.Services;

services.AddTransient<LiteDbContext>();

var app = builder.Build();

app.UseApi();
app.UseMiddleware<FrontendMiddleware>();
app.UseMiddleware<NoCacheMiddleware>();

app.Urls.Add($"http://[::]:{HttpPort}");

await app.RunAsync();

Dispose();

return 0;
