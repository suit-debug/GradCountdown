using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using GradCountdown.Services;

Console.WriteLine("=== GradCountdown 全量自动化测试套件 ===");

// 1. 生成 Application Icon (.ico) - 使用项目相对路径
string baseDir = AppDomain.CurrentDomain.BaseDirectory;
string iconPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\GradCountdown\Resources\app.ico"));
GenerateAppIcon(iconPath);
Console.WriteLine($"[OK] 已更新应用图标: {iconPath}");

// 2. 边界测试 1: 2025-08-31 (入学前一天)
DateOnly d1 = new(2025, 8, 31);
CountdownSnapshot s1 = CountdownService.Calculate(d1);
string passedText1 = CountdownFormatter.ElapsedValue(s1);
string remainingText1 = CountdownFormatter.GraduationValue(s1);
Assert(passedText1 == "尚未入学", $"2025-08-31 生涯应显示「尚未入学」，实际为「{passedText1}」");
Assert(remainingText1 == "1035", $"2025-08-31 毕业剩余应为 1035，实际为「{remainingText1}」");
Assert(CountdownService.GetDaysPassedText(new DateTime(2025, 8, 31)) == "尚未入学", "GetDaysPassedText 兼容性测试失败");
Console.WriteLine($"[PASS] 2025-08-31 边界测试通过 (生涯={passedText1}, 毕业剩余={remainingText1})");

// 3. 边界测试 2: 2025-09-01 (入学当天)
DateOnly d2 = new(2025, 9, 1);
CountdownSnapshot s2 = CountdownService.Calculate(d2);
string passedText2 = CountdownFormatter.ElapsedValue(s2);
string remainingText2 = CountdownFormatter.GraduationValue(s2);
Assert(s2.ElapsedDays == 0 && passedText2 == "0", "2025-09-01 已度过应为 0");
Assert(s2.RemainingDays == 1034 && remainingText2 == "1034", "2025-09-01 毕业剩余应为 1034");
Console.WriteLine($"[PASS] 2025-09-01 入学日测试通过 (已度过={s2.ElapsedDays}, 毕业剩余={s2.RemainingDays})");

// 4. 基准测试 3: 2026-08-19 (学期中)
DateOnly d3 = new(2026, 8, 19);
CountdownSnapshot s3 = CountdownService.Calculate(d3);
Assert(s3.ElapsedDays == 352, $"2026-08-19 已度过应为 352，实际为 {s3.ElapsedDays}");
Assert(s3.RemainingDays == 682, $"2026-08-19 毕业剩余应为 682，实际为 {s3.RemainingDays}");
Assert(s3.TotalDays == 1034, $"总学业天数应为 1034，实际为 {s3.TotalDays}");
Console.WriteLine($"[PASS] 2026-08-19 基准测试通过 (已度过={s3.ElapsedDays}, 毕业剩余={s3.RemainingDays}, 进度={s3.Progress:P2})");

// 5. 边界测试 4: 2028-07-01 (毕业当天)
DateOnly d4 = new(2028, 7, 1);
CountdownSnapshot s4 = CountdownService.Calculate(d4);
string remainingText4 = CountdownFormatter.GraduationValue(s4);
Assert(s4.ElapsedDays == 1034, "2028-07-01 已度过应为 1034");
Assert(s4.RemainingDays == 0 && remainingText4 == "0", "2028-07-01 毕业剩余应为 0");
Console.WriteLine($"[PASS] 2028-07-01 毕业日测试通过 (已度过={s4.ElapsedDays}, 毕业剩余={s4.RemainingDays})");

// 6. 边界测试 5: 2028-07-02 (毕业后一天)
DateOnly d5 = new(2028, 7, 2);
CountdownSnapshot s5 = CountdownService.Calculate(d5);
string remainingText5 = CountdownFormatter.GraduationValue(s5);
Assert(s5.ElapsedDays == 1035, "2028-07-02 已度过应为 1035");
Assert(remainingText5 == "已毕业", $"2028-07-02 毕业剩余应显示「已毕业」，实际为「{remainingText5}」");
Console.WriteLine($"[PASS] 2028-07-02 毕业后测试通过 (已度过={s5.ElapsedDays}, 毕业状态={remainingText5})");

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("全部 5 组边界与基准测试 100% 验证通过！");
Console.WriteLine("========================================");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[FAIL] 断言失败: {message}");
        Console.ResetColor();
        Environment.Exit(1);
    }
}

static void GenerateAppIcon(string outputPath)
{
    int[] sizes = new int[] { 256, 48, 32, 16 };
    using MemoryStream ms = new();
    using BinaryWriter bw = new(ms);

    bw.Write((short)0);
    bw.Write((short)1);
    bw.Write((short)sizes.Length);

    int offset = 6 + (sizes.Length * 16);
    byte[][] pngBuffers = new byte[sizes.Length][];

    for (int i = 0; i < sizes.Length; i++)
    {
        int size = sizes[i];
        using Bitmap bmp = new(size, size, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        float margin = size * 0.05f;
        float cornerRadius = size * 0.22f;
        RectangleF rect = new(margin, margin, size - 2 * margin, size - 2 * margin);
        using GraphicsPath path = CreateRoundedRectangle(rect, cornerRadius);
        using LinearGradientBrush brush = new(
            rect,
            Color.FromArgb(255, 46, 139, 153),
            Color.FromArgb(255, 30, 102, 119),
            LinearGradientMode.ForwardDiagonal);
        g.FillPath(brush, path);

        using SolidBrush capBrush = new(Color.White);
        using Pen capPen = new(Color.White, Math.Max(1.5f, size * 0.04f));
        capPen.StartCap = LineCap.Round;
        capPen.EndCap = LineCap.Round;
        capPen.LineJoin = LineJoin.Round;

        PointF pTop = new(size * 0.5f, size * 0.26f);
        PointF pRight = new(size * 0.82f, size * 0.44f);
        PointF pBottom = new(size * 0.5f, size * 0.62f);
        PointF pLeft = new(size * 0.18f, size * 0.44f);
        g.FillPolygon(capBrush, new PointF[] { pTop, pRight, pBottom, pLeft });

        PointF[] skullCap = new PointF[] {
            new(size * 0.32f, size * 0.54f),
            new(size * 0.32f, size * 0.70f),
            new(size * 0.50f, size * 0.80f),
            new(size * 0.68f, size * 0.70f),
            new(size * 0.68f, size * 0.54f)
        };
        g.FillPolygon(capBrush, skullCap);

        g.DrawLine(capPen, size * 0.78f, size * 0.46f, size * 0.82f, size * 0.72f);

        using MemoryStream pngMs = new();
        bmp.Save(pngMs, ImageFormat.Png);
        pngBuffers[i] = pngMs.ToArray();
    }

    for (int i = 0; i < sizes.Length; i++)
    {
        int size = sizes[i];
        bw.Write((byte)(size == 256 ? 0 : size));
        bw.Write((byte)(size == 256 ? 0 : size));
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write((short)1);
        bw.Write((short)32);
        bw.Write((int)pngBuffers[i].Length);
        bw.Write((int)offset);
        offset += pngBuffers[i].Length;
    }

    for (int i = 0; i < sizes.Length; i++)
    {
        bw.Write(pngBuffers[i]);
    }

    string? dir = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }
    File.WriteAllBytes(outputPath, ms.ToArray());
}

static GraphicsPath CreateRoundedRectangle(RectangleF rect, float radius)
{
    GraphicsPath path = new();
    float diameter = radius * 2;
    path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
    path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
    path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
    path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
    path.CloseFigure();
    return path;
}
