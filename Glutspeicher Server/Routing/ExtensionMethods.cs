using Microsoft.AspNetCore.Builder;

namespace Glutspeicher.Server.Routing;

using Mapping;

public static class ExtensionMethods
{
    public static void UseApi(this WebApplication app)
    {
        app.MapGet("/api", Api.Get);

        app.MapGet("/api/database", Api.DownloadDatabase);

        app.MapGet("/api/database/rebuild", Api.RebuildDatabase);

        foreach (var route in new Dictionary<string, Delegate[]>() {
            { "passwords", [
                Api.Passwords.GetAll,
                Api.Passwords.Export,
                Api.Passwords.Get,
                Api.Passwords.Post,
                Api.Passwords.Put,
                Api.Passwords.Delete
            ] },
            { "exports", [
                Api.Exports.GetAll,
                Api.Exports.Export,
                Api.Exports.Get,
                Api.Exports.Post,
                Api.Exports.Put,
                Api.Exports.Delete
            ] },
            { "generators", [
                Api.Generators.GetAll,
                Api.Generators.Export,
                Api.Generators.Get,
                Api.Generators.Post,
                Api.Generators.Put,
                Api.Generators.Delete
            ] },
            { "relays", [
                Api.Relays.GetAll,
                Api.Relays.Export,
                Api.Relays.Get,
                Api.Relays.Post,
                Api.Relays.Put,
                Api.Relays.Delete
            ] },
        })
        {
            app.MapGet($"/api/{route.Key}", route.Value[0]);

            app.MapGet($"/api/{route.Key}/export", route.Value[1]);

            app.MapGet($"/api/{route.Key}/{{id}}", route.Value[2]);

            app.MapPost($"/api/{route.Key}", route.Value[3]);

            app.MapPut($"/api/{route.Key}", route.Value[4]);

            app.MapDelete($"/api/{route.Key}/{{id}}", route.Value[5]);
        }
    }
}
