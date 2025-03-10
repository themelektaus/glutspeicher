using System.Windows.Forms;

namespace Glutspeicher.Client;

public class Label : System.Windows.Forms.Label
{
    public Label()
    {
        Enabled = false;
        Padding = new(0);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter
        );
    }
}
