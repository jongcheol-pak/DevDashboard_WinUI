using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Domain.Entities;

/// <summary>
/// 테스트 항목 엔티티.
/// ProjectItem의 자식 엔티티이며, UI 바인딩을 위해 ObservableObject를 상속합니다.
/// </summary>
public partial class TestItem : ObservableObject
{
    /// <summary>상태 상수: 테스트</summary>
    public const string StatusTesting = "Testing";

    /// <summary>상태 상수: 수정</summary>
    public const string StatusFix = "Fix";

    /// <summary>상태 상수: 완료</summary>
    public const string StatusDone = "Done";

    /// <summary>항목 고유 식별자 (UUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>소속 카테고리 식별자</summary>
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>테스트 항목 제목</summary>
    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>테스트 진행 내용 및 비고 (실패 사유, 로그 등)</summary>
    [ObservableProperty]
    public partial string ProgressNote { get; set; } = string.Empty;

    /// <summary>테스트 항목 상태 ("Testing", "Fix", "Done")</summary>
    [ObservableProperty]
    public partial string Status { get; set; } = StatusTesting;

    /// <summary>완료 여부 (Status == "Done")</summary>
    public bool IsCompleted => Status == StatusDone;

    /// <summary>완료 처리된 일시 (미완료이면 null)</summary>
    [ObservableProperty]
    public partial DateTime? CompletedAt { get; set; }

    /// <summary>항목 등록 일시</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
