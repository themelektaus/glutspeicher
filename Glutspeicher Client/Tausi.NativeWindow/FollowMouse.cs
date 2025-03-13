using System;
using System.Drawing;
using Vanara.PInvoke;

namespace Tausi.NativeWindow;

public class FollowMouse
{
    public Point FollowMouseSpace { get; init; }

    Point currentPosition;

    bool paused;

    public FollowMouse(Window window)
    {
        window.Draggable = false;

        window.OnAfterCreateWindow += Window_OnAfterCreateWindow;
        window.OnCreateWorker += Window_OnCreateWorker;
        window.OnUpdate += Window_OnUpdate;
    }

    public void Pause()
    {
        paused = true;
    }

    public void Resume()
    {
        paused = false;
    }

    void Window_OnAfterCreateWindow(object sender, EventArgs e)
    {
        var window = sender as Window;

        currentPosition = window.Position;
    }

    void Window_OnCreateWorker(object sender, Window.CreateWorkerEventArgs e)
    {
        var window = sender as Window;

        e.Worker.ProgressChanged += (_, _) =>
        {
            window.Position = currentPosition;
        };
    }

    void Window_OnUpdate(object sender, Window.UpdateEventArgs e)
    {
        if (paused)
        {
            return;
        }

        var window = sender as Window;

        var targetPosition = currentPosition;

        User32.GetCursorPos(out var cursor);
        var cursorX = cursor.X;
        var cursorY = cursor.Y;

        if (cursorX - currentPosition.X < -FollowMouseSpace.X)
        {
            targetPosition.X = cursorX + FollowMouseSpace.X;
        }
        else if (cursorX - currentPosition.X - window.Width > FollowMouseSpace.X)
        {
            targetPosition.X = cursorX - window.Width - FollowMouseSpace.X;
        }

        if (cursorY - currentPosition.Y < -FollowMouseSpace.Y)
        {
            targetPosition.Y = cursorY + FollowMouseSpace.Y;
        }
        else if (cursorY - currentPosition.Y - window.Height > FollowMouseSpace.Y)
        {
            targetPosition.Y = cursorY - window.Height - FollowMouseSpace.Y;
        }

        if (currentPosition != targetPosition)
        {
            PointF delta = new(
                targetPosition.X - currentPosition.X,
                targetPosition.Y - currentPosition.Y
            );

            if (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
            {
                currentPosition = new(
                    delta.X < 0
                        ? (int) Math.Floor(currentPosition.X + delta.X / 5)
                        : (int) Math.Ceiling(currentPosition.X + delta.X / 5),
                    delta.Y < 0
                        ? (int) Math.Floor(currentPosition.Y + delta.Y / 5)
                        : (int) Math.Ceiling(currentPosition.Y + delta.Y / 5)
                );

                e.Progress = 0;
            }
        }
    }
}
