namespace DevDashboard.Domain.Entities;

/// <summary>
/// To-Do 항목의 진행 상태를 나타냅니다.
/// </summary>
public enum TodoStatus
{
    /// <summary>대기</summary>
    Waiting,

    /// <summary>진행 중</summary>
    Active,

    /// <summary>완료</summary>
    Completed
}
