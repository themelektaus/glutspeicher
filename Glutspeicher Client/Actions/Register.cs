using System.Drawing;
using Tausi.NativeWindow;

namespace Glutspeicher.Client;

public class Register
{
    public void Run()
    {
        ShowDialog();
    }

    public static void ShowDialog()
    {
        using var dialog = new Window();

        var rowLayout = new RowLayout(dialog)
        {
            Title = $"{nameof(Glutspeicher)} {nameof(Client)}"
        };

        var registerButton = new Button
        {
            Width = 100,
            Text = "Register",
            BackgroundColor = Color.DarkGreen
        };
        registerButton.Click += (_, _) =>
        {
            dialog.Dispose();
            Utils.RegisterGlutspeicherClientLink();
        };
        dialog.Add(registerButton);

        var unregisterButton = new Button
        {
            Width = 100,
            Text = "Unregister",
            BackgroundColor = Color.FromArgb(90, 40, 10)
        };
        unregisterButton.Click += (_, _) =>
        {
            dialog.Dispose();
            Utils.UnregisterGlutspeicherClientLink();
        };
        dialog.Add(unregisterButton);

        dialog.ShowDialog();
    }
}
