namespace DevDashboard.Models;

/// <summary>작업 기록 항목 모델</summary>
public class HistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>작업 제목</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>상세 설명</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>작업 완료 날짜</summary>
    public DateTime CompletedAt { get; set; } = DateTime.Today;

    /// <summary>등록 시각</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
