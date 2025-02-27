// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WindowOverlayManager;

public static class Logger
{
    private static readonly string logFilePath = "app.log";

    public static void LogInfo(string message) => WriteLog("INFO", message);
    public static void LogWarning(string message) => WriteLog("WARN", message);
    public static void LogError(string message) => WriteLog("ERROR", message);

    private static void WriteLog(string level, string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        try
        {
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }
        catch
        {
            Console.WriteLine($"Logging failed: {logEntry}");
        }
    }
}

public class WindowSettingsRecord
{
    public uint ProcessId { get; set; }
    public string Title { get; set; }
    public byte Transparency { get; set; }
    public bool ClickThrough { get; set; }
    public bool AlwaysOnTop { get; set; }
}

public static class SettingsManager
{
    private static readonly string settingsFilePath = "settings.json";
    public static Dictionary<string, WindowSettingsRecord> Settings { get; private set; } = new Dictionary<string, WindowSettingsRecord>();

    public static void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(settingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, WindowSettingsRecord>>(json);
                if (loadedSettings != null)
                {
                    Settings = loadedSettings;
                    Logger.LogInfo("Settings loaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load settings: {ex.Message}");
            }
        }
        else
        {
            Logger.LogInfo("No settings file found; starting with fresh settings.");
        }
    }

    public static void SaveSettings()
    {
        try
        {
            string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFilePath, json);
            Logger.LogInfo("Settings saved successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to save settings: {ex.Message}");
        }
    }

    public static string GetWindowKey(uint processId, string title) => $"{processId}:{title}";
}

public class WindowInfo
{
    public IntPtr Hwnd { get; set; }
    public string Title { get; set; }
    public uint ProcessId { get; set; }
}

public static class WindowManager
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // New API for always-on-top
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy,
        uint uFlags);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const uint LWA_ALPHA = 0x2;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    public static List<WindowInfo> EnumerateWindows()
    {
        List<WindowInfo> windows = new List<WindowInfo>();
        IntPtr shellWindow = GetShellWindow();

        try
        {
            EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == shellWindow || !IsWindowVisible(hWnd))
                    return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                    return true;

                StringBuilder builder = new StringBuilder(length + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                string title = builder.ToString();
                if (string.IsNullOrWhiteSpace(title))
                    return true;

                GetWindowThreadProcessId(hWnd, out uint processId);
                windows.Add(new WindowInfo { Hwnd = hWnd, Title = title, ProcessId = processId });
                return true;
            }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error enumerating windows: {ex.Message}");
        }
        return windows;
    }

    public static bool ApplySettings(IntPtr hWnd, byte transparency, bool clickThrough, bool alwaysOnTop)
    {
        try
        {
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if (exStyle == 0)
                Logger.LogWarning($"GetWindowLong returned 0; error code: {Marshal.GetLastWin32Error()}");

            exStyle |= WS_EX_LAYERED;
            exStyle = clickThrough ? (exStyle | WS_EX_TRANSPARENT) : (exStyle & ~WS_EX_TRANSPARENT);
            int result = SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);
            if (result == 0)
                Logger.LogWarning($"SetWindowLong returned 0; error code: {Marshal.GetLastWin32Error()}");

            if (!SetLayeredWindowAttributes(hWnd, 0, transparency, LWA_ALPHA))
            {
                Logger.LogError($"SetLayeredWindowAttributes failed; error code: {Marshal.GetLastWin32Error()}");
                return false;
            }

            // Set always on top if required
            IntPtr topMostFlag = alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
            bool posResult = SetWindowPos(hWnd, topMostFlag, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            if (!posResult)
            {
                Logger.LogError($"SetWindowPos failed; error code: {Marshal.GetLastWin32Error()}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Exception in ApplySettings: {ex.Message}");
            return false;
        }
    }
}

public static class UIHelper
{
    public static byte PromptForByte(string prompt, byte defaultValue)
    {
        Console.Write(prompt);
        string input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            return defaultValue;
        if (byte.TryParse(input, out byte result))
            return result;
        Console.WriteLine("Invalid input. Using default value.");
        return defaultValue;
    }

