namespace DevDashboard.Domain.Entities;

/// <summary>
/// 작업 기록 항목 엔티티.
/// ProjectItem의 자식 엔티티로, 완료한 작업 이력을 기록합니다.
/// </summary>
public class HistoryEntry
{
    /// <summary>항목 고유 식별자 (UUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>작업 제목</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>작업 상세 설명</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>작업 완료 날짜</summary>
    public DateTime CompletedAt { get; set; } = DateTime.Today;

    /// <summary>기록 등록 일시</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
