namespace DevDashboard.Domain.Enums;

/// <summary>
/// 작업 마감(종료일) 알림의 긴급도 상태입니다.
/// 종료일과 오늘 날짜의 차이로 결정됩니다.
/// </summary>
public enum DeadlineStatus
{
    /// <summary>마감 임박 — 종료일까지 1~3일 남음</summary>
    Imminent,

    /// <summary>오늘 마감 — 종료일이 오늘</summary>
    DueToday,

    /// <summary>마감 경과 — 종료일이 지남</summary>
    Overdue
}
