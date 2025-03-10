using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Glutspeicher.Client;

public partial class AutoType
{
    public string title;
    public JArray text;

    public void Run()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        var processesCount = Process.GetProcessesByName(assemblyName)
            .Where(process =>
            {
                if (ProcessCommandLine.Retrieve(process, out var commandLine) == 0)
                {
                    var args = Utils.EnumerateArgs(commandLine);

                    if (Utils.ReadGlutLink(args)?.TryGetValue("type", out var type) ?? false)
                    {
                        if (type == nameof(AutoType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            })
            .Count();

        if (processesCount > 3)
        {
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();

        var dialog = new Dialog(Math.Max(0, processesCount - 1))
        {
            Text = title ?? string.Empty,
            followMouseSpace = new(200, 300)
        };

        var button1 = dialog.AddButton("Username && Password", 0, Color.DarkOliveGreen);
        var button2 = dialog.AddButton("Username", 100, Color.DarkSlateBlue);
        var button3 = dialog.AddButton("Password", 100, Color.DarkSlateBlue);
        var button4 = dialog.AddButton("Cancel", 70, Color.DimGray);

        button1.Click += (sender, e) =>
        {
            dialog.SetInvisible();
            PerformTextPart(0, 2);
            dialog.Close();
        };

        button2.Click += (sender, e) =>
        {
            PerformTextPart(0, 1);
        };

        button3.Click += (sender, e) =>
        {
            PerformTextPart(1, 1);
        };

        button4.Click += (sender, e) =>
        {
            dialog.SetInvisible();
            dialog.Close();
        };

        dialog.BeforeShow();
        dialog.ShowDialog();
        dialog.Dispose();
    }

    void PerformTextPart(int index, int count)
    {
        PerformIntoCurrentWindow(
            GetTextPartEncoded(index, count)
        );
    }

    string GetTextPartEncoded(int index, int count)
    {
        var lines = (text ?? [])
            .Skip(index)
            .Select(x => x.ToString())
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(Escape)
            .Take(count);

        return Encode(string.Join('\t', lines) + '\n');
    }
}
