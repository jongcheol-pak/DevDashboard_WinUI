using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// GitHub 릴리스 최신 버전 확인 및 브라우저 URL 열기 기능을 제공하는 서비스.
/// 소켓 고갈 방지를 위해 HttpClient 인스턴스를 정적으로 공유합니다.
/// </summary>
public static class VersionCheckService
{
    private const string GitHubOwner = "jongcheol-pak";
    private const string GitHubRepo = "DevDashboard";

    /// <summary>소켓 고갈 방지를 위해 정적으로 공유하는 HttpClient</summary>
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static VersionCheckService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DevDashboard");
    }

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
    /// GitHub Releases API를 통해 최신 버전을 확인합니다.
    /// </summary>
    /// <returns>업데이트가 있으면 <see cref="VersionCheckResult"/>, 없으면 null</returns>
    public static async Task<VersionCheckResult?> CheckLatestVersionAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            var htmlUrl = root.GetProperty("html_url").GetString() ?? string.Empty;

            // "v1.0.0" 또는 "1.0.0" 형식 모두 지원
            var versionStr = tagName.StartsWith('v') ? tagName[1..] : tagName;
            var currentStr = ReadCurrentVersion();

            if (Version.TryParse(versionStr, out var latestVer)
                && Version.TryParse(currentStr, out var currentVer)
                && latestVer > currentVer)
            {
                return new VersionCheckResult(versionStr, htmlUrl);
            }
        }
        catch (HttpRequestException)
        {
            // 네트워크 오류 — 무시
        }
        catch (JsonException)
        {
            // JSON 파싱 오류 — 무시
        }
        catch (TaskCanceledException)
        {
            // 타임아웃 — 무시
        }

        return null;
    }

    /// <summary>지정된 URL을 기본 브라우저로 엽니다.</summary>
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
/// <param name="VersionText">최신 버전 문자열 (예: "1.1.0")</param>
/// <param name="ReleaseUrl">GitHub 릴리스 페이지 URL</param>
public record VersionCheckResult(string VersionText, string ReleaseUrl);
