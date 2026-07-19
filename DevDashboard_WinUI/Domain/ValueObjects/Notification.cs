namespace DevDashboard.Domain.ValueObjects;

/// <summary>
/// 마감 알림 1건의 불변 사실을 표현하는 값 객체입니다.
/// 특정 작업(TodoItem)의 종료일이 임박·오늘·경과 상태일 때 생성됩니다.
/// 읽음 여부는 이 객체에 담지 않습니다(영속 상태 기반 표현 관심사 — 표현 계층에서 부여).
/// </summary>
public record Notification
{
    /// <summary>알림 대상 작업이 속한 프로젝트 Id</summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>프로젝트 표시 이름(그룹 헤더용)</summary>
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>알림 대상 작업 Id</summary>
    public string TodoId { get; init; } = string.Empty;

    /// <summary>작업 본문 텍스트(표시용)</summary>
    public string TodoText { get; init; } = string.Empty;

    /// <summary>작업 종료(마감)일 — 알림 대상은 항상 종료일이 지정된 작업</summary>
    public DateTime EndDate { get; init; }

    /// <summary>마감 긴급도 상태(임박/오늘/경과)</summary>
    public DeadlineStatus Status { get; init; }
}
