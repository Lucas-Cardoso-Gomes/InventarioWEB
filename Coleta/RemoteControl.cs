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

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

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

    public static void HandleMouseEvent(string type, int x, int y, int deltaY)
    {
        uint flags = 0;
        uint mouseData = 0;

        switch (type)
        {
            case "down_0": flags = MOUSEEVENTF_LEFTDOWN; SetCursorPos(x, y); break;
            case "up_0": flags = MOUSEEVENTF_LEFTUP; SetCursorPos(x, y); break;
            case "down_1": flags = MOUSEEVENTF_MIDDLEDOWN; SetCursorPos(x, y); break;
            case "up_1": flags = MOUSEEVENTF_MIDDLEUP; SetCursorPos(x, y); break;
            case "down_2": flags = MOUSEEVENTF_RIGHTDOWN; SetCursorPos(x, y); break;
            case "up_2": flags = MOUSEEVENTF_RIGHTUP; SetCursorPos(x, y); break;
            case "move": MoveCursor(x, y); return;
            case "wheel": flags = MOUSEEVENTF_WHEEL; mouseData = (uint)deltaY; break;
        }

        if (flags != 0)
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
    }


    // Structures and functions for SendInput
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
    public const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

    public static void SendKeyEvent(ushort vkCode, bool keyUp)
    {
        INPUT[] inputs = new INPUT[1];

        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vkCode,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    private const uint CF_UNICODETEXT = 13;

    public static void SetClipboardText(string text)
    {
        if (OpenClipboard(IntPtr.Zero))
        {
            EmptyClipboard();
            IntPtr hGlobal = Marshal.StringToHGlobalUni(text);
            SetClipboardData(CF_UNICODETEXT, hGlobal);
            CloseClipboard();
            Marshal.FreeHGlobal(hGlobal);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);

    public static string GetClipboardText()
    {
        if (OpenClipboard(IntPtr.Zero))
        {
            IntPtr hData = GetClipboardData(CF_UNICODETEXT);
            if (hData != IntPtr.Zero)
            {
                string text = Marshal.PtrToStringUni(hData);
                CloseClipboard();
                return text;
            }
            CloseClipboard();
        }
        return string.Empty;
    }
}
