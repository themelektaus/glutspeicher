using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Glutspeicher.Agent;

public class Web : Relay
{
    public string name;
    public string uri;

    protected override async Task OnRun(RelaySession relaySession)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();

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
            string port = this.port == 0 ? string.Empty : $":{this.port}";
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
            var dialog = new Dialog { Text = name ?? string.Empty, viewportPosition = new(.5f, 1) };
            dialog.AddButton($"Disconnect", (int) (Screen.PrimaryScreen.WorkingArea.Width / dialog.scale) / 8, Color.DarkRed).Click += (sender, e) =>
            {
                dialog.SetInvisible();
                dialog.Close();
            };

            dialog.BeforeShow();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        await Task.CompletedTask;
    }
}
