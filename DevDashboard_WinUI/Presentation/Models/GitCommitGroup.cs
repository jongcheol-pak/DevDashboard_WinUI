using DevDashboard.Domain.ValueObjects;

namespace DevDashboard.Presentation.Models;

/// <summary>
/// 날짜별 커밋 그룹. UI에서 날짜 헤더 + 커밋 목록을 표시하는 데 사용합니다.
/// </summary>
public sealed class GitCommitGroup
{
    /// <summary>그룹 헤더에 표시할 날짜 텍스트 (예: "2026-01-12")</summary>
    public string DateLabel { get; }

    /// <summary>해당 날짜에 속하는 커밋 목록</summary>
    public IReadOnlyList<GitCommit> Items { get; }

    public GitCommitGroup(string dateLabel, IEnumerable<GitCommit> items)
    {
        DateLabel = dateLabel;
        Items = [.. items];
    }
}
