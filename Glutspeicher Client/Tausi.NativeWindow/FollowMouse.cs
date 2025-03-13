using System;
using System.Drawing;

namespace Tausi.NativeWindow;

public abstract class FollowMouse<T>(T frame) where T : Frame
{
    readonly T frame = frame;

    public Point FollowMouseSpace { get; init; }

    bool paused;

    public void Pause()
    {
        paused = true;
    }

    public void Resume()
    {
        paused = false;
    }

    protected void Update()
    {
        if (paused)
        {
            return;
        }

        var targetLocation = frame.Location;

        User32.GetCursorPos(out var cursor);
        var cursorX = cursor.X;
        var cursorY = cursor.Y;

        if (cursorX - frame.Location.X < -FollowMouseSpace.X)
        {
            targetLocation.X = cursorX + FollowMouseSpace.X;
        }
        else if (cursorX - frame.Location.X - frame.Width > FollowMouseSpace.X)
        {
            targetLocation.X = cursorX - frame.Width - FollowMouseSpace.X;
        }

        if (cursorY - frame.Location.Y < -FollowMouseSpace.Y)
        {
            targetLocation.Y = cursorY + FollowMouseSpace.Y;
        }
        else if (cursorY - frame.Location.Y - frame.Height > FollowMouseSpace.Y)
        {
            targetLocation.Y = cursorY - frame.Height - FollowMouseSpace.Y;
        }

        if (frame.Location != targetLocation)
        {
            PointF delta = new(
                targetLocation.X - frame.Location.X,
                targetLocation.Y - frame.Location.Y
            );

            if (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
            {
                frame.Location = new(
                    delta.X < 0
                        ? (int) Math.Floor(frame.Location.X + delta.X / 5)
                        : (int) Math.Ceiling(frame.Location.X + delta.X / 5),
                    delta.Y < 0
                        ? (int) Math.Floor(frame.Location.Y + delta.Y / 5)
                        : (int) Math.Ceiling(frame.Location.Y + delta.Y / 5)
                );
            }
        }
    }
}
