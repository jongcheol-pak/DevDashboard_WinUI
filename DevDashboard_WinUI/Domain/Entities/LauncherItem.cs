namespace DevDashboard.Domain.Entities;

/// <summary>
/// 좌측 사이드바 런처에 등록된 앱 항목.
/// SQLite에 저장되며, 아이콘 캐시 경로와 실행 경로를 포함합니다.
/// </summary>
public class LauncherItem
{
    /// <summary>고유 식별자</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>앱 표시 이름</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>실행 파일 경로 또는 shell:AppsFolder AUMID</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>캐시된 아이콘 파일 경로 (PNG)</summary>
    public string IconCachePath { get; set; } = string.Empty;

    /// <summary>정렬 순서 (0부터 시작)</summary>
    public int SortOrder { get; set; }
}
