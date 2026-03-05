using System.Text.Json;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// ApplicationData.Current.LocalSettings 기반 앱 설정 저장/불러오기 서비스.
/// JSON 문자열로 직렬화하여 LocalSettings에 저장합니다.
/// 데이터 부재 또는 손상 시 기본값을 반환합니다.
/// </summary>
public class JsonStorageService
{
    private const string SettingsKey = "AppSettings";

    private static Windows.Storage.ApplicationDataContainer LocalSettings =>
        Windows.Storage.ApplicationData.Current.LocalSettings;

    /// <summary>
    /// 설정을 불러옵니다.
    /// 값이 없거나 손상된 경우 기본값을 반환합니다.
    /// </summary>
    public AppSettings Load()
    {
        try
        {
            if (LocalSettings.Values[SettingsKey] is not string json)
                return new AppSettings();

            return JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    /// <summary>설정을 LocalSettings에 저장합니다.</summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var json = JsonSerializer.Serialize(settings, AppJsonContext.Default.AppSettings);
        LocalSettings.Values[SettingsKey] = json;
    }
}
