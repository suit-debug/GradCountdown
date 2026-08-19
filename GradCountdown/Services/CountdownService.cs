namespace GradCountdown.Services;

/// <summary>
/// 研究生生涯所处阶段
/// </summary>
public enum CountdownPhase
{
    /// <summary>今天 &lt; 入学日</summary>
    BeforeStart,

    /// <summary>入学日 &lt;= 今天 &lt;= 毕业日</summary>
    InProgress,

    /// <summary>今天 &gt; 毕业日</summary>
    Graduated
}

/// <summary>
/// 某一天的倒计时快照，只包含自然日，不含任何时分秒
/// </summary>
public readonly record struct CountdownSnapshot(
    DateOnly Today,
    int ElapsedDays,
    int RemainingDays,
    int TotalDays,
    CountdownPhase Phase)
{
    /// <summary>生涯进度 0.0 ~ 1.0</summary>
    public double Progress => TotalDays <= 0
        ? 1.0
        : Math.Clamp((double)ElapsedDays / TotalDays, 0.0, 1.0);
}

/// <summary>
/// 纯日期计算服务：不依赖 WPF，不联网，动态读取本地系统时间。
/// </summary>
public static class CountdownService
{
    /// <summary>研究生入学日期（生涯起始日）：2025-09-01</summary>
    public static readonly DateTime EnrollmentDate = new(2025, 9, 1);

    /// <summary>预计毕业日期（倒计时目标日）：2028-07-01</summary>
    public static readonly DateTime GraduationDate = new(2028, 7, 1);

    public static readonly DateOnly StartDateOnly = new(2025, 9, 1);
    public static readonly DateOnly GraduationDateOnly = new(2028, 7, 1);

    public const string CohortName = "25级研究生";
    public const string GraduationTitle = "25级研究生毕业还有";
    public const string ElapsedTitle = "研究生生涯已经";
    public const string GraduationFooter = "目标日： 2028-07-01";
    public const string StartFooter = "起始日： 2025-09-01";
    public const string NotEnrolledText = "尚未入学";
    public const string GraduatedText = "已毕业";

    /// <summary>入学日到毕业日之间的自然日总数（1034 天）</summary>
    public static int TotalDays => (GraduationDate.Date - EnrollmentDate.Date).Days;

    /// <summary>
    /// 获取当前日期相对入学日期已度过的天数数值
    /// </summary>
    public static int GetDaysPassed(DateTime? current = null)
    {
        var today = (current ?? DateTime.Today).Date;
        return (today - EnrollmentDate.Date).Days;
    }

    /// <summary>
    /// 获取当前日期距离毕业日期剩余的天数数值
    /// </summary>
    public static int GetDaysRemaining(DateTime? current = null)
    {
        var today = (current ?? DateTime.Today).Date;
        return (GraduationDate.Date - today).Days;
    }

    /// <summary>
    /// 获取已度过天数的界面显示文本（入学前显示「尚未入学」，绝不显示负数）
    /// </summary>
    public static string GetDaysPassedText(DateTime? current = null)
    {
        var today = (current ?? DateTime.Today).Date;
        if (today < EnrollmentDate.Date)
        {
            return NotEnrolledText;
        }
        return (today - EnrollmentDate.Date).Days.ToString();
    }

    /// <summary>
    /// 获取毕业倒计时的界面显示文本（毕业后显示「已毕业」，绝不显示负数）
    /// </summary>
    public static string GetDaysRemainingText(DateTime? current = null)
    {
        var today = (current ?? DateTime.Today).Date;
        if (today > GraduationDate.Date)
        {
            return GraduatedText;
        }
        return (GraduationDate.Date - today).Days.ToString();
    }

    /// <summary>
    /// 获取已度过天数占总学业天数的比例（0.0 ~ 1.0）
    /// </summary>
    public static double GetProgressRatio(DateTime? current = null)
    {
        int passed = GetDaysPassed(current);
        if (TotalDays <= 0) return 0.0;
        double ratio = (double)passed / TotalDays;
        return Math.Clamp(ratio, 0.0, 1.0);
    }

    /// <summary>
    /// 使用 Windows 当前本地系统日期计算快照
    /// </summary>
    public static CountdownSnapshot CalculateForToday() =>
        Calculate(DateOnly.FromDateTime(DateTime.Today));

    /// <summary>
    /// 按指定日期计算快照
    /// </summary>
    public static CountdownSnapshot Calculate(DateOnly today) =>
        Calculate(today, StartDateOnly, GraduationDateOnly);

    /// <summary>
    /// 核心算法：按日期范围计算快照
    /// </summary>
    public static CountdownSnapshot Calculate(DateOnly today, DateOnly start, DateOnly graduation)
    {
        int total = Math.Max(0, graduation.DayNumber - start.DayNumber);
        int rawElapsed = today.DayNumber - start.DayNumber;
        int rawRemaining = graduation.DayNumber - today.DayNumber;

        CountdownPhase phase;
        int elapsed;
        int remaining;

        if (rawElapsed < 0)
        {
            phase = CountdownPhase.BeforeStart;
            elapsed = 0;
            remaining = Math.Max(0, rawRemaining);
        }
        else if (rawRemaining < 0)
        {
            phase = CountdownPhase.Graduated;
            elapsed = Math.Max(0, rawElapsed);
            remaining = 0;
        }
        else
        {
            phase = CountdownPhase.InProgress;
            elapsed = rawElapsed;
            remaining = rawRemaining;
        }

        return new CountdownSnapshot(today, elapsed, remaining, total, phase);
    }
}

/// <summary>
/// 倒计时格式化辅助类
/// </summary>
public static class CountdownFormatter
{
    public static string GraduationValue(CountdownSnapshot s) =>
        s.Phase == CountdownPhase.Graduated
            ? CountdownService.GraduatedText
            : s.RemainingDays.ToString();

    public static string ElapsedValue(CountdownSnapshot s) =>
        s.Phase == CountdownPhase.BeforeStart
            ? CountdownService.NotEnrolledText
            : s.ElapsedDays.ToString();
}
