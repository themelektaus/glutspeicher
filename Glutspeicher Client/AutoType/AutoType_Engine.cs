﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Glutspeicher.Client;

using SendMethod = AutoType_WindowInfo.SendMethod;

public sealed class AutoType_Engine : IDisposable
{
    nint originalKeyboardLayout;
    nint currentKeyboardLayout;

    bool inputBlocked;

    SendMethod? forceSendMethod;

    readonly Dictionary<nint, AutoType_WindowInfo> windowInfos = [];
    AutoType_WindowInfo currentWindowInfo = new(0);

    int currentKeyModifiers = 0;

    readonly Stopwatch lastEventStopWatch = new();

    public AutoType_Engine()
    {
        InitializeEnvironment();

        inputBlocked = AutoType_NativeMethods.BlockInput(true);

        var lastInputTime = AutoType_NativeMethods.GetLastInputTime();
        if (lastInputTime.HasValue)
        {
            if (Environment.TickCount - (int) lastInputTime.Value == 0)
            {
                SleepAndDoEvents(1);
            }
        }

        PrepareSend();

        if (ReleaseModifiers(true) != 0)
        {
            return;
        }

        SleepAndDoEvents(1);
    }

    public void Dispose()
    {
        PrepareSend();

        if (inputBlocked)
        {
            AutoType_NativeMethods.BlockInput(false);
            inputBlocked = false;
        }

        if (currentKeyboardLayout != originalKeyboardLayout)
        {
            if (originalKeyboardLayout != nint.Zero)
            {
                currentKeyboardLayout = originalKeyboardLayout;
            }
        }

        lastEventStopWatch.Stop();
    }

    public void SendKey(int iVKey, bool? obExtKey, bool? obDown)
    {
        PreSendEvent();
        SendKeyInternal(iVKey, obExtKey, obDown);
    }

    void SendKeyInternal(int vKey, bool? isExtendedKey, bool? down)
    {
        PrepareSend();

        if (down.HasValue)
        {
            SendKeyNative(vKey, isExtendedKey, down.Value);
            return;
        }

        SendKeyNative(vKey, isExtendedKey, true);
        SendKeyNative(vKey, isExtendedKey, false);
    }

    public void SetKeyModifier(int keyModifier, bool down)
    {
        PreSendEvent();
        SetKeyModifierInternal(keyModifier, down, false);
    }

    void SetKeyModifierInternal(int modifierKey, bool down, bool forChar)
    {
        PrepareSend();

        var shift = (modifierKey & 65536) != 0;
        var control = (modifierKey & 131072) != 0;
        var alt = (modifierKey & 262144) != 0;

        if (shift)
        {
            SendKeyNative(160, null, down);
        }

        if (control && alt && forChar)
        {
            if (!currentWindowInfo.charsRAltAsCtrlAlt)
            {
                SendKeyNative(162, null, down);
            }

            SendKeyNative(165, null, down);
        }
        else
        {
            if (control)
            {
                SendKeyNative(162, null, down);
            }

            if (alt)
            {
                SendKeyNative(164, null, down);
            }
        }

        if (down)
        {
            currentKeyModifiers |= modifierKey;
            return;
        }

        currentKeyModifiers &= ~modifierKey;
    }

    public void SendChar(char c, bool? down)
    {
        PreSendEvent();
        SendCharInternal(c, down);
    }

    void SendCharInternal(char c, bool? down)
    {
        PrepareSend();

        if (TrySendCharByKeyPresses(c, down))
        {
            return;
        }

        if (down.HasValue)
        {
            SendCharNative(c, down.Value);
            return;
        }

        SendCharNative(c, true);
        SendCharNative(c, false);
    }

    public void Delay(uint milliseconds)
    {
        if (!lastEventStopWatch.IsRunning)
        {
            Thread.Sleep((int) milliseconds);
            lastEventStopWatch.Reset();
            lastEventStopWatch.Start();
            return;
        }

        lastEventStopWatch.Stop();

        var elapsedDelay = lastEventStopWatch.ElapsedMilliseconds;
        var remainingDelay = milliseconds - elapsedDelay;

        if (remainingDelay >= 0)
        {
            Thread.Sleep((int) remainingDelay);
        }

        lastEventStopWatch.Reset();
        lastEventStopWatch.Start();
    }

    void PreSendEvent()
    {
        lastEventStopWatch.Reset();
        lastEventStopWatch.Start();
    }

