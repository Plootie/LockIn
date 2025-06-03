using System.Runtime.InteropServices;

namespace LockIn;

public static class Natives
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ClipCursor([In] ref RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool ClipCursor(IntPtr lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetClipCursor(out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
}

public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;

    public override string ToString() =>
        $"Left={Left}, Top={Top}, Right={Right}, Bottom={Bottom}, Width={Width}, Height={Height}";
}