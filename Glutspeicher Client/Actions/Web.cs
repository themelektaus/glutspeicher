using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Tausi.NativeWindow;
using Vanara.PInvoke;

namespace Glutspeicher.Client;

public class Web : Relay
{
    public string name;
    public string uri;

    protected override async Task OnRun(RelaySession relaySession)
    {
        string url;

        if (relaySession is null)
        {
            if (string.IsNullOrEmpty(uri))
            {
                throw new($"{nameof(uri)} is null or empty");
            }

            url = uri;
        }
        else
        {
            var port = this.port == 0 ? string.Empty : $":{this.port}";
            url = $"{(string.IsNullOrEmpty(uri) ? "https" : uri).Split("://")[0]}://{hostname}{port}";
        }

        if (string.IsNullOrEmpty(webCommandLine))
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        else
        {
            var webCommandLine = this.webCommandLine;

            DirectoryInfo tempFolder = null;

            if (webCommandLine.Contains("{temp}", System.StringComparison.InvariantCultureIgnoreCase))
            {
                tempFolder = new(Path.Combine("Temp", DateTime.Now.Ticks.ToString()));
                if (tempFolder.Exists)
                {
                    tempFolder.Delete(recursive: true);
                }
                tempFolder.Create();

                webCommandLine = webCommandLine.Replace("{temp}", tempFolder.FullName, StringComparison.InvariantCultureIgnoreCase);
            }

            var process = Process.Start(new ProcessStartInfo("cmd", $"/c start /wait {webCommandLine} {url}")
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            await process.WaitForExitAsync();

            tempFolder?.Delete(recursive: true);
        }

        if (relaySession is not null)
        {
            ShowDialog(name);
        }

        await Task.CompletedTask;
    }

    public static void ShowDialog(string text)
    {
        var screenWidth = Window.UseDC(x => Gdi32.GetDeviceCaps(x, Gdi32.DeviceCap.HORZRES));

        using var dialog = new Window
        {
            ViewportPosition = new(.5f, 1)
        };

        var rowLayout = new RowLayout(dialog)
        {
            Title = text
        };

        var disconnectButton = new Button
        {
            Width = screenWidth / 8,
            Text = "Disconnect",
            BackgroundColor = Color.FromArgb(90, 40, 10)
        };
        disconnectButton.Click += (_, _) =>
        {
            dialog.Dispose();
        };
        dialog.Add(disconnectButton);

        dialog.ShowDialog();
    }
}
