using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// WinUI 표준 ResourceLoader 기반 다국어 문자열 서비스.
/// .resw 파일(Strings/ko-KR, Strings/en-US)에서 현재 언어에 맞는 문자열을 반환합니다.
/// </summary>
public static class LocalizationService
{
    private static ResourceLoader? _loader;

    /// <summary>ResourceLoader 인스턴스를 지연 초기화하여 반환합니다.</summary>
    private static ResourceLoader Loader =>
        _loader ??= new ResourceLoader();

    /// <summary>캐시된 ResourceLoader를 초기화하고 리소스 시스템에 새 언어를 알립니다.</summary>
    public static void Reset()
    {
        _loader = null;

        // PrimaryLanguageOverride만으로는 같은 프로세스 내 ResourceLoader가 새 언어를 인식하지 못함.
        // 리소스 컨텍스트의 Language qualifier를 명시적으로 갱신하여 즉시 적용.
        try
        {
            var lang = Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
            if (!string.IsNullOrEmpty(lang))
                ResourceContext.SetGlobalQualifierValue("Language", lang);
        }
        catch
        {
            // 리소스 컨텍스트 갱신 실패 시 무시 — 앱 재시작 시 적용
        }
    }

    /// <summary>
    /// 리소스 키로 현재 언어의 문자열을 반환합니다.
    /// 키가 없으면 키 자체를 반환합니다.
    /// </summary>
    public static string Get(string key)
    {
        try
        {
            var value = Loader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }
}