    void InitializeEnvironment()
    {
        currentKeyboardLayout = AutoType_NativeMethods.GetKeyboardLayout(0);
        originalKeyboardLayout = currentKeyboardLayout;

        foreach (var process in Process.GetProcesses())
        {
            if (process is null)
            {
                continue;
            }

            var processName = AutoType_WindowInfo.GetProcessName(process);

            if (AutoType_WindowInfo.ProcessNameMatches(processName, "Neo20"))
            {
                var info = process.MainModule.FileVersionInfo;
                if (
                    (info.ProductName ?? string.Empty).Trim().Length == 0 &&
                    (info.FileDescription ?? string.Empty).Trim().Length == 0
                )
                {
                    forceSendMethod = SendMethod.UnicodePacket;
                }
            }
            else if (AutoType_WindowInfo.ProcessNameMatches(processName, "KbdNeo_Ahk"))
            {
                forceSendMethod = SendMethod.UnicodePacket;
            }

            try
            {
                process.Dispose();
            }
            catch
            {
            }
        }
    }

    bool SendKeyNative(int vKey, bool? isExtendedKey, bool down)
    {
        if (nint.Size == 4)
        {
            return SendKeyNative32(vKey, isExtendedKey, null, down);
        }

        if (nint.Size == 8)
        {
            return SendKeyNative64(vKey, isExtendedKey, null, down);
        }

        return false;
    }

    bool SendCharNative(char c, bool down)
    {
        if (nint.Size == 4)
        {
            return SendKeyNative32(0, null, c, down);
        }

        if (nint.Size == 8)
        {
            return SendKeyNative64(0, null, c, down);
        }

        return false;
    }

    bool SendKeyNative32(int vKey, bool? isExtendedKey, char? unicodeChar, bool down)
    {
        var input = new AutoType_NativeMethods.INPUT32[1];

        input[0].Type = 1;

        var ki = input[0].KeyboardInput;

        if (unicodeChar.HasValue)
        {
            ki.VirtualKeyCode = 0;
            ki.ScanCode = unicodeChar.Value;
            ki.Flags = (uint) ((down ? 0 : 2) | 4);
        }
        else
        {
            var keyboardLayout = currentWindowInfo.keyboardLayout;

            if (unicodeChar.HasValue)
            {
                vKey = (int) (AutoType_NativeMethods.VkKeyScan3(unicodeChar.Value, keyboardLayout) & 0xFFU);
            }

            ki.VirtualKeyCode = (ushort) vKey;
            ki.ScanCode = (ushort) (AutoType_NativeMethods.MapVirtualKey3((uint) vKey, 0, keyboardLayout) & 0xFFU);
            ki.Flags = GetKeyEventFlags(vKey, isExtendedKey, down);
        }

        ki.Time = 0;
        ki.ExtraInfo = AutoType_NativeMethods.GetMessageExtraInfo();

        return AutoType_NativeMethods.SendInput32(1, input, Marshal.SizeOf<AutoType_NativeMethods.INPUT32>()) == 1;
    }

    bool SendKeyNative64(int vKey, bool? isExtendedKey, char? unicodeChar, bool down)
    {
        var input = new AutoType_NativeMethods.SpecializedKeyboardINPUT64[1];

        input[0].Type = 1;

        if (unicodeChar.HasValue)
        {
            input[0].VirtualKeyCode = 0;
            input[0].ScanCode = unicodeChar.Value;
            input[0].Flags = (uint) ((down ? 0 : 2) | 4);
        }
        else
        {
            var keyboardLayout = currentWindowInfo.keyboardLayout;

            if (unicodeChar.HasValue)
            {
                vKey = (int) (AutoType_NativeMethods.VkKeyScan3(unicodeChar.Value, keyboardLayout) & 0xFFU);
            }

            input[0].VirtualKeyCode = (ushort) vKey;
            input[0].ScanCode = (ushort) (AutoType_NativeMethods.MapVirtualKey3((uint) vKey, 0, keyboardLayout) & 0xFFU);
            input[0].Flags = GetKeyEventFlags(vKey, isExtendedKey, down);
        }

        input[0].Time = 0;
        input[0].ExtraInfo = AutoType_NativeMethods.GetMessageExtraInfo();

        return AutoType_NativeMethods.SendInput64Special(1, input, Marshal.SizeOf<AutoType_NativeMethods.SpecializedKeyboardINPUT64>()) == 1;
    }

