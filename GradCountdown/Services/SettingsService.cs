using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GradCountdown.Models;

namespace GradCountdown.Services;

/// <summary>
/// 用户设置读写服务，数据保存在 %LOCALAPPDATA%\GradCountdown\settings.json
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GradCountdown");

    public static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

    /// <summary>
    /// 读取设置；文件不存在或解析失败时回落到默认值
    /// </summary>
    public static AppSettings LoadSettings()
    {
        AppSettings? settings = null;

        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            App.Log($"SettingsService.LoadSettings failed: {ex.Message}");
        }

        settings ??= new AppSettings();

        // 约束数值范围
        settings.Scale = Math.Clamp(Math.Round(settings.Scale, 2), 0.60, 1.30);
        settings.Opacity = Math.Clamp(Math.Round(settings.Opacity, 2), 0.60, 1.00);

        // 以系统注册表真实状态作为开机自启最终依据
        bool regAutoStart = AutoStartService.IsAutoStartEnabled();
        if (settings.IsAutoStart != regAutoStart)
        {
            settings.IsAutoStart = regAutoStart;
        }

        return settings;
    }

    /// <summary>
    /// 写入设置至本地磁盘
    /// </summary>
    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            string tempPath = SettingsFilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, SettingsFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            App.Log($"SettingsService.SaveSettings failed: {ex.Message}");
        }
    }

    // 实例方法适配兼容
    public AppSettings Load() => LoadSettings();
    public void Save(AppSettings settings) => SaveSettings(settings);
}
