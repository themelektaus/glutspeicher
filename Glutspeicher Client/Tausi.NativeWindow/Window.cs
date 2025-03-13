using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using Vanara.PInvoke;

namespace Tausi.NativeWindow;

public class Window : IDisposable
{
    const string CLASS_NAME = $"{nameof(Tausi)}{nameof(NativeWindow)}";

    static int classNameIndex;

    public readonly static HINSTANCE moduleHandle = Kernel32.GetModuleHandle(null);

    public HWND Handle { get; private set; }

    readonly List<Control> controls = [];

    string className;
    User32.WindowProc windowProc;

    public int X => Rect.X;
    public int Y => Rect.Y;
    public int Width => Rect.Width;
    public int Height => Rect.Height;

    public Point Position
    {
        get => Rect.Location;
        set
        {
            if (Rect.Location != value)
            {
                var rect = Rect;
                rect.Location = value;
                Rect = rect;
            }
        }
    }

    public Size Size
    {
        get => Rect.Size;
        set
        {
            if (Rect.Size != value)
            {
                var rect = Rect;
                rect.Size = value;
                Rect = rect;
            }
        }
    }

    Rectangle _Rect;
    public Rectangle Rect
    {
        get => _Rect;
        set
        {
            if (_Rect != value)
            {
                _Rect = value;
                UpdateRect();
            }
        }
    }

    float _Opacity;
    public float Opacity
    {
        get => _Opacity;
        set
        {
            value = Math.Clamp(value, 0, 1);
            if (_Opacity != value)
            {
                _Opacity = value;
                UpdateOpacity();
            }
        }
    }

    public Color BackgroundColor { get; set; } = Color.FromArgb(20, 20, 20);

    public PointF ViewportPosition { get; set; } = new(.5f, .5f);

    public bool Draggable { get; set; } = true;

    BackgroundWorker worker;

    public event EventHandler<AddEventArgs> OnAdd;
    public class AddEventArgs : EventArgs
    {
        public Control Control { get; init; }
    }

    public event EventHandler OnBeforeCreateWindow;

    public event EventHandler OnAfterCreateWindow;

    public event EventHandler<CreateWorkerEventArgs> OnCreateWorker;
    public class CreateWorkerEventArgs : EventArgs
    {
        public BackgroundWorker Worker { get; init; }
    }

    public event EventHandler<UpdateEventArgs> OnUpdate;
    public class UpdateEventArgs : EventArgs
    {
        public int? Progress { get; set; }
    }

    public void Add(Control control)
    {
        controls.Add(control);
        control.Id = controls.Count;
        control.Owner = this;
        OnAdd?.Invoke(this, new() { Control = control });
    }

    HBRUSH backgroundColorBrush;

    public void Show()
    {
        OnBeforeCreateWindow?.Invoke(this, EventArgs.Empty);

        ApplyViewportPosition();
        CreateWindow();

        OnAfterCreateWindow?.Invoke(this, EventArgs.Empty);

        User32.ShowWindow(Handle, ShowWindowCommand.SW_SHOWNORMAL);

        CreateWorker();
        OnCreateWorker?.Invoke(this, new() { Worker = worker });
    }

    public void ShowDialog()
    {
        Show();
        Wait();
    }

    void CreateWindow()
    {
        className = CLASS_NAME + (++classNameIndex);
        windowProc = WindowProc;

        backgroundColorBrush = Gdi32.CreateSolidBrush(BackgroundColor);

        var windowClass = new User32.WNDCLASS
        {
            hInstance = moduleHandle,
            lpszClassName = className,
            hbrBackground = backgroundColorBrush,
            hCursor = User32.LoadCursor(0, User32.IDC_ARROW),
            lpfnWndProc = windowProc
        };

        User32.RegisterClass(windowClass);

        Handle = User32.CreateWindowEx(
            dwExStyle: User32.WindowStylesEx.WS_EX_TOPMOST | User32.WindowStylesEx.WS_EX_NOACTIVATE | User32.WindowStylesEx.WS_EX_LAYERED,
            lpClassName: className,
            dwStyle: User32.WindowStyles.WS_POPUP,
            X: Rect.X,
            Y: Rect.Y,
            nWidth: Rect.Width,
            nHeight: Rect.Height,
            hInstance: moduleHandle
        );

        UpdateOpacity();

        foreach (var control in controls)
        {
            control.Create(this);
        }
    }

    void CreateWorker()
    {
        worker = new() { WorkerReportsProgress = true };

        worker.DoWork += (sender, e) =>
        {
            while (Opacity < 1)
            {
                Opacity += .25f;
                Thread.Sleep(12);
            }

            while (!disposing)
            {
                User32.WINDOWINFO info = default;
                User32.GetWindowInfo(Handle, ref info);
                var winPos = info.rcWindow.Location;

                User32.GetCursorPos(out var curPos);

                foreach (var control in controls)
                {
                    if (control is Button button)
                    {
                        var rect = new RECT(
                            left: winPos.X + button.X,
                            top: winPos.Y + button.Y,
                            right: winPos.X + button.X + button.Width,
                            bottom: winPos.Y + button.Y + button.Height
                        );

                        var hover = curPos.X >= rect.left
                            && curPos.X < rect.right
                            && curPos.Y >= rect.top
                            && curPos.Y < rect.bottom;

                        if (button.Hover != hover)
                        {
                            button.Hover = hover;
                            User32.InvalidateRect(button.Handle, null, true);
                        }
                    }
                }

                var updateEventArgs = new UpdateEventArgs();
                OnUpdate?.Invoke(this, updateEventArgs);
                if (updateEventArgs.Progress.HasValue)
                {
                    worker.ReportProgress(updateEventArgs.Progress.Value);
                }

                Thread.Sleep(12);
            }

            while (Opacity > 0)
            {
                Opacity -= .25f;
                Thread.Sleep(12);
            }
        };

        OnCreateWorker?.Invoke(this, new() { Worker = worker });

        worker.RunWorkerAsync();
    }