    int ReleaseModifiers(bool withSpecial)
    {
        List<int> vKeyMods =
        [
            .. new int[] {
                AutoType_NativeMethods.VK_LWIN,
                AutoType_NativeMethods.VK_RWIN,
                AutoType_NativeMethods.VK_LSHIFT,
                AutoType_NativeMethods.VK_RSHIFT,
                AutoType_NativeMethods.VK_SHIFT,
                AutoType_NativeMethods.VK_LCONTROL,
                AutoType_NativeMethods.VK_RCONTROL,
                AutoType_NativeMethods.VK_CONTROL,
                AutoType_NativeMethods.VK_LMENU,
                AutoType_NativeMethods.VK_RMENU,
                AutoType_NativeMethods.VK_MENU
            },
            .. toggleKeys,
        ];

        var vKeysReleased = new List<int>();

        foreach (int vKey in vKeyMods)
        {
            if (IsKeyActive(vKey))
            {
                ActivateOrToggleKey(vKey, false);
                vKeysReleased.Add(vKey);
            }
        }

        if (withSpecial)
        {
            ReleaseModifiersSpecialPost(vKeysReleased);
        }

        return vKeysReleased.Count;
    }

    void ActivateOrToggleKey(int vKey, bool down)
    {
        if (System.Array.IndexOf(toggleKeys, vKey) >= 0)
        {
            SendKeyNative(vKey, null, true);
            SendKeyNative(vKey, null, false);
            return;
        }

        SendKeyNative(vKey, null, down);
    }

    void ReleaseModifiersSpecialPost(List<int> vKeys)
    {
        if (vKeys.Count == 0)
        {
            return;
        }

        if (!vKeys.All(IsAltOrToggle))
        {
            return;
        }

        if (vKeys.Contains(AutoType_NativeMethods.VK_LMENU))
        {
            SendKeyNative(AutoType_NativeMethods.VK_LMENU, null, true);
            SendKeyNative(AutoType_NativeMethods.VK_LMENU, null, false);
            return;
        }

        if (vKeys.Contains(AutoType_NativeMethods.VK_RMENU))
        {
            SendKeyNative(AutoType_NativeMethods.VK_RMENU, null, true);
            SendKeyNative(AutoType_NativeMethods.VK_RMENU, null, false);
        }
    }

    static readonly char[] forceUniChars = [
        '\u005E',
        '\u0060',
        '\u00A8',
        '\u00AF',
        '\u00B0',
        '\u00B4',
        '\u00B8',
        '\u0022',
        '\u0027',
        '\u007E'
    ];

    bool TrySendCharByKeyPresses(char c, bool? down)
    {
        if (c == char.MinValue)
        {
            return false;
        }

        var sendMethod = GetSendMethod(currentWindowInfo);
        if (sendMethod == SendMethod.UnicodePacket)
        {
            return false;
        }

        if (sendMethod != SendMethod.KeyEvent)
        {
            if (System.Array.IndexOf(forceUniChars, c) >= 0)
            {
                return false;
            }

            if (c >= '\u02B0' && c <= '\u02FF')
            {
                return false;
            }
        }

        var keyboardLayout = currentWindowInfo.keyboardLayout;

        var u = AutoType_NativeMethods.VkKeyScan3(c, keyboardLayout);
        if (u == 0xFFFFU)
        {
            return false;
        }

        var vKey = (int) (u & 0xFFU);

        var mod = 0;

        var shift = (mod & 65536) != 0;
        var control = (mod & 131072) != 0;
        var alt = (mod & 262144) != 0;

        var capsLock = false;

        var keyState = new byte[256];

        if (shift)
        {
            keyState[AutoType_NativeMethods.VK_SHIFT] = 0x80;
            keyState[AutoType_NativeMethods.VK_LSHIFT] = 0x80;
        }

        if (control && alt)
        {
            keyState[AutoType_NativeMethods.VK_CONTROL] = 0x80;
            keyState[AutoType_NativeMethods.VK_LCONTROL] = 0x80;
            keyState[AutoType_NativeMethods.VK_MENU] = 0x80;
            keyState[AutoType_NativeMethods.VK_RMENU] = 0x80;
        }
        else
        {
            if (control)
            {
                keyState[AutoType_NativeMethods.VK_CONTROL] = 0x80;
                keyState[AutoType_NativeMethods.VK_LCONTROL] = 0x80;
            }

            if (alt)
            {
                keyState[AutoType_NativeMethods.VK_MENU] = 0x80;
                keyState[AutoType_NativeMethods.VK_LMENU] = 0x80;
            }
        }

        keyState[AutoType_NativeMethods.VK_NUMLOCK] = 0x01;

        var uniString = AutoType_NativeMethods.ToUnicode3(vKey, keyState, keyboardLayout);

        if (uniString is null || (uniString.Length > 0 && uniString[^1] != c))
        {
            keyState[AutoType_NativeMethods.VK_CAPITAL] = 0x01;

            uniString = AutoType_NativeMethods.ToUnicode3(vKey, keyState, keyboardLayout);

            if (uniString is null || (uniString.Length > 0 && (uniString[^1] != c)))
                return false;

            capsLock = true;
        }

        var keyModDiff = mod & ~currentKeyModifiers;
        var shouldSleep = capsLock || (keyModDiff != 0);
        var sleepMilliseconds = currentWindowInfo.sleepAroundKeyMod;

        if (capsLock)
        {
            SendKeyInternal(AutoType_NativeMethods.VK_CAPITAL, null, null);
        }

        if (keyModDiff != 0)
        {
            SetKeyModifierInternal(keyModDiff, true, true);
        }

        if (shouldSleep)
        {
            SleepAndDoEvents(sleepMilliseconds);
        }

        SendKeyInternal(vKey, null, down);

        if (shouldSleep)
        {
            SleepAndDoEvents(sleepMilliseconds);
        }

        if (keyModDiff != 0)
        {
            SetKeyModifierInternal(keyModDiff, false, true);
        }

        if (capsLock)
        {
            SendKeyInternal(AutoType_NativeMethods.VK_CAPITAL, null, null);
        }

        if (shouldSleep)
        {
            SleepAndDoEvents(sleepMilliseconds);
        }

        return true;
    }

