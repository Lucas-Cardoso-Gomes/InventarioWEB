using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public class ScreenCapturer
{
    public static byte[] CaptureScreen()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new NotSupportedException("Screen capture is only supported on Windows.");
        }

        IntPtr desktopPtr = GetDC(IntPtr.Zero);
        IntPtr memoryDcPtr = CreateCompatibleDC(desktopPtr);
        
        int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        IntPtr bitmapPtr = CreateCompatibleBitmap(desktopPtr, width, height);
        SelectObject(memoryDcPtr, bitmapPtr);

        // Copy the entire virtual screen to the memory device context
        BitBlt(memoryDcPtr, 0, 0, width, height, desktopPtr, x, y, SRCCOPY);

        // Capture and draw the cursor
        CURSORINFO pci;
        pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
        if (GetCursorInfo(out pci))
        {
            if (pci.flags == CURSOR_SHOWING)
            {
                IntPtr hicon = CopyIcon(pci.hCursor);
                if (hicon != IntPtr.Zero)
                {
                    ICONINFO ii;
                    if (GetIconInfo(hicon, out ii))
                    {
                        int x_cursor = pci.ptScreenPos.x - ii.xHotspot;
                        int y_cursor = pci.ptScreenPos.y - ii.yHotspot;
                        DrawIcon(memoryDcPtr, x_cursor, y_cursor, hicon);
                    }
                }
            }
        }

        using (Bitmap bmp = Bitmap.FromHbitmap(bitmapPtr))
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
    }

    // Constants for GetSystemMetrics
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    private const int SRCCOPY = 0x00CC0020; // Ternary raster operation code

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    // P/Invoke for cursor
    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    const int CURSOR_SHOWING = 0x00000001;
    [DllImport("user32.dll")]
    static extern bool GetCursorInfo(out CURSORINFO pci);
    [DllImport("user32.dll")]
    static extern IntPtr CopyIcon(IntPtr hIcon);
    [DllImport("user32.dll")]
    static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
    [DllImport("user32.dll")]
    static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);
}