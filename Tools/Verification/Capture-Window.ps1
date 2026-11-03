# Captures the main window of one process to a PNG using PrintWindow, so only that
# window's content is recorded (other windows on the desktop are not captured).
# Usage: . .\Capture-Window.ps1; Save-WindowCapture -ProcessId 1234 -Path out.png
Add-Type -AssemblyName System.Drawing
if (-not ('NU36.WinCapture' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace NU36 {
public static class WinCapture {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    const uint PW_RENDERFULLCONTENT = 2;
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    // Largest visible top-level window owned by the process (MainWindowHandle can be zero
    // for windows .NET does not recognise as the main window).
    public static IntPtr FindLargestWindow(int processId) {
        IntPtr best = IntPtr.Zero; long bestArea = 0;
        EnumWindows((h, l) => {
            uint pid; GetWindowThreadProcessId(h, out pid);
            RECT r;
            if (pid == processId && IsWindowVisible(h) && GetWindowRect(h, out r)) {
                long area = (long)(r.Right - r.Left) * (r.Bottom - r.Top);
                if (area > bestArea) { bestArea = area; best = h; }
            }
            return true;
        }, IntPtr.Zero);
        return best;
    }
    public static bool RequestClose(IntPtr h) { return PostMessage(h, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero); }
    public static string Save(IntPtr hwnd, string path) {
        SetProcessDPIAware();
        RECT r;
        if (!GetWindowRect(hwnd, out r)) return "GetWindowRect failed";
        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        if (w <= 0 || h <= 0) return "Window has no size (minimized?)";
        using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp)) {
            IntPtr hdc = g.GetHdc();
            bool ok = PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT);
            g.ReleaseHdc(hdc);
            if (!ok) return "PrintWindow failed";
            bmp.Save(path, ImageFormat.Png);
        }
        return "OK " + w + "x" + h;
    }
}
}
'@
}

function Save-WindowCapture([int]$ProcessId, [string]$Path) {
    $p = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $p) { return 'Process not running' }
    $p.Refresh()
    $h = $p.MainWindowHandle
    if ($h -eq [IntPtr]::Zero) { $h = [NU36.WinCapture]::FindLargestWindow($ProcessId) }
    if ($h -eq [IntPtr]::Zero) { return 'No visible window yet' }
    return [NU36.WinCapture]::Save($h, $Path)
}

function Request-WindowClose([int]$ProcessId) {
    $h = [NU36.WinCapture]::FindLargestWindow($ProcessId)
    if ($h -eq [IntPtr]::Zero) { return $false }
    return [NU36.WinCapture]::RequestClose($h)
}
