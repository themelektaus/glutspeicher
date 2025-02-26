using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glutspeicher.Agent;

public static class Utils
{
    static Utils()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static void RegisterGlutspeicherAgentLink()
    {
        Registry_User_AddScheme("glut", "Glutspeicher Agent Link", $"\"{Environment.ProcessPath}\" %1");
        Registry_User_SetValue(@"SOFTWARE\Microsoft\Terminal Server Client", "AuthenticationLevelOverride", 0);
    }

    public static void UnregisterGlutspeicherAgentLink()
    {
        Registry_User_DeleteScheme("glut");
        Registry_User_DeleteValue(@"SOFTWARE\Microsoft\Terminal Server Client", "AuthenticationLevelOverride");
    }

    static void Registry_User_AddScheme(string name, string title, string command)
    {
        Registry_User_SetValue(@$"SOFTWARE\Classes\{name}", title);
        Registry_User_SetValue(@$"SOFTWARE\Classes\{name}\shell\open\command", command);
        Registry_User_SetValue(@$"SOFTWARE\Classes\{name}", "URL Protocol", string.Empty);
    }

    static void Registry_User_SetValue(string path, object value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path);
        key.SetValue(string.Empty, value);
    }

    static void Registry_User_SetValue(string path, string name, object value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path);
        key.SetValue(name, value);
    }

    static void Registry_User_DeleteScheme(string name)
    {
        Registry_User_DeleteKey(@$"SOFTWARE\Classes\{name}");
    }

    static void Registry_User_DeleteKey(string path)
    {
        Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
    }

    static void Registry_User_DeleteValue(string path, string name)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path);
        key.DeleteValue(name, throwOnMissingValue: false);
    }

    public static async Task RunAsync(string fileName, string arguments, bool hidden)
    {
        using var process = new Process();
        process.StartInfo = new()
        {
            FileName = fileName,
            Arguments = arguments,
            WindowStyle = hidden
                    ? ProcessWindowStyle.Hidden
                    : ProcessWindowStyle.Normal
        };
        process.Start();
        await process.WaitForExitAsync();
    }

    public static Dictionary<string, dynamic> ReadGlutLink(IEnumerable<string> args)
    {
        var uri = args.FirstOrDefault(x => x.StartsWith("glut://"));

        if (uri is null)
        {
            return null;
        }

        var jsonData = Convert.FromBase64String(uri[7..].TrimEnd("/"));
        var json = Encoding.GetEncoding(1252).GetString(jsonData);
        return JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
    }

    //Source: https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/64236441#64236441
    public static IEnumerable<string> EnumerateArgs(string commandLine)
    {
        var result = new StringBuilder();

        var quoted = false;
        var escaped = false;
        var started = false;
        var allowcaret = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            var chr = commandLine[i];

            if (chr == '^' && !quoted)
            {
                if (allowcaret)
                {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                    allowcaret = false;
                }
                else if (i + 1 < commandLine.Length && commandLine[i + 1] == '^')
                {
                    allowcaret = true;
                }
                else if (i + 1 == commandLine.Length)
                {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                }
            }
            else if (escaped)
            {
                result.Append(chr);
                started = true;
                escaped = false;
            }
            else if (chr == '"')
            {
                quoted = !quoted;
                started = true;
            }
            else if (chr == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
            {
                escaped = true;
            }
            else if (chr == ' ' && !quoted)
            {
                if (started) yield return result.ToString();
                result.Clear();
                started = false;
            }
            else
            {
                result.Append(chr);
                started = true;
            }
        }

        if (started)
        {
            yield return result.ToString();
        }
    }
}
