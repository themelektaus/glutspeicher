using BitwardenAgent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Glutspeicher.Agent;

public class Dialog : Form
{
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= 0x00000008 | 0x08000000;
            return createParams;
        }
    }

    protected override void DefWndProc(ref Message m)
    {
        if (m.Msg == 0x21)
        {
            m.Result = 0x0003;
            return;
        }

        base.DefWndProc(ref m);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (followMouseSpace == Point.Empty)
        {
            if (m.Msg == 0x84 && (int) m.Result == 0x1)
            {
                m.Result = 0x2;
            }
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        AutoType_NativeMethods.LoseFocus(Handle);
    }

    public readonly float scale;

    int buttonX;
    int buttonY;

    readonly int padding;
    readonly int buttonSpacing;

    readonly Font titleFont;
    readonly Font buttonFont;
    readonly int buttonHeight;
    readonly List<Button> buttons = [];

    readonly int yOffset;

    BackgroundWorker worker;
    Point currentLocation;

    public PointF viewportPosition = new(.5f, .5f);
    public Point followMouseSpace = new();

    public Dialog(int index = 0)
    {
        TopMost = true;
        TopLevel = true;
        ControlBox = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(20, 20, 20);

        scale = DeviceDpi / 96f;

        padding = (int) (4 * scale);
        buttonSpacing = padding;
        buttonHeight = (int) (32 * scale);

        buttonX = padding;
        buttonY = buttonSpacing + buttonHeight;

        using var fontCollection = new PrivateFontCollection();
        fontCollection.AddFontFile("rubik.ttf");
        titleFont = new(fontCollection.Families[0], 13);
        buttonFont = new(fontCollection.Families[0], 12);

        yOffset = (buttonY + buttonHeight * 2 + padding * 3) * (index == 2 ? -1 : index);
    }

    public void AddRow()
    {
        buttonX = padding;
        buttonY += buttonHeight + padding;
    }

    public Button AddButton(string text, int width, Color color)
    {
        var button = new Button
        {
            Text = text,
            Padding = new(0),
            Location = new(buttonX, buttonY),
            Size = new((int) (width * scale), buttonHeight),
            BackColor = color,
            ForeColor = Color.White,
            TabStop = false,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 0,
            },
            Font = buttonFont
        };

        buttonX += button.Width + buttonSpacing;

        if (width == 0)
        {
            AddRow();
        }

        buttons.Add(button);

        return button;
    }

    public void SetInvisible()
    {
        Opacity = 0;

        foreach (var button in buttons)
        {
            button.Visible = false;
        }
    }

    public void BeforeShow()
    {
        var width = buttons.Max(x => x.Location.X + x.Width);

        Size = new(width + padding, buttonSpacing + buttonY + buttonHeight);

        Controls.Add(new Label
        {
            Text = Text,
            Location = new(padding, padding),
            Size = new(width - padding, buttonHeight),
            ForeColor = Color.White,
            Font = titleFont
        });

        foreach (var button in buttons)
        {
            if (button.Width == 0)
            {
                button.Width = width - padding;
            }

            Controls.Add(button);
        }

        var area = Screen.PrimaryScreen.WorkingArea;

        var xRange = new Point(area.Left + Size.Width, area.Right - Size.Width * 2);
        var yRange = new Point(area.Top + Size.Height, area.Bottom - Size.Height * 2);

        Location = new(
            (int) (xRange.X + (xRange.Y - xRange.X) * viewportPosition.X),
            (int) (yRange.X + (yRange.Y - yRange.X) * viewportPosition.Y)
        );

        if (followMouseSpace == Point.Empty)
        {
            Size = new(
                Size.Width + (Width - ClientRectangle.Width),
                Size.Height + (Height - ClientRectangle.Height)
            );

            return;
        }

        Opacity = 0;

        var visible = 0;

        worker = new() { WorkerReportsProgress = true };

        worker.ProgressChanged += (sender, e) =>
        {
            Location = new(currentLocation.X, currentLocation.Y + yOffset);
        };

        worker.DoWork += (sender, e) =>
        {
            while (!Visible)
            {
                Thread.Sleep(20);
            }

            while (Visible)
            {
                if (visible == 0)
                {
                    visible = 1;
                    Invoke(() =>
                    {
                        currentLocation = Location;
                        Location = new(currentLocation.X, currentLocation.Y + yOffset);
                    });
                    Thread.Sleep(20);
                    continue;
                }

                if (visible == 1)
                {
                    visible = 2;
                    Invoke(() => Opacity = 1);
                    Thread.Sleep(20);
                    continue;
                }

                var targetLocation = currentLocation;

                if (followMouseSpace.X > 0)
                {
                    if (Cursor.Position.X - currentLocation.X < -followMouseSpace.X)
                    {
                        targetLocation.X = Cursor.Position.X + followMouseSpace.X;
                    }
                    else if (Cursor.Position.X - currentLocation.X - Size.Width > followMouseSpace.X)
                    {
                        targetLocation.X = Cursor.Position.X - Size.Width - followMouseSpace.X;
                    }
                }

                if (followMouseSpace.Y > 0)
                {
                    if (Cursor.Position.Y - currentLocation.Y < -followMouseSpace.Y)
                    {
                        targetLocation.Y = Cursor.Position.Y + followMouseSpace.Y;
                    }
                    else if (Cursor.Position.Y - currentLocation.Y - Size.Height > followMouseSpace.Y)
                    {
                        targetLocation.Y = Cursor.Position.Y - Size.Height - followMouseSpace.Y;
                    }
                }

                if (currentLocation != targetLocation)
                {
                    float deltaX = targetLocation.X - currentLocation.X;
                    float deltaY = targetLocation.Y - currentLocation.Y;

                    if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1)
                    {
                        currentLocation = new Point(
                            deltaX < 0
                                ? (int) Math.Floor(currentLocation.X + deltaX / 5)
                                : (int) Math.Ceiling(currentLocation.X + deltaX / 5),
                            deltaY < 0
                                ? (int) Math.Floor(currentLocation.Y + deltaY / 5)
                                : (int) Math.Ceiling(currentLocation.Y + deltaY / 5)
                        );

                        worker.ReportProgress(0);
                    }
                }

                Thread.Sleep(12);
            }
        };

        worker.RunWorkerAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            worker?.Dispose();
        }

        base.Dispose(disposing);
    }
}