    void Wait()
    {
        while (User32.GetMessage(out var msg, Handle, 0, 0) > 0)
        {
            User32.TranslateMessage(msg);
            User32.DispatchMessage(msg);
        }
    }

    void Hide()
    {
        User32.ShowWindow(Handle, ShowWindowCommand.SW_HIDE);
    }

    bool disposing;

    public virtual void Dispose()
    {
        if (disposing)
        {
            return;
        }

        disposing = true;

        while (worker.IsBusy)
        {
            Thread.Sleep(100);
        }

        worker.Dispose();

        foreach (var control in controls)
        {
            control.Dispose();
        }

        Gdi32.DeleteBrush(backgroundColorBrush);

        User32.DestroyWindow(Handle);

        Handle = 0;

        User32.UnregisterClass(className, moduleHandle);
    }

    void ApplyViewportPosition()
    {
        var (screenWidth, screenHeight) = UseDC(x =>
        {
            return (
                Gdi32.GetDeviceCaps(x, Gdi32.DeviceCap.HORZRES),
                Gdi32.GetDeviceCaps(x, Gdi32.DeviceCap.VERTRES)
            );
        });

        var xRange = new Point(Rect.Width, screenWidth - Rect.Width * 2);
        var yRange = new Point(Rect.Height, screenHeight - Rect.Height * 2);

        _Rect.X = (int) (xRange.X + (xRange.Y - xRange.X) * ViewportPosition.X);
        _Rect.Y = (int) (yRange.X + (yRange.Y - yRange.X) * ViewportPosition.Y);
    }

    void UpdateRect()
    {
        User32.MoveWindow(Handle, Rect.X, Rect.Y, Rect.Width, Rect.Height, false);
    }

    void UpdateOpacity()
    {
        User32.SetLayeredWindowAttributes(Handle, 0, (byte) (255 * Opacity), User32.LayeredWindowAttributes.LWA_ALPHA);
    }

    nint WindowProc(HWND hwnd, uint uMsg, nint wParam, nint lParam)
    {
        var message = (User32.WindowMessage) uMsg;

        switch (message)
        {
            case User32.WindowMessage.WM_NCHITTEST:
                if (Draggable)
                {
                    return (nint) User32.HitTestValues.HTCAPTION;
                }
                break;

            case User32.WindowMessage.WM_MOUSEACTIVATE:
                return (nint) User32.WM_MOUSEACTIVATE_RETURN.MA_NOACTIVATE;

            case User32.WindowMessage.WM_CLOSE:
                Dispose();
                return 0;

            case User32.WindowMessage.WM_DESTROY:
                User32.PostQuitMessage(0);
                return 0;

            case User32.WindowMessage.WM_CTLCOLORBTN:
                if (HandleRendering(wParam, lParam, out var brush))
                {
                    return (nint) brush;
                }
                break;

            case User32.WindowMessage.WM_COMMAND:
                HandleCommand(wParam);
                break;
        }

        return User32.DefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    void HandleCommand(nint wParam)
    {
        if (controls.FirstOrDefault(x => x.Id == wParam) is Button button)
        {
            button.InvokeClick();
        }
    }

    bool HandleRendering(HDC hdc, HWND handle, out HBRUSH brush)
    {
        var control = controls.FirstOrDefault(x => x.Handle == handle);

        if (control is Label label)
        {
            var backgroundColor = label.BackgroundColor == Color.Transparent
                ? BackgroundColor
                : label.BackgroundColor;


            if (label is Button button && button.Hover)
            {
                backgroundColor = Color.FromArgb(backgroundColor.ToArgb() + Color.FromArgb(20, 20, 20).ToArgb());
            }

            Gdi32.SetBkColor(hdc, backgroundColor);

            if (!string.IsNullOrWhiteSpace(label.Text))
            {
                Gdi32.SetTextColor(hdc, label.TextColor);
                Gdi32.SetTextAlign(hdc, Gdi32.TextAlign.TA_CENTER);

                var font = label.CreateFont();
                var previousFont = Gdi32.SelectObject(hdc, font);
                Gdi32.TextOut(hdc, label.Width / 2 + label.TextOffset.X, label.Height / 2 + label.TextOffset.Y, label.Text, label.Text.Length);
                Gdi32.SelectObject(hdc, previousFont);
                Gdi32.DeleteFont(font);
            }

            brush = Gdi32.CreateSolidBrush(backgroundColor);
            return true;
        }

        brush = HBRUSH.NULL;
        return false;
    }

    public static T UseDC<T>(Func<HDC, T> callback)
    {
        var hdc = User32.GetDC();
        var result = callback(hdc);
        User32.ReleaseDC(0, hdc);
        return result;
    }
}
