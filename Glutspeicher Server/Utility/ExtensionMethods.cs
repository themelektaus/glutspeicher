using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Glutspeicher.Server.Utility;

public static class ExtensionMethods
{
    public static string GetQueryString(
        this HttpContext httpContext,
        string key,
        string defaultValue
    )
    {
        _ = httpContext.TryGetQueryString(key, out var value);
        return (value ?? string.Empty) == string.Empty ? defaultValue : value;
    }

    public static bool TryGetQueryString(
        this HttpContext httpContext,
        string key,
        out string value
    )
    {
        if (httpContext.Request.Query.TryGetValue(key, out var stringValue))
        {
            value = stringValue;
            return true;
        }

        value = default;
        return false;
    }

    public static bool GetQueryBoolean(
        this HttpContext httpContext,
        string key,
        bool defaultValue
    )
    {
        return httpContext.TryGetQueryBoolean(key, out var value)
            ? value
            : defaultValue;
    }

    public static bool TryGetQueryBoolean(
        this HttpContext httpContext,
        string key,
        out bool value
    )
    {
        if (httpContext.TryGetQueryString(key, out var stringValue))
        {
            if (stringValue.Like("true", "on", "yes", "active", "enabled"))
            {
                value = true;
                return true;
            }

            if (int.TryParse(stringValue, out var intValue))
            {
                value = intValue != 0;
                return true;
            }

            value = false;
            return true;
        }

        value = default;
        return false;
    }

    public static int GetQueryInteger(
        this HttpContext httpContext,
        string key,
        int defaultValue
    )
    {
        return httpContext.TryGetQueryInteger(key, out var value)
            ? value
            : defaultValue;
    }

    public static bool TryGetQueryInteger(
        this HttpContext httpContext,
        string key,
        out int value
    )
    {
        if (httpContext.TryGetQueryString(key, out var stringValue))
        {
            if (int.TryParse(stringValue, out var intValue))
            {
                value = intValue;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static DateTime? GetQueryDateTime(
        this HttpContext httpContext,
        string key,
        DateTime? defaultValue
    )
    {
        return httpContext.TryGetQueryDateTime(key, out var value)
            ? value
            : defaultValue;
    }

    public static bool TryGetQueryDateTime(
        this HttpContext httpContext,
        string key,
        out DateTime? value
    )
    {
        if (httpContext.TryGetQueryString(key, out var stringValue))
        {
            if (DateTime.TryParse(stringValue, out var dateTimeValue))
            {
                value = dateTimeValue;
                return true;
            }
        }

        value = null;
        return false;
    }

    public static (int offset, int? limit) GetLimit(
       this HttpContext httpContext,
       int defaultOffset = 0,
       int defaultLimit = 25
    )
    {
        if (httpContext is null)
        {
            return (0, null);
        }

        return (
            httpContext.GetQueryInteger("offset", defaultOffset),
            Math.Max(0, httpContext.GetQueryInteger("limit", defaultLimit))
        );
    }

    public static string GetBaseUrl(this HttpContext httpContext)
    {
        var request = httpContext.Request;

        var scheme = request.Headers.TryGetValue("X-Forwarded-Proto", out var proto)
            ? (proto.ToString() ?? string.Empty) : string.Empty;

        if (scheme == string.Empty)
        {
            scheme = request.Scheme;
        }

        return $"{scheme}://{request.Host}";
    }

    public static bool Like(this string a, params string[] others)
    {
        var comparisonType = StringComparison.InvariantCultureIgnoreCase;

        foreach (var b in others)
        {
            if (string.Equals(a, b, comparisonType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsInside(this string a, params string[] others)
    {
        var comparisonType = StringComparison.InvariantCultureIgnoreCase;

        foreach (var b in others)
        {
            if ((b ?? string.Empty).Contains(a, comparisonType))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetHeaderValue(this HttpContext httpContext, string key)
    {
        if (httpContext.Request.Headers.TryGetValue(key, out var value))
        {
            var trimmedValue = value.ToString().Trim();

            if (trimmedValue != string.Empty)
            {
                return trimmedValue;
            }
        }

        return string.Empty;
    }

    public static DateTime Truncate(this DateTime @this, TimeSpan kind)
    {
        if (kind == TimeSpan.Zero)
        {
            return @this;
        }

        if (@this == DateTime.MinValue || @this == DateTime.MaxValue)
        {
            return @this;
        }

        return @this.AddTicks(-(@this.Ticks % kind.Ticks));
    }

    public class ScopedInstance<T>(IServiceScope scope, T instance) : IDisposable where T : IDisposable
    {
        readonly IServiceScope scope = scope;

        public readonly T instance = instance;

        public void Dispose()
        {
            instance.Dispose();
            scope.Dispose();
        }
    }

    public static ScopedInstance<T> CreateScopedInstance<T>(this WebApplication app)
        where T : IDisposable
    {
        var scope = app.Services.CreateScope();

        var instance = ActivatorUtilities.CreateInstance<T>(
            scope.ServiceProvider
        );

        return new(scope, instance);
    }
}
