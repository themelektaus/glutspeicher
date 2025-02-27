using LibSassHost;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Glutspeicher.Server.Middleware;

public class FrontendMiddleware(RequestDelegate next)
{
    readonly RequestDelegate next = next;

    readonly static FileExtensionContentTypeProvider provider = new();

    public async Task Invoke(HttpContext context)
    {
        var response = context.Response;

        if (response.StatusCode == 200 && !response.HasStarted)
        {
            var path = context.Request.Path.ToString();

            if (path == "/")
            {
                path = "/index.html";
            }
            else if (!path.TrimStart('/').Contains('/'))
            {
                if (path.EndsWith(".csv"))
                {
                    path = $"/static/exports{path}";
                }
            }

            if (
                path == "/index.html" ||
                path == "/favicon.ico" ||
                path.StartsWith("/static/")
            )
            {
                var isHTML = path.EndsWith(".html");
                var isCSS = path.EndsWith(".css");
                var isSCSS = path.EndsWith(".scss");
                var isJS = path.EndsWith(".js");
                var isCSV = path.EndsWith(".csv");

                if (isHTML || isCSS || isSCSS || isJS || isCSV)
                {
                    var content = await File.ReadAllTextAsync(
                        GetWwwRootPath(path)
                    );

                    if (!isCSV)
                    {
                        content = Regex.Replace(
                            content,
                            isHTML
                                ? @"\<\!\-\- include (.*) \-\-\>"
                                : (
                                    isCSS
                                        ? @"\/\* include (.*) \*\/"
                                        : @"\/\/ include (.*)"
                                ),
                            x =>
                            {
                                var basePath = GetWwwRootPath(
                                    Path.GetDirectoryName(path.TrimStart('/'))
                                );

                                IEnumerable<string> files;

                                var includePath = x.Groups[1].Value.Trim();

                                if (includePath.Contains('*'))
                                {
                                    var includePathParts = includePath.Split('*');

                                    files = Directory.EnumerateFiles(
                                        Path.Combine(basePath, includePathParts[0]),
                                        "*.*",
                                        SearchOption.AllDirectories
                                    ).Where(x => x.EndsWith(includePathParts[1]));

                                }
                                else
                                {
                                    files = [Path.Combine(basePath, includePath)];
                                }

                                return string.Join(
                                    Environment.NewLine,
                                    files.Select(File.ReadAllText)
                                );
                            },
                            RegexOptions.Multiline
                        );
                    }

                    if (isSCSS)
                    {
                        content = SassCompiler.Compile(content).CompiledContent;
                    }

                    if (isHTML)
                    {
                        context.Response.ContentType = "text/html";
                    }
                    else if (isCSS || isSCSS)
                    {
                        context.Response.ContentType = "text/css";
                    }
                    else if (isJS)
                    {
                        context.Response.ContentType = "text/javascript";

#if DEBUG
                        content = $"DEBUG=true;{content}";
#else
                        content = $"DEBUG=false;{content}";
#endif
                        content = $"SERVER_VERSION=`{ServerVersion}`;AGENT_VERSION=`{AgentVersion}`;{content}";
                    }
                    else if (isCSV)
                    {
                        context.Response.ContentType = "text/plain; charset=utf-8";
                    }

                    var data = Encoding.UTF8.GetBytes(content);

                    using var stream = new MemoryStream(data);
                    await stream.CopyToAsync(context.Response.Body);

                    return;
                }

                await SendFileAsync(context, path);
                return;
            }
        }

        await next.Invoke(context);
    }

    static async Task SendFileAsync(HttpContext context, string path)
    {
        var file = GetWwwRootPath(path);

        if (!File.Exists(file))
        {
            context.Response.StatusCode = 404;
            return;
        }

        if (provider.TryGetContentType(file, out var contentType))
        {
            context.Response.ContentType = contentType;
        }

        await context.Response.SendFileAsync(file);
    }

    static string GetWwwRootPath(string path)
    {
        return Path.Combine(
            "wwwroot",
            Path.Combine(
                Uri.UnescapeDataString(path).TrimStart('/').Split('/')
            )
        );
    }
}
