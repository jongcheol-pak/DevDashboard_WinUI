using System.Diagnostics;
using Windows.Services.Store;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// MS Store를 통한 최신 버전 확인 및 스토어 페이지 열기 기능을 제공하는 서비스.
/// 세션 동안 스토어 조회 결과를 캐시하여 중복 호출을 방지합니다.
/// </summary>
public static class VersionCheckService
{
    /// <summary>세션 단위 캐시 — 첫 호출의 Task를 재사용하여 스토어 중복 조회 방지</summary>
    private static Task<VersionCheckResult?>? _cachedTask;

    /// <summary>MSIX 패키지 ID에서 현재 앱 버전을 읽어옵니다.</summary>
    public static string ReadCurrentVersion()
    {
        try
        {
            var v = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
        catch (Exception)
        {
            return "0";
        }
    }

    /// <summary>
    /// MS Store를 통해 최신 버전 업데이트 여부를 확인합니다.
    /// 세션 내 첫 호출만 스토어에 조회하고, 이후 호출은 캐시된 결과를 즉시 반환합니다.
    /// </summary>
    /// <returns>업데이트가 있으면 <see cref="VersionCheckResult"/>, 없으면 null</returns>
    public static Task<VersionCheckResult?> CheckLatestVersionAsync()
    {
        return _cachedTask ??= CheckLatestVersionCoreAsync();
    }

    private static async Task<VersionCheckResult?> CheckLatestVersionCoreAsync()
    {
        try
        {
            // Store 서명 패키지만 Store API 호출 허용 (그 외는 COMException/Stowed Exception 방지)
            if (Debugger.IsAttached
                || Windows.ApplicationModel.Package.Current.SignatureKind
                    != Windows.ApplicationModel.PackageSignatureKind.Store)
                return null;

            var storeContext = StoreContext.GetDefault();
            var updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();

            if (updates.Count == 0) return null;

            return new VersionCheckResult("ms-windows-store://updates");
        }
        catch (Exception)
        {
            // 스토어 연결 오류 — 무시
            return null;
        }
    }

    /// <summary>지정된 URL을 기본 브라우저 또는 셸 핸들러로 엽니다.</summary>
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // URL 열기 실패 — 무시
        }
    }
}

/// <summary>버전 확인 결과 값 객체</summary>
/// <param name="ReleaseUrl">MS Store 업데이트 페이지 URI</param>
public record VersionCheckResult(string ReleaseUrl);
