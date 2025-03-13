using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace Tausi.NativeWindow;

public class Window : Frame
{
    const string CLASS_NAME = $"{nameof(Tausi)}{nameof(NativeWindow)}";

    static int classNameIndex;

    public readonly static nint moduleHandle = Kernel32.GetModuleHandle(null);

    public nint Handle { get; private set; }

    readonly List<Control> controls = [];

    string className;
    User32.WindowProc windowProc;

    public override Rectangle Rect
    {
        get => base.Rect;
        set
        {
            if (base.Rect != value)
            {
                base.Rect = value;
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

    public PointF ViewportPoint { get; set; } = new(.5f, .5f);

    public bool Draggable { get; set; } = true;

    BackgroundWorker worker;

    public event EventHandler<AddControlEventArgs> OnAddControl;
    public class AddControlEventArgs : EventArgs
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

    public event EventHandler OnUpdate;

    public void AddControl(Control control)
    {
        controls.Add(control);
        control.Id = controls.Count;
        control.Owner = this;
        OnAddControl?.Invoke(this, new() { Control = control });
    }

    nint backgroundColorBrush;

    public void Show()
    {
        OnBeforeCreateWindow?.Invoke(this, EventArgs.Empty);

        ApplyViewportPoint();
        CreateWindow();

        OnAfterCreateWindow?.Invoke(this, EventArgs.Empty);

        User32.ShowWindow(Handle, User32.ShowWindowCommand.SW_SHOWNORMAL);

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
#if DEBUG
        Console.WriteLine(nameof(CreateWindow));
#endif

        className = CLASS_NAME + (++classNameIndex);
        windowProc = WindowProc;

        backgroundColorBrush = Gdi32.CreateSolidBrush(ColorTranslator.ToWin32(BackgroundColor));

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
            X: X,
            Y: Y,
            nWidth: Width,
            nHeight: Height,
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
                Opacity += .2f;
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
                        var rect = new User32.RECT()
                        {
                            Left = winPos.X + button.X,
                            Top = winPos.Y + button.Y,
                            Right = winPos.X + button.X + button.Width,
                            Bottom = winPos.Y + button.Y + button.Height
                        };

                        var hover = curPos.X >= rect.Left
                            && curPos.X < rect.Right
                            && curPos.Y >= rect.Top
                            && curPos.Y < rect.Bottom;

                        if (button.Hover != hover)
                        {
                            button.Hover = hover;
                            User32.InvalidateRect(button.Handle, 0, true);
                        }
                    }
                }

                OnUpdate?.Invoke(this, EventArgs.Empty);

                Thread.Sleep(12);
            }

            while (Opacity > 0)
            {
                Opacity -= .2f;
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

    bool disposing;

    public override void Dispose()
    {
        if (disposing)
        {
            return;
        }

        disposing = true;

        while (worker.IsBusy)
        {
            Thread.Sleep(12);
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

    void ApplyViewportPoint()
    {
        var hdc = User32.GetDC();
        var screenWidth = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.HORZRES);
        var screenHeight = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.VERTRES);
        User32.ReleaseDC(hdc);

        var xRange = new Point(Rect.Width, screenWidth - Rect.Width * 2);
        var yRange = new Point(Rect.Height, screenHeight - Rect.Height * 2);

        var rect = base.Rect;
        rect.X = (int) (xRange.X + (xRange.Y - xRange.X) * ViewportPoint.X);
        rect.Y = (int) (yRange.X + (yRange.Y - yRange.X) * ViewportPoint.Y);
        base.Rect = rect;
    }

    void UpdateRect()
    {
        if (Handle == 0)
        {
            return;
        }

#if DEBUG
        Console.WriteLine(nameof(UpdateRect));
#endif

        User32.MoveWindow(Handle, X, Y, Width, Height, false);
    }

    void UpdateOpacity()
    {
        if (Handle == 0)
        {
            return;
        }

#if DEBUG
        Console.WriteLine(nameof(UpdateOpacity));
#endif
        User32.SetLayeredWindowAttributes(Handle, default, (byte) (255 * Opacity), User32.LayeredWindowAttributes.LWA_ALPHA);
    }

    nint WindowProc(nint hwnd, uint uMsg, nint wParam, nint lParam)
    {
        var message = uMsg;

        const uint WM_NCHITTEST = 0x0084;
        const uint WM_MOUSEACTIVATE = 0x0021;
        const uint WM_CLOSE = 0x0010;
        const uint WM_DESTROY = 0x0002;
        const uint WM_CTLCOLORBTN = 0x0135;
        const uint WM_COMMAND = 0x0111;

        const nint HTCAPTION = 2;
        const nint MA_NOACTIVATE = 3;

        switch (message)
        {
            case WM_NCHITTEST:
                if (Draggable)
                {
                    return HTCAPTION;
                }
                break;

            case WM_MOUSEACTIVATE:
                return MA_NOACTIVATE;

            case WM_CLOSE:
                Dispose();
                return 0;

            case WM_DESTROY:
                User32.PostQuitMessage(0);
                return 0;

            case WM_CTLCOLORBTN:
                if (HandleRendering(wParam, lParam, out var brush))
                {
                    return brush;
                }
                break;

            case WM_COMMAND:
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

    bool HandleRendering(nint hdc, nint handle, out nint brush)
    {
        const uint TA_CENTER = 6;

        var control = controls.FirstOrDefault(x => x.Handle == handle);

        if (control is Label label)
        {
            var backgroundColor = label.BackgroundColor == Color.Transparent
                ? BackgroundColor
                : label.BackgroundColor;

            if (label is Button button && button.Hover)
            {
                backgroundColor = Color.FromArgb(
                    Math.Min(255, backgroundColor.R + 20),
                    Math.Min(255, backgroundColor.G + 20),
                    Math.Min(255, backgroundColor.B + 20)
                );
            }

            Gdi32.SetBkColor(hdc, ColorTranslator.ToWin32(backgroundColor));

            if (!string.IsNullOrWhiteSpace(label.Text))
            {
                Gdi32.SetTextColor(hdc, ColorTranslator.ToWin32(label.TextColor));

                Gdi32.SetTextAlign(hdc, TA_CENTER);

                var font = label.CreateFont();
                var previousFont = Gdi32.SelectObject(hdc, font);
                Gdi32.TextOut(hdc, label.Width / 2 + label.TextOffset.X, label.Height / 2 + label.TextOffset.Y, label.Text, label.Text.Length);
                Gdi32.SelectObject(hdc, previousFont);
                Gdi32.DeleteFont(font);
            }

            brush = Gdi32.CreateSolidBrush(ColorTranslator.ToWin32(backgroundColor));
            return true;
        }

        brush = 0;
        return false;
    }
}
