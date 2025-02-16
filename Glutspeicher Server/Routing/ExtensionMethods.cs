using Microsoft.AspNetCore.Builder;

namespace Glutspeicher.Server.Routing;

using Mapping;

public static class ExtensionMethods
{
    public static void UseApi(this WebApplication app)
    {
        app.MapGet("/api", Api.Get);

        app.MapGet("/api/database", Api.GetDatabse);

        foreach (var route in new Dictionary<string, Delegate[]>() {
            { "items", [
                Api.Items.GetAll,
                Api.Items.Get,
                Api.Items.Post,
                Api.Items.Put,
                Api.Items.Delete
            ] },
            { "csvs", [
                Api.Csvs.GetAll,
                Api.Csvs.Get,
                Api.Csvs.Post,
                Api.Csvs.Put,
                Api.Csvs.Delete
            ] },
            { "generators", [
                Api.Generators.GetAll,
                Api.Generators.Get,
                Api.Generators.Post,
                Api.Generators.Put,
                Api.Generators.Delete
            ] },
            { "relays", [
                Api.Relays.GetAll,
                Api.Relays.Get,
                Api.Relays.Post,
                Api.Relays.Put,
                Api.Relays.Delete
            ] },
        })
        {
            app.MapGet($"/api/{route.Key}", route.Value[0]);

            app.MapGet($"/api/{route.Key}/{{id}}", route.Value[1]);

            app.MapPost($"/api/{route.Key}", route.Value[2]);

            app.MapPut($"/api/{route.Key}", route.Value[3]);

            app.MapDelete($"/api/{route.Key}/{{id}}", route.Value[4]);
        }
    }
}
