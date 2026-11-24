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
        if (desktopPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to acquire desktop device context.");
        }

        IntPtr memoryDcPtr = IntPtr.Zero;
        IntPtr bitmapPtr = IntPtr.Zero;
        IntPtr oldBitmapPtr = IntPtr.Zero;

        byte[] result;
        try
        {
            memoryDcPtr = CreateCompatibleDC(desktopPtr);

            int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException($"Invalid virtual screen dimensions: {width}x{height}.");
            }

            bitmapPtr = CreateCompatibleBitmap(desktopPtr, width, height);
            oldBitmapPtr = SelectObject(memoryDcPtr, bitmapPtr);

            // Copia a imagem da tela para a memória
            BitBlt(memoryDcPtr, 0, 0, width, height, desktopPtr, x, y, SRCCOPY);

            // Desenha o cursor sobre a captura
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
                            int x_cursor = pci.ptScreenPos.x - x - ii.xHotspot;
                            int y_cursor = pci.ptScreenPos.y - y - ii.yHotspot;
                            DrawIconEx(memoryDcPtr, x_cursor, y_cursor, hicon, 0, 0, 0, IntPtr.Zero, DI_NORMAL);

                            if (ii.hbmColor != IntPtr.Zero) DeleteObject(ii.hbmColor);
                            if (ii.hbmMask != IntPtr.Zero) DeleteObject(ii.hbmMask);
                        }
                        DestroyIcon(hicon);
                    }
                }
            }

            using (Bitmap bmp = Bitmap.FromHbitmap(bitmapPtr))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
                    if (jpegCodec != null)
                    {
                        using (EncoderParameters encoderParams = new EncoderParameters(1))
                        using (EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L))
                        {
                            encoderParams.Param[0] = qualityParam;
                            bmp.Save(ms, jpegCodec, encoderParams);
                        }
                    }
                    else
                    {
                        bmp.Save(ms, ImageFormat.Jpeg);
                    }
                    result = ms.ToArray();
                }
            }
        }
        finally
        {
            if (oldBitmapPtr != IntPtr.Zero && memoryDcPtr != IntPtr.Zero) SelectObject(memoryDcPtr, oldBitmapPtr);
            if (bitmapPtr != IntPtr.Zero) DeleteObject(bitmapPtr);
            if (memoryDcPtr != IntPtr.Zero) DeleteDC(memoryDcPtr);
            ReleaseDC(IntPtr.Zero, desktopPtr);
        }

        return result;
    }

    private static ImageCodecInfo GetEncoderInfo(string mimeType)
    {
        ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
        for (int j = 0; j < encoders.Length; ++j)
        {
            if (encoders[j].MimeType == mimeType)
                return encoders[j];
        }
        return null;
    }

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    private const int SRCCOPY = 0x00CC0020;

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

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

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
    const uint DI_NORMAL = 0x0003;
    [DllImport("user32.dll")]
    static extern bool GetCursorInfo(out CURSORINFO pci);
    [DllImport("user32.dll")]
    static extern IntPtr CopyIcon(IntPtr hIcon);
    [DllImport("user32.dll")]
    static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
    [DllImport("user32.dll")]
    static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);
}