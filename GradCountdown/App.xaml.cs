using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using GradCountdown.Services;
using WpfApplication = System.Windows.Application;

namespace GradCountdown;

public partial class App : WpfApplication
{
    private static Mutex? _instanceMutex;
    private const string MutexName = "Local\\GradCountdown_SingleInstance_Mutex";
    public const int WM_USER_SHOW = 0x0400 + 101;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    public static void Log(string message)
    {
        try
        {
            string logDir = SettingsService.SettingsDirectory;
            Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "app.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // 单实例检查
        _instanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            Log("Another instance is already running. Activating existing instance and exiting.");
            try
            {
                IntPtr existingHwnd = FindWindow(null, "GradCountdown");
                if (existingHwnd != IntPtr.Zero)
                {
                    PostMessage(existingHwnd, WM_USER_SHOW, IntPtr.Zero, IntPtr.Zero);
                    ShowWindow(existingHwnd, SW_SHOW);
                    ShowWindow(existingHwnd, SW_RESTORE);
                    SetForegroundWindow(existingHwnd);
                }
            }
            catch { }

            Shutdown(0);
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (s, ev) => 
            Log($"[FATAL] AppDomain UnhandledException: {ev.ExceptionObject}");

        DispatcherUnhandledException += (s, ev) =>
        {
            Log($"[FATAL] DispatcherUnhandledException: {ev.Exception}");
            ev.Handled = true;
        };

        base.OnStartup(e);

        try
        {
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;

            mainWindow.Show();

            var handle = new WindowInteropHelper(mainWindow).Handle;
            Log($"MainWindow HWND: 0x{handle.ToInt64():X}");

            mainWindow.Activate();
            mainWindow.Focus();
        }
        catch (Exception ex)
        {
            Log($"[FATAL] Exception in OnStartup: {ex}");
            System.Windows.MessageBox.Show($"启动失败: {ex.Message}\n{ex.StackTrace}", "GradCountdown 错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_instanceMutex != null)
        {
            try
            {
                _instanceMutex.ReleaseMutex();
            }
            catch { }
            _instanceMutex.Dispose();
            _instanceMutex = null;
        }
        base.OnExit(e);
    }
}