    public static bool PromptForBool(string prompt, bool defaultValue)
    {
        Console.Write(prompt);
        string input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            return defaultValue;
        return input.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    public static void Pause(string message = "Press any key to continue...")
    {
        Console.WriteLine(message);
        Console.ReadKey();
    }
}

class Program
{
    static void Main(string[] args)
    {
        Logger.LogInfo("Application started.");
        SettingsManager.LoadSettings();

        while (true)
        {
            List<WindowInfo> windows = WindowManager.EnumerateWindows();
            DisplayWindowList(windows);

            Console.WriteLine("Select a window by number (0 to refresh, -1 to exit):");
            if (!int.TryParse(Console.ReadLine(), out int choice))
            {
                Console.WriteLine("Invalid input.");
                UIHelper.Pause();
                continue;
            }
            if (choice == -1)
                break;
            if (choice == 0)
                continue;
            if (choice < 1 || choice > windows.Count)
            {
                Console.WriteLine("Invalid selection.");
                UIHelper.Pause();
                continue;
            }
            ProcessWindowSelection(windows[choice - 1]);
        }
        Logger.LogInfo("Application exiting.");
    }

    private static void DisplayWindowList(List<WindowInfo> windows)
    {
        Console.Clear();
        Console.WriteLine("=== Window Overlay Manager ===");
        if (windows.Count == 0)
        {
            Console.WriteLine("No windows found.");
            return;
        }
        Console.WriteLine("List of available windows:");
        for (int i = 0; i < windows.Count; i++)
        {
            string key = SettingsManager.GetWindowKey(windows[i].ProcessId, windows[i].Title);
            bool hasSetting = SettingsManager.Settings.ContainsKey(key);
            Console.Write($"{i + 1}. [PID: {windows[i].ProcessId}] [HWND: {windows[i].Hwnd}] {windows[i].Title}");
            if (hasSetting)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(" *");
                Console.ResetColor();
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    private static void ProcessWindowSelection(WindowInfo window)
    {
        string key = SettingsManager.GetWindowKey(window.ProcessId, window.Title);
        SettingsManager.Settings.TryGetValue(key, out WindowSettingsRecord currentSetting);

        Console.WriteLine("Current settings for this window:");
        Console.WriteLine(currentSetting != null
            ? $" Transparency: {currentSetting.Transparency}\n Click-through: {currentSetting.ClickThrough}\n Always on top: {currentSetting.AlwaysOnTop}"
            : " No settings currently saved for this window.");

        byte defaultTransparency = currentSetting?.Transparency ?? (byte)255;
        bool defaultClickThrough = currentSetting?.ClickThrough ?? false;
        bool defaultAlwaysOnTop = currentSetting?.AlwaysOnTop ?? false;

        byte transparency = UIHelper.PromptForByte("Enter transparency level (0-255) (press Enter to keep current): ", defaultTransparency);
        bool clickThrough = UIHelper.PromptForBool("Should the window be click-through? (y/n) (press Enter to keep current): ", defaultClickThrough);
        bool alwaysOnTop = UIHelper.PromptForBool("Should the window always be on top? (y/n) (press Enter to keep current): ", defaultAlwaysOnTop);

        bool success = WindowManager.ApplySettings(window.Hwnd, transparency, clickThrough, alwaysOnTop);
        if (success)
        {
            var newSetting = new WindowSettingsRecord
            {
                ProcessId = window.ProcessId,
                Title = window.Title,
                Transparency = transparency,
                ClickThrough = clickThrough,
                AlwaysOnTop = alwaysOnTop
            };
            SettingsManager.Settings[key] = newSetting;
            SettingsManager.SaveSettings();
            Console.WriteLine("Settings applied and saved.");
            Logger.LogInfo($"Applied settings to window '{window.Title}' (PID: {window.ProcessId}).");
        }
        else
        {
            Console.WriteLine("Failed to apply settings.");
            Logger.LogError($"Failed to apply settings to window '{window.Title}' (PID: {window.ProcessId}).");
        }
        UIHelper.Pause();
    }
}