    SendMethod GetSendMethod(AutoType_WindowInfo info)
    {
        return forceSendMethod ?? info.sendMethod;
    }

    AutoType_WindowInfo GetWindowInfo(nint windowHandle)
    {
        if (windowInfos.TryGetValue(windowHandle, out var swi))
        {
            return swi;
        }

        swi = new AutoType_WindowInfo(windowHandle);
        windowInfos[windowHandle] = swi;
        return swi;
    }

    void PrepareSend()
    {
        var windowHandle = AutoType_NativeMethods.GetForegroundWindow();
        currentWindowInfo = GetWindowInfo(windowHandle);

        EnsureSameKeyboardLayout();
    }

    void EnsureSameKeyboardLayout()
    {
        var targetKeyboardLayout = currentWindowInfo.keyboardLayout;
        if (targetKeyboardLayout == nint.Zero)
        {
            return;
        }

        if (currentKeyboardLayout == targetKeyboardLayout)
        {
            return;
        }

        currentKeyboardLayout = targetKeyboardLayout;
        SleepAndDoEvents(1);
    }

    static uint GetKeyEventFlags(int vKey, bool? isExtendedKey, bool down)
    {
        uint u = 0;

        if (!down)
        {
            u |= 2;
        }

        if (isExtendedKey.HasValue)
        {
            if (isExtendedKey.Value)
            {
                u |= 1;
            }
        }
        else if (IsExtendedKeyEx(vKey))
        {
            u |= 1;
        }

        return u;
    }

    static bool IsExtendedKeyEx(int vKey)
    {
        if (vKey >= 0x21 && vKey <= 0x2E)
        {
            return true;
        }

        if (vKey >= 0x5B && vKey <= 0x5D)
        {
            return true;
        }

        if (vKey == 0x6F)
        {
            return true;
        }

        if (vKey == AutoType_NativeMethods.VK_RCONTROL)
        {
            return true;
        }

        if (vKey == AutoType_NativeMethods.VK_RMENU)
        {
            return true;
        }

        return false;
    }

    static readonly int[] toggleKeys = [0x14];

    static bool IsKeyActive(int vKey)
    {
        if (System.Array.IndexOf(toggleKeys, vKey) >= 0)
        {
            return (AutoType_NativeMethods.GetKeyState(vKey) & 1) != 0;
        }

        return (AutoType_NativeMethods.GetAsyncKeyState(vKey) & 0x8000) != 0;
    }

    static bool IsAltOrToggle(int vKey)
    {
        if (vKey == AutoType_NativeMethods.VK_LMENU)
        {
            return true;
        }

        if (vKey == AutoType_NativeMethods.VK_RMENU)
        {
            return true;
        }

        if (vKey == AutoType_NativeMethods.VK_MENU)
        {
            return true;
        }

        if (System.Array.IndexOf(toggleKeys, vKey) >= 0)
        {
            return true;
        }

        return false;
    }

    static void SleepAndDoEvents(int millseconds)
    {
        if (millseconds >= 0)
        {
            Thread.Sleep(millseconds);
        }
    }
}
