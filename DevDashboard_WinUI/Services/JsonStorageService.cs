using System.Text.Json;
using DevDashboard.Models;

namespace DevDashboard.Services;

/// <summary>
/// JSON 파일 기반 앱 설정 저장/불러오기 서비스.
/// 저장 위치: [설치폴더]\settings\settings.json
/// </summary>
public class JsonStorageService
{
    private static readonly string SettingsPath = Path.Combine(
        AppContext.BaseDirectory,
        "settings",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>
    /// 설정 파일을 불러옵니다. 파일이 없거나 손상된 경우 기본값을 반환합니다.
    /// </summary>
    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            // 파일 손상 시 기본 설정으로 복구
            return new AppSettings();
        }
    }

    /// <summary>
    /// 설정을 JSON 파일에 저장합니다. 디렉터리가 없으면 자동 생성합니다.
    /// </summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json, System.Text.Encoding.UTF8);
    }
}
