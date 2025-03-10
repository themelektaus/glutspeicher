using System.Drawing;
using System.Windows.Forms;

namespace Glutspeicher.Client;

public class Register
{
    public void Run()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();

        var dialog = new Dialog { Text = $"{nameof(Glutspeicher)} {nameof(Client)}" };

        dialog.AddButton($"Register", 100, Color.DarkGreen).Click += (sender, e) =>
        {
            dialog.SetInvisible();
            Utils.RegisterGlutspeicherClientLink();
            dialog.Close();
        };

        dialog.AddButton($"Unregister", 100, Color.DarkRed).Click += (sender, e) =>
        {
            dialog.SetInvisible();
            Utils.UnregisterGlutspeicherClientLink();
            dialog.Close();
        };

        dialog.BeforeShow();
        dialog.ShowDialog();
        dialog.Dispose();
    }
}
