﻿namespace Glutspeicher.Server.Middleware;

public class NoCacheMiddleware(RequestDelegate next)
{
    readonly RequestDelegate next = next;

    public async Task Invoke(HttpContext httpContext)
    {
        var response = httpContext.Response;

        response.OnStarting(_ =>
        {
            var headers = response.Headers;
            headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            headers.Append("Pragma", "no-cache");
            headers.Append("Expires", "0");
            return Task.CompletedTask;
        }, state: null);

        await next.Invoke(httpContext);
    }
}
