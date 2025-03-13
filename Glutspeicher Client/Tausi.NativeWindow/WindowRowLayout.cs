namespace Tausi.NativeWindow;

public class WindowRowLayout : RowLayout
{
    readonly Window window;

    public WindowRowLayout(Window window)
    {
        Padding = 3;

        this.window = window;

        window.OnAddControl += (_, e) => Register(e.Control);

        window.OnBeforeCreateWindow += (_, _) =>
        {
            Apply(out var size);
            window.Size = size;
        };
    }

    public void AddTitleRow(string text)
    {
        window.AddControl(new Label
        {
            Text = text,
            Bold = true
        });

        NextRow();
    }
}
