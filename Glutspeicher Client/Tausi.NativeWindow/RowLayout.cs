using System;
using System.Collections.Generic;
using System.Drawing;

namespace Tausi.NativeWindow;

public abstract class RowLayout
{
    public int Padding { get; set; }

    int currentRowIndex;

    readonly Dictionary<Control, ControlInfo> controlInfos = [];
    struct ControlInfo
    {
        public int rowIndex;
    }

    public void NextRow()
    {
        currentRowIndex++;
    }

    protected void Register(Control control)
    {
        controlInfos.Add(control, new() { rowIndex = currentRowIndex });
    }

    protected void Apply(out Size size)
    {
        var x = Padding;
        var y = Padding;

        var rowIndex = -1;
        var width = 0;
        var height = 0;
        var rowHeight = 0;

        foreach (var (control, controlInfo) in controlInfos)
        {
            if (rowIndex != controlInfo.rowIndex)
            {
                rowIndex = controlInfo.rowIndex;
                x = Padding;
                y += Padding + rowHeight;
                rowHeight = 0;
            }

            control.X = x;
            control.Y = y;

            x += control.Width + Padding;

            width = Math.Max(height, x);
            height = Math.Max(height, y + control.Height + Padding);
            rowHeight = Math.Max(rowHeight, control.Height);
        }

        width += Padding - Padding;

        foreach (var (control, _) in controlInfos)
        {
            if (control.Width == 0)
            {
                control.Width = width - Padding * 2;
            }
        }

        size = new(width, height);
    }
}
