using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class RemoteControl
{
    [DllImport("user32.dll")]
    public static extern void SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    public const uint MOUSEEVENTF_LEFTUP = 0x04;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
    public const uint MOUSEEVENTF_RIGHTUP = 0x10;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const uint MOUSEEVENTF_WHEEL = 0x0800;
    public const uint MOUSEEVENTF_HWHEEL = 0x01000;

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;

    private static readonly object _inputLock = new object();
    private static readonly HashSet<ushort> _pressedKeys = new HashSet<ushort>();
    private static readonly HashSet<int> _pressedMouseButtons = new HashSet<int>();

    public struct POINT
    {
        public int X;
        public int Y;
    }

    public static void MoveCursor(int dx, int dy)
    {
        if (GetCursorPos(out POINT p))
        {
            SetCursorPos(p.X + dx, p.Y + dy);
        }
    }

    public static void MoveCursorAbsolute(int x, int y)
    {
        int virtualX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int virtualY = GetSystemMetrics(SM_YVIRTUALSCREEN);
        SetCursorPos(x + virtualX, y + virtualY);
    }

    public static void HandleInputLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        switch (parts[0])
        {
            case "m":
                if (parts.Length >= 3 && int.TryParse(parts[1], out int mx) && int.TryParse(parts[2], out int my))
                {
                    MoveCursorAbsolute(mx, my);
                }
                break;

            case "d":
            case "u":
                if (parts.Length >= 2 && int.TryParse(parts[1], out int btn) && btn >= 0 && btn <= 2)
                {
                    if (parts.Length >= 4 && int.TryParse(parts[2], out int bx) && int.TryParse(parts[3], out int by))
                    {
                        MoveCursorAbsolute(bx, by);
                    }
                    SendMouseButton(btn, isDown: parts[0] == "d");
                }
                break;

            case "w":
                if (parts.Length >= 2 && int.TryParse(parts[1], out int deltaY))
                {
                    SendMouseWheel(deltaY, horizontal: false);
                }
                break;

            case "h":
                if (parts.Length >= 2 && int.TryParse(parts[1], out int deltaX))
                {
                    SendMouseWheel(deltaX, horizontal: true);
                }
                break;

            case "k":
                if (parts.Length >= 3)
                {
                    var vkCode = KeyCodeConverter.GetVirtualKeyCode(parts[1]);
                    if (vkCode != 0)
                    {
                        SendKeyEventEx(vkCode, parts[2] == "up");
                    }
                }
                break;

            case "r":
                ReleaseAllKeys();
                break;
        }
    }

    public static void HandleMouseEvent(string type, int x, int y, int deltaY)
    {
        switch (type)
        {
            case "down_0": MoveCursorAbsolute(x, y); SendMouseButton(0, isDown: true); break;
            case "up_0": MoveCursorAbsolute(x, y); SendMouseButton(0, isDown: false); break;
            case "down_1": MoveCursorAbsolute(x, y); SendMouseButton(1, isDown: true); break;
            case "up_1": MoveCursorAbsolute(x, y); SendMouseButton(1, isDown: false); break;
            case "down_2": MoveCursorAbsolute(x, y); SendMouseButton(2, isDown: true); break;
            case "up_2": MoveCursorAbsolute(x, y); SendMouseButton(2, isDown: false); break;
            case "move": MoveCursor(x, y); break; // dx and dy are relative, so no offset needed
            case "wheel": SendMouseWheel(deltaY, horizontal: false); break;
        }
    }

    private static void SendMouseButton(int button, bool isDown)
    {
        uint flags = (button, isDown) switch
        {
            (0, true) => MOUSEEVENTF_LEFTDOWN,
            (0, false) => MOUSEEVENTF_LEFTUP,
            (1, true) => MOUSEEVENTF_MIDDLEDOWN,
            (1, false) => MOUSEEVENTF_MIDDLEUP,
            (2, true) => MOUSEEVENTF_RIGHTDOWN,
            (2, false) => MOUSEEVENTF_RIGHTUP,
            _ => 0
        };

        if (flags == 0)
            return;

        lock (_inputLock)
        {
            if (isDown)
                _pressedMouseButtons.Add(button);
            else
                _pressedMouseButtons.Remove(button);
        }

        SendMouseInput(flags);
    }

    private static void SendMouseWheel(int delta, bool horizontal)
    {
        uint flags = horizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL;
        uint mouseData = horizontal ? unchecked((uint)delta) : unchecked((uint)(-delta));
        SendMouseInput(flags, mouseData);
    }

    private static void SendMouseInput(uint flags, uint mouseData = 0)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0] = new INPUT
        {
            type = 0, // INPUT_MOUSE
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dwFlags = flags,
                    mouseData = mouseData,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

    public static void SendKeyEvent(ushort vkCode, bool keyUp)
    {
        SendKeyEventEx(vkCode, keyUp);
    }

    public static void SendKeyEventEx(ushort vkCode, bool up)
    {
        lock (_inputLock)
        {
            if (up)
                _pressedKeys.Remove(vkCode);
            else
                _pressedKeys.Add(vkCode);
        }

        uint flags = up ? KEYEVENTF_KEYUP : 0;
        if (IsExtendedKey(vkCode))
        {
            flags |= KEYEVENTF_EXTENDEDKEY;
        }

        INPUT[] inputs = new INPUT[1];
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vkCode,
                    wScan = (ushort)MapVirtualKey(vkCode, 0),
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public static void ReleaseAllKeys()
    {
        ushort[] keysToRelease;
        int[] buttonsToRelease;

        lock (_inputLock)
        {
            keysToRelease = _pressedKeys.ToArray();
            buttonsToRelease = _pressedMouseButtons.ToArray();
            _pressedKeys.Clear();
            _pressedMouseButtons.Clear();
        }

        foreach (var vk in keysToRelease)
        {
            SendKeyEventEx(vk, true);
        }

        foreach (var btn in buttonsToRelease)
        {
            SendMouseButton(btn, isDown: false);
        }
    }

    private static bool IsExtendedKey(ushort vkCode)
    {
        return vkCode switch
        {
            >= 0x21 and <= 0x28 => true, // PageUp, PageDown, End, Home, Arrows
            0x2D or 0x2E => true,        // Insert, Delete
            0x5B or 0x5C or 0x5D => true,// LWin, RWin, Apps
            0x6F => true,                // NumpadDivide
            0x90 => true,                // NumLock
            0xA3 or 0xA5 => true,        // RCtrl, RAlt
            _ => false
        };
    }

    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint CF_UNICODETEXT = 13;

    public static void SetClipboardText(string text)
    {
        text ??= string.Empty;
        if (!OpenClipboard(IntPtr.Zero))
            return;

        IntPtr hGlobal = IntPtr.Zero;
        bool ownershipTransferred = false;
        try
        {
            EmptyClipboard();
            char[] chars = new char[text.Length + 1];
            text.CopyTo(0, chars, 0, text.Length);
            chars[text.Length] = '\0';

            hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)(chars.Length * 2));
            if (hGlobal == IntPtr.Zero)
                return;

            IntPtr target = GlobalLock(hGlobal);
            if (target == IntPtr.Zero)
                return;

            try
            {
                Marshal.Copy(chars, 0, target, chars.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (SetClipboardData(CF_UNICODETEXT, hGlobal) != IntPtr.Zero)
            {
                ownershipTransferred = true;
            }
        }
        finally
        {
            CloseClipboard();
            if (hGlobal != IntPtr.Zero && !ownershipTransferred)
            {
                GlobalFree(hGlobal);
            }
        }
    }

    public static string GetClipboardText()
    {
        if (!OpenClipboard(IntPtr.Zero))
            return string.Empty;

        try
        {
            IntPtr hData = GetClipboardData(CF_UNICODETEXT);
            if (hData == IntPtr.Zero)
                return string.Empty;

            IntPtr ptr = GlobalLock(hData);
            if (ptr == IntPtr.Zero)
                return string.Empty;

            try
            {
                return Marshal.PtrToStringUni(ptr) ?? string.Empty;
            }
            finally
            {
                GlobalUnlock(hData);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }
}
