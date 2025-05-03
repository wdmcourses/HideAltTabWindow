using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security.Principal;
using System.Diagnostics;
using System.Threading;

class HideAltTabWindow {
  [DllImport("user32.dll")]
  private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

  [DllImport("user32.dll")]
  private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

  [DllImport("user32.dll")]
  private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

  [DllImport("user32.dll")]
  private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

  private const int GWL_EXSTYLE = -20;
  private const int WS_EX_LAYERED = 0x80000;
  private const int WS_EX_TRANSPARENT = 0x20;
  private const int LWA_ALPHA = 0x2;

  static void Main() {
    if (IsRunAsAdministrator()) {
      RemoveFromStartup();
      RestartExplorer();
    } else {
      AddToStartup();
    }

    int retries = 3;
    int delayMs = 1000;

    IntPtr hWnd = IntPtr.Zero;
    for (int i = 0; i < retries; i++) {
      hWnd = FindWindow("XamlExplorerHostIslandWindow", null);
      if (hWnd != IntPtr.Zero) {
        int extendedStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        SetLayeredWindowAttributes(hWnd, 0, 0, LWA_ALPHA);
        break;
      }
      Thread.Sleep(delayMs);
    }
    if (hWnd == IntPtr.Zero) {
      Console.WriteLine("No Alt Tab Window.");
    }
  }

  static void AddToStartup() {
    const string appName = "HideAltTabWindow";
    string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

    using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)) {
      if (key.GetValue(appName) == null) {
        Console.WriteLine("Run at startup? (Y/N)");
        if (Console.ReadKey().Key == ConsoleKey.Y) {
          key.SetValue(appName, executablePath);
          Console.WriteLine("Added to Windows Startup");
        }
      } else {
        Console.WriteLine("Startup entry exists...");
      }
    }
  }

  static void RemoveFromStartup() {
    const string appName = "HideAltTabWindow";
    using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)) {
      if (key.GetValue(appName) != null) {
        key.DeleteValue(appName);
        Console.WriteLine("Removed from Windows Startup");
      } else {
        Console.WriteLine("No startup entry found.");
      }
    }
  }

  static bool IsRunAsAdministrator() {
    WindowsIdentity identity = WindowsIdentity.GetCurrent();
    WindowsPrincipal principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
  }

  static void RestartExplorer() {
    try {
      Process.Start("taskkill", "/F /IM explorer.exe");

      Thread.Sleep(1000);

      Process.Start("explorer.exe");
      Console.WriteLine("Explorer restarted successfully.");
    } catch (Exception ex) {
      Console.WriteLine("Error restarting explorer: " + ex.Message);
    }
  }
}
