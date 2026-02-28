using Windows.ApplicationModel.Resources;

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
