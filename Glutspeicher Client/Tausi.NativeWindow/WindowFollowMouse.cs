namespace Tausi.NativeWindow;

public class WindowFollowMouse : FollowMouse<Window>
{
    public WindowFollowMouse(Window window) : base(window)
    {
        window.Draggable = false;
        window.OnUpdate += (_, _) => Update();
    }
}
