using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using GradCountdown.Services;
using GradCountdown.ViewModels;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfApplication = System.Windows.Application;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using WpfSlider = System.Windows.Controls.Slider;

namespace GradCountdown;

public partial class MainWindow : Window
{
    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_VISIBLE = 0x10000000;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int SW_SHOWNORMAL = 1;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SetWindowText(IntPtr hWnd, string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);

    private readonly MainViewModel _viewModel;
    private readonly TrayIconService _trayIconService;
    private Point _dragStartPoint;
    private bool _isDragging;
    private bool _isCardPressed;
    private IntPtr _hwnd;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = (MainViewModel)DataContext;
        _trayIconService = new TrayIconService(_viewModel);

        _viewModel.RequestShow += OnRequestShow;
        _viewModel.RequestHide += OnRequestHide;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.IsTopmost))
        {
            SyncTopmost(_viewModel.IsTopmost);
        }
    }

    private void SyncTopmost(bool isTopmost)
    {
        if (_hwnd == IntPtr.Zero) return;
        IntPtr insertAfter = isTopmost ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(_hwnd, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        
        SetWindowText(_hwnd, "GradCountdown");

        int currentStyle = GetWindowLong(_hwnd, GWL_STYLE);
        SetWindowLong(_hwnd, GWL_STYLE, currentStyle | WS_CAPTION | WS_VISIBLE);

        // 挂载消息钩子以响应快捷方式重复点击唤醒
        HwndSource? source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(WndProc);

        SyncTopmost(_viewModel.IsTopmost);
        ShowWindow(_hwnd, SW_SHOWNORMAL);
        UpdateWindow(_hwnd);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == App.WM_USER_SHOW)
        {
            OnRequestShow();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 恢复上次保存的窗口坐标或默认右下角
        if (_viewModel.SavedWindowLeft.HasValue && _viewModel.SavedWindowTop.HasValue)
        {
            double left = _viewModel.SavedWindowLeft.Value;
            double top = _viewModel.SavedWindowTop.Value;

            if (left >= SystemParameters.VirtualScreenLeft &&
                left <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 50 &&
                top >= SystemParameters.VirtualScreenTop &&
                top <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 50)
            {
                Left = left;
                Top = top;
                return;
            }
        }

        // 默认定位在主屏幕右下角
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 20;
        Top = workArea.Bottom - ActualHeight - 20;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _trayIconService.Dispose();
    }

    private void OnRequestShow()
    {
        Show();
        WindowState = WindowState.Normal;
        SyncTopmost(_viewModel.IsTopmost);
        Activate();
        Focus();
    }

    private void OnRequestHide()
    {
        Hide();
    }

    private static bool IsInteractiveControl(DependencyObject? obj)
    {
        while (obj != null)
        {
            if (obj is WpfButton ||
                obj is WpfSlider ||
                obj is WpfCheckBox ||
                obj is Thumb ||
                obj is RepeatButton ||
                obj is Track ||
                obj is WpfMenuItem ||
                obj is Popup)
            {
                return true;
            }
            obj = VisualTreeHelper.GetParent(obj);
        }
        return false;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 如果点击源属于按钮、滑块、复选框等交互控件，绝不触发主卡片的拖拽或点击检测
        if (IsInteractiveControl(e.OriginalSource as DependencyObject))
        {
            _isCardPressed = false;
            _isDragging = false;
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;
        _isCardPressed = true;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isCardPressed) return;

        // 如果开启了锁定位置，则禁止拖拽移动
        if (_viewModel.LockPosition) return;

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPoint = e.GetPosition(this);
            Vector diff = _dragStartPoint - currentPoint;

            if (!_isDragging && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                _isDragging = true;
                try
                {
                    DragMove();
                }
                catch { }
            }
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 如果点击源是交互控件，直接退出
        if (IsInteractiveControl(e.OriginalSource as DependencyObject))
        {
            _isCardPressed = false;
            _isDragging = false;
            return;
        }

        if (_isDragging)
        {
            // 拖动结束，保存新坐标
            _viewModel.SavePosition(Left, Top);
        }
        else if (_isCardPressed)
        {
            // 如果设置弹窗是打开状态，点击卡片空白处则关闭弹窗；如果弹窗是关闭的，才切换倒计时模式
            if (_viewModel.IsSettingsOpen)
            {
                _viewModel.IsSettingsOpen = false;
            }
            else
            {
                _viewModel.ToggleMode();
            }
        }

        _isDragging = false;
        _isCardPressed = false;
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _viewModel.IsSettingsOpen = !_viewModel.IsSettingsOpen;
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsSettingsOpen = true;
    }

    private void ResetScale_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetScale();
    }

    private void ResetOpacity_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetOpacity();
    }

    private void Hide_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.HideWindow();
    }

    private void ToggleMode_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleMode();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        WpfApplication.Current.Shutdown();
    }
}