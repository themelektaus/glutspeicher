using Microsoft.AspNetCore.Routing;

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
}
