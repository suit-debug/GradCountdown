namespace GradCountdown.Models;

/// <summary>
/// 倒计时 Widget 显示模式
/// </summary>
public enum DisplayMode
{
    /// <summary>
    /// 模式 A：25级研究生毕业还有（毕业倒计时）
    /// </summary>
    GraduationRemaining,

    /// <summary>
    /// 模式 B：研究生生涯已经（已度过天数）
    /// </summary>
    CareerPassed
}

/// <summary>
/// 应用程序设置模型，保存在 %LOCALAPPDATA%\GradCountdown\settings.json
/// </summary>
public sealed class AppSettings
{
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public DisplayMode DisplayMode { get; set; } = DisplayMode.GraduationRemaining;
    public double Scale { get; set; } = 0.85;
    public double Opacity { get; set; } = 0.95;
    public bool LockPosition { get; set; } = false;
    public bool IsTopmost { get; set; } = false;
    public bool IsAutoStart { get; set; } = false;
}
