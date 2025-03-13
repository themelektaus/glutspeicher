namespace Tausi.NativeWindow;

public abstract class Control : Frame
{
    public int Id { get; set; }

    public Window Owner { get; set; }

    public nint Handle { get; set; }

    public abstract void Create(Window window);
}
