using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Glutspeicher.Client;

// The log is only written to disk when an action failed, never holds credentials
// and is deleted again once Notepad was closed.
public static partial class ClientLog
{
    const string FILE_PREFIX = "glutspeicher-client-";
    const string FILE_SUFFIX = ".log";
    const string MASK = "***";

    // Notepad may hand the file over to an already running instance and exit at once.
    static readonly TimeSpan minShowTime = TimeSpan.FromSeconds(10);

    // Another client instance may be about to show its log right now.
    static readonly TimeSpan minCleanUpAge = TimeSpan.FromMinutes(1);

    static bool IsSecretName(string name)
    {
        return name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || name.Equals("text", StringComparison.OrdinalIgnoreCase);
    }

    // JSON of the action for the log: secret fields are masked,
    // their values are collected in "secrets" (for Scrub).
    public static string Describe(object instance, List<string> secrets)
    {
        var data = JObject.FromObject(instance);

        var properties = data
            .Descendants()
            .OfType<JProperty>()
            .Where(x => IsSecretName(x.Name))
            .ToList();

        foreach (var property in properties)
        {
            var values = property.Value is JContainer container
                ? container.Descendants().OfType<JValue>()
                : [property.Value as JValue];

            var strings = values
                .Select(x => x?.Value?.ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            if (strings.Count == 0)
            {
                continue;
            }

            secrets.AddRange(strings);
            property.Value = MASK;
        }

        foreach (var value in data.Descendants().OfType<JValue>().Where(x => x.Type == JTokenType.String).ToList())
        {
            value.Value = UriUserInfoRegex().Replace((string) value.Value, $"://{MASK}@");
        }

        return JsonConvert.SerializeObject(new
        {
            type = instance.GetType().Name,
            instance = data,
        },
        Formatting.Indented);
    }

    // Removes the secret values from any text (e.g. from an exception message).
    public static string Scrub(string text, IEnumerable<string> secrets)
    {
        foreach (var secret in secrets.Where(x => x.Length >= 4).Distinct().OrderByDescending(x => x.Length))
        {
            text = text.Replace(secret, MASK);
        }

        return text;
    }

    public static async Task ShowAsync(string text)
    {
        var file = Path.Combine(Path.GetTempPath(), $"{FILE_PREFIX}{Guid.NewGuid():N}{FILE_SUFFIX}");

        try
        {
            await File.WriteAllTextAsync(file, text);

            var stopwatch = Stopwatch.StartNew();

            await Utils.RunAsync("notepad", $"\"{file}\"", hidden: false);

            var remaining = minShowTime - stopwatch.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining);
            }
        }
        finally
        {
            TryDelete(file);
        }
    }

    // Removes logs left behind in %TEMP%: own ones (e.g. after a crash) and the ones of
    // older client versions, which wrote every call with all credentials to tmp*.tmp.
    public static void CleanUp()
    {
        try
        {
            var folder = new DirectoryInfo(Path.GetTempPath());
            var maxTime = DateTime.UtcNow - minCleanUpAge;

            foreach (var file in folder.EnumerateFiles($"{FILE_PREFIX}*{FILE_SUFFIX}"))
            {
                if (file.LastWriteTimeUtc < maxTime)
                {
                    TryDelete(file.FullName);
                }
            }

            foreach (var file in folder.EnumerateFiles("tmp*.tmp"))
            {
                if (IsLegacyLog(file))
                {
                    TryDelete(file.FullName);
                }
            }
        }
        catch
        {
            // Cleaning up must never stop the client.
        }
    }

    static bool IsLegacyLog(FileInfo file)
    {
        try
        {
            var buffer = new byte[128];
            int length;

            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                length = stream.Read(buffer, 0, buffer.Length);
            }

            return LegacyLogRegex().IsMatch(Encoding.UTF8.GetString(buffer, 0, length));
        }
        catch
        {
            return false;
        }
    }

    static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch
        {
            // Still in use or already gone: the next start tries again.
        }
    }

    [GeneratedRegex(@"^\s*\{\s*""type"":\s*""\w+"",\s*""instance"":")]
    private static partial Regex LegacyLogRegex();

    [GeneratedRegex(@"://[^/\s@]+@")]
    private static partial Regex UriUserInfoRegex();
}
