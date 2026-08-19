using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using GradCountdown.Models;
using GradCountdown.Services;

namespace GradCountdown.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private DisplayMode _currentMode = DisplayMode.GraduationRemaining;
    private double _uiScale = 0.85;
    private double _uiOpacity = 0.95;
    private bool _lockPosition = false;
    private bool _isTopmost = false;
    private bool _isAutoStart = false;
    private bool _isSettingsOpen;
    private DateTime _lastUpdatedDate;
    private readonly DispatcherTimer _timer;
    private readonly AppSettings _settings;

    public event Action? RequestShow;
    public event Action? RequestHide;

    public MainViewModel()
    {
        _settings = SettingsService.LoadSettings();
        _uiScale = _settings.Scale;
        _uiOpacity = _settings.Opacity;
        _lockPosition = _settings.LockPosition;
        _isTopmost = _settings.IsTopmost;
        _isAutoStart = _settings.IsAutoStart;
        _currentMode = _settings.DisplayMode;

        _lastUpdatedDate = DateTime.Today;
        Refresh();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _timer.Tick += (s, e) =>
        {
            if (DateTime.Today != _lastUpdatedDate)
            {
                _lastUpdatedDate = DateTime.Today;
                Refresh();
            }
        };
        _timer.Start();
    }

    public double? SavedWindowLeft => _settings.WindowLeft;
    public double? SavedWindowTop => _settings.WindowTop;

    public DisplayMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode != value)
            {
                _currentMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGraduationMode));
                OnPropertyChanged(nameof(HeaderTitle));
                OnPropertyChanged(nameof(DisplayDays));
                OnPropertyChanged(nameof(DisplayFontSize));
                OnPropertyChanged(nameof(FooterText));

                _settings.DisplayMode = _currentMode;
                SettingsService.SaveSettings(_settings);
            }
        }
    }

    public double UiScale
    {
        get => _uiScale;
        set
        {
            double clamped = Math.Clamp(Math.Round(value, 2), 0.60, 1.30);
            if (Math.Abs(_uiScale - clamped) > 0.001)
            {
                _uiScale = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UiScalePercentageText));

                _settings.Scale = _uiScale;
                SettingsService.SaveSettings(_settings);
            }
        }
    }

    public string UiScalePercentageText => $"{Math.Round(_uiScale * 100)}%";

    public double UiOpacity
    {
        get => _uiOpacity;
        set
        {
            double clamped = Math.Clamp(Math.Round(value, 2), 0.60, 1.00);
            if (Math.Abs(_uiOpacity - clamped) > 0.001)
            {
                _uiOpacity = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UiOpacityPercentageText));

                _settings.Opacity = _uiOpacity;
                SettingsService.SaveSettings(_settings);
            }
        }
    }

    public string UiOpacityPercentageText => $"{Math.Round(_uiOpacity * 100)}%";

    public bool LockPosition
    {
        get => _lockPosition;
        set
        {
            if (_lockPosition != value)
            {
                _lockPosition = value;
                OnPropertyChanged();

                _settings.LockPosition = _lockPosition;
                SettingsService.SaveSettings(_settings);
            }
        }
    }

    public bool IsTopmost
    {
        get => _isTopmost;
        set
        {
            if (_isTopmost != value)
            {
                _isTopmost = value;
                OnPropertyChanged();

                _settings.IsTopmost = _isTopmost;
                SettingsService.SaveSettings(_settings);
            }
        }
    }

    public bool IsAutoStart
    {
        get => _isAutoStart;
        set
        {
            if (_isAutoStart != value)
            {
                _isAutoStart = value;
                AutoStartService.SetAutoStart(_isAutoStart);
                OnPropertyChanged();

                _settings.IsAutoStart = _isAutoStart;
                SettingsService.SaveSettings(_settings);
            }
        }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (_isSettingsOpen != value)
            {
                _isSettingsOpen = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsGraduationMode => _currentMode == DisplayMode.GraduationRemaining;

    public string HeaderTitle => IsGraduationMode ? CountdownService.GraduationTitle : CountdownService.ElapsedTitle;

    public string DisplayDays
    {
        get
        {
            return IsGraduationMode
                ? CountdownService.GetDaysRemainingText()
                : CountdownService.GetDaysPassedText();
        }
    }

    public double DisplayFontSize
    {
        get
        {
            string text = DisplayDays;
            if (text.Length >= 4) return 38;
            if (text.Length == 3) return 56;
            return 58;
        }
    }

    public string FooterText => IsGraduationMode ? CountdownService.GraduationFooter : CountdownService.StartFooter;

    public void ToggleMode()
    {
        CurrentMode = _currentMode == DisplayMode.GraduationRemaining
            ? DisplayMode.CareerPassed
            : DisplayMode.GraduationRemaining;
    }

    public void ResetScale()
    {
        UiScale = 0.85;
    }

    public void ResetOpacity()
    {
        UiOpacity = 0.95;
    }

    public void SavePosition(double left, double top)
    {
        _settings.WindowLeft = left;
        _settings.WindowTop = top;
        SettingsService.SaveSettings(_settings);
    }

    public void ShowWindow() => RequestShow?.Invoke();

    public void HideWindow() => RequestHide?.Invoke();

    public void Refresh()
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(DisplayDays));
        OnPropertyChanged(nameof(DisplayFontSize));
        OnPropertyChanged(nameof(FooterText));
        OnPropertyChanged(nameof(IsGraduationMode));
        OnPropertyChanged(nameof(UiScale));
        OnPropertyChanged(nameof(UiScalePercentageText));
        OnPropertyChanged(nameof(UiOpacity));
        OnPropertyChanged(nameof(UiOpacityPercentageText));
        OnPropertyChanged(nameof(LockPosition));
        OnPropertyChanged(nameof(IsTopmost));
        OnPropertyChanged(nameof(IsAutoStart));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
