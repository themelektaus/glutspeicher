using Microsoft.AspNetCore.Routing;
using System.IO;
using System.Text;

namespace Glutspeicher.Server.Mapping;

public static partial class Api
{
    public static IApiResult Get(HttpContext httpContext, IEnumerable<EndpointDataSource> sources)
    {
        var assemblyName = Assembly.GetName();

        var endpoints = GetEndpoints(sources);

        var methodLength = endpoints.Max(x => (x.method as string).Length) + 2;
        var pathLength = endpoints.Max(x => (x.path as string).Length) + 2;

        return ApiResult.Ok(
            data: new
            {
                assemblyName.Name,
                assemblyName.Version,
                baseUrl = httpContext.GetBaseUrl(),
                host = httpContext.Request.Headers.Host.ToString(),
                endpointAsLines = endpoints.Select(x => string.Format("{0," + methodLength + "}", x.method) + "  " + string.Format("{0,-" + pathLength + "}", x.path)),
                endpoints
            }
        );
    }

    static List<dynamic> GetEndpoints(IEnumerable<EndpointDataSource> sources)
    {
        var endpoints = new List<dynamic>();

        foreach (var endpoint in sources.SelectMany(x => x.Endpoints))
        {
            string path = default;

            if (endpoint is RouteEndpoint routeEndpoint)
            {
                path = routeEndpoint.RoutePattern.RawText;
            }

            var methods = endpoint.Metadata
                .OfType<HttpMethodMetadata>()
                .FirstOrDefault()?.HttpMethods;

            foreach (var method in methods)
            {
                endpoints.Add(new { method, path, });
            }
        }

        return endpoints.OrderBy(x => x.path).ThenBy(x => x.method).ToList();
    }

    public static IResult RebuildDatabase(LiteDbContext liteDbContext)
    {
        return Results.Ok(liteDbContext.Database.Rebuild());
    }

    public static IResult DownloadDatabase()
    {
        return Results.File(
            LiteDbContext.Read(),
            "application/octet-stream",
            $"{Path.GetFileNameWithoutExtension(LiteDbContext.path)} {Now:yyyy-MM-dd HH-mm-ss}.litedb"
        );
    }

    static readonly Csv.CsvOptions csvOptions = new()
    {
        Separator = Csv.SeparatorType.Semicolon,
        QuoteMode = Csv.QuoteMode.All
    };

    static byte[] ToCsv<T>(IEnumerable<T> data)
    {
        return Csv.CsvSerializer.Serialize(data, csvOptions);
    }

    static string ToCsvString<T>(IEnumerable<T> data)
    {
        return Encoding.UTF8.GetString(ToCsv(data));
    }

    static T[] FromCsv<T>(byte[] data)
    {
        return Csv.CsvSerializer.Deserialize<T>(data, csvOptions);
    }

    static T[] FromCsv<T>(string data)
    {
        return Csv.CsvSerializer.Deserialize<T>(data, csvOptions);
    }
}
