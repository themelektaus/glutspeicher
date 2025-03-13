using System;
using System.Drawing;

namespace Tausi.NativeWindow;

public class FollowMouse
{
    public Point FollowMouseSpace { get; init; }

    Point currentLocation;

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
        var frame = sender as Frame;

        currentLocation = frame.Location;
    }

    void Window_OnCreateWorker(object sender, Window.CreateWorkerEventArgs e)
    {
        var frame = sender as Frame;

        e.Worker.ProgressChanged += (_, _) =>
        {
            frame.Location = currentLocation;
        };
    }

    void Window_OnUpdate(object sender, Window.UpdateEventArgs e)
    {
        if (paused)
        {
            return;
        }

        var frame = sender as Frame;

        var targetLocation = currentLocation;

        User32.GetCursorPos(out var cursor);
        var cursorX = cursor.X;
        var cursorY = cursor.Y;

        if (cursorX - currentLocation.X < -FollowMouseSpace.X)
        {
            targetLocation.X = cursorX + FollowMouseSpace.X;
        }
        else if (cursorX - currentLocation.X - frame.Width > FollowMouseSpace.X)
        {
            targetLocation.X = cursorX - frame.Width - FollowMouseSpace.X;
        }

        if (cursorY - currentLocation.Y < -FollowMouseSpace.Y)
        {
            targetLocation.Y = cursorY + FollowMouseSpace.Y;
        }
        else if (cursorY - currentLocation.Y - frame.Height > FollowMouseSpace.Y)
        {
            targetLocation.Y = cursorY - frame.Height - FollowMouseSpace.Y;
        }

        if (currentLocation != targetLocation)
        {
            PointF delta = new(
                targetLocation.X - currentLocation.X,
                targetLocation.Y - currentLocation.Y
            );

            if (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
            {
                currentLocation = new(
                    delta.X < 0
                        ? (int) Math.Floor(currentLocation.X + delta.X / 5)
                        : (int) Math.Ceiling(currentLocation.X + delta.X / 5),
                    delta.Y < 0
                        ? (int) Math.Floor(currentLocation.Y + delta.Y / 5)
                        : (int) Math.Ceiling(currentLocation.Y + delta.Y / 5)
                );

                e.Progress = 0;
            }
        }
    }
}
