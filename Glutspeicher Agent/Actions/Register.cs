using System.Drawing;
using System.Windows.Forms;

namespace Glutspeicher.Agent;

public class Register
{
    public void Run()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();

        var dialog = new Dialog { Text = nameof(Register) };

        dialog.AddButton($"Register", 100, Color.DarkGreen).Click += (sender, e) =>
        {
            dialog.SetInvisible();
            Utils.RegisterGlutspeicherAgentLink();
            dialog.Close();
        };

        dialog.AddButton($"Unregister", 100, Color.DarkRed).Click += (sender, e) =>
        {
            dialog.SetInvisible();
            Utils.UnregisterGlutspeicherAgentLink();
            dialog.Close();
        };

        dialog.BeforeShow();
        dialog.ShowDialog();
        dialog.Dispose();
    }
}
