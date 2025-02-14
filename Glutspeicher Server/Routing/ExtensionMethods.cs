using Microsoft.AspNetCore.Builder;

namespace Glutspeicher.Server.Routing;

using Mapping;

public static class ExtensionMethods
{
    public static void UseApi(this WebApplication app)
    {
        app.MapGet("/api", Api.Get);



        app.MapGet("/api/items", Api.Items.GetAll);

        app.MapGet("/api/items/{id}", Api.Items.Get);

        app.MapPost("/api/items", Api.Items.Post);

        app.MapPut("/api/items", Api.Items.Put);

        app.MapDelete("/api/items/{id}", Api.Items.Delete);



        app.MapGet("/api/relays", Api.Relays.GetAll);

        app.MapGet("/api/relays/{id}", Api.Relays.Get);

        app.MapPost("/api/relays", Api.Relays.Post);

        app.MapPut("/api/relays", Api.Relays.Put);

        app.MapDelete("/api/relays/{id}", Api.Relays.Delete);
    }
}
