using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using Tausi.NativeWindow;

namespace Glutspeicher.Client;

public partial class AutoType
{
    public string title;
    public JArray text;

    public void Run()
    {
        ShowDialog(
            title,
            () => PerformTextPart(0, 2),
            () => PerformTextPart(0, 1),
            () => PerformTextPart(1, 1)
        );
    }

    public static void ShowDialog(string text, params Action[] actions)
    {
        using var dialog = new Window();

        var followMouse = new FollowMouse(dialog)
        {
            FollowMouseSpace = new(200, 300)
        };

        var rowLayout = new RowLayout(dialog)
        {
            Title = text
        };

        var button1 = new Button
        {
            Text = "Username & Password",
            BackgroundColor = Color.DarkOliveGreen
        };
        button1.Click += (_, _) =>
        {
            followMouse.Pause();
            dialog.Dispose();
            actions.FirstOrDefault()?.Invoke();
        };
        dialog.Add(button1);

        rowLayout.NextRow();

        var button2 = new Button
        {
            Width = 100,
            Text = "Username",
            BackgroundColor = Color.DarkSlateBlue
        };
        button2.Click += (_, _) =>
        {
            followMouse.Pause();
            actions.Skip(1).FirstOrDefault()?.Invoke();
            followMouse.Resume();
        };
        dialog.Add(button2);

        var button3 = new Button
        {
            Width = 100,
            Text = "Password",
            BackgroundColor = Color.DarkSlateBlue
        };
        button3.Click += (_, _) =>
        {
            followMouse.Pause();
            actions.Skip(2).FirstOrDefault()?.Invoke();
            followMouse.Resume();
        };
        dialog.Add(button3);

        var button4 = new Button
        {
            Width = 70,
            Text = "Close"
        };
        button4.Click += (_, _) =>
        {
            followMouse.Pause();
            dialog.Dispose();
        };
        dialog.Add(button4);

        dialog.ShowDialog();
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
