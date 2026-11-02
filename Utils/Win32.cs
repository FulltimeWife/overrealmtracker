using System.Runtime.InteropServices;

namespace OverrealmTracker.Utils;

internal static class Win32
{
  public const int GWL_EXSTYLE = -20;

  public const int WS_EX_TOOLWINDOW = 0x00000080;
  public const int WS_EX_APPWINDOW = 0x00040000;
  public const int WS_EX_LAYERED = 0x00080000;

  [DllImport("user32.dll", SetLastError = true)]
  public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

  [DllImport("user32.dll", SetLastError = true)]
  public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

  public static void SetAppWindow(IntPtr hWnd, bool appWindow)
  {
    var ex = GetWindowLong(hWnd, GWL_EXSTYLE);
    if (appWindow)
    {
      ex &= ~WS_EX_TOOLWINDOW;
      ex |= WS_EX_APPWINDOW;
    }
    else
    {
      ex &= ~WS_EX_APPWINDOW;
      ex |= WS_EX_TOOLWINDOW;
    }
    SetWindowLong(hWnd, GWL_EXSTYLE, ex);
  }

  public static void SetLayered(IntPtr hWnd, bool layered)
  {
    var ex = GetWindowLong(hWnd, GWL_EXSTYLE);
    if (layered) ex |= WS_EX_LAYERED;
    else ex &= ~WS_EX_LAYERED;
    SetWindowLong(hWnd, GWL_EXSTYLE, ex);
  }
}
