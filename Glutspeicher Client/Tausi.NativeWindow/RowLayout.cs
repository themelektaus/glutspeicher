using System;
using System.Collections.Generic;

namespace Tausi.NativeWindow;

public class RowLayout
{
    public string Title { get; set; }

    public int Padding { get; set; } = 3;

    int rowIndex;

    readonly Dictionary<Control, ControlInfo> controlInfos = [];
    struct ControlInfo
    {
        public int rowIndex;
    }

    public RowLayout(Window window)
    {
        window.OnAdd += Window_OnAdd;
        window.OnBeforeCreateWindow += Window_OnBeforeCreateWindow;
    }

    public void NextRow()
    {
        rowIndex++;
    }

    void Window_OnAdd(object sender, Window.AddEventArgs e)
    {
        controlInfos.Add(e.Control, new() { rowIndex = rowIndex });
    }

    void Window_OnBeforeCreateWindow(object sender, EventArgs e)
    {
        var window = sender as Window;

        var x = Padding;
        var y = Padding / 2;

        var titleLabel = new Label
        {
            X = x,
            Y = y,
            Text = Title,
            Bold = true,
        };

        y -= Padding / 2;

        var rowIndex = -1;
        var width = 0;

        foreach (var (control, controlInfo) in controlInfos)
        {
            if (rowIndex != controlInfo.rowIndex)
            {
                rowIndex = controlInfo.rowIndex;
                x = Padding;
                y += Padding + titleLabel.Height;
            }

            control.X = x;
            control.Y = y;
            
            x += control.Width + Padding;

            if (width < x)
            {
                width = x;
            }
        }

        width += Padding - Padding;

        foreach (var (control, _) in controlInfos)
        {
            if (control.Width == 0)
            {
                control.Width = width - Padding * 2;
            }
        }

        window.Size = new(width, y + titleLabel.Height + Padding);

        titleLabel.Width = width - Padding * 2;

        window.Add(titleLabel);
    }
}
