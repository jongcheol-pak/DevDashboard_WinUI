using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Domain.Entities;

/// <summary>
/// 테스트 항목 엔티티.
/// ProjectItem의 자식 엔티티이며, UI 바인딩을 위해 ObservableObject를 상속합니다.
/// </summary>
public partial class TestItem : ObservableObject
{
    /// <summary>상태 상수: 통과</summary>
    public const string StatusPass = "Pass";

    /// <summary>상태 상수: 실패</summary>
    public const string StatusFail = "Fail";

    /// <summary>상태 상수: 미실행</summary>
    public const string StatusUntested = "Untested";

    /// <summary>항목 고유 식별자 (UUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>소속 카테고리 식별자</summary>
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>테스트 항목 제목</summary>
    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>테스트 방법 (재현 절차·수행 방식 등)</summary>
    [ObservableProperty]
    public partial string Method { get; set; } = string.Empty;

    /// <summary>테스트 진행 내용 및 비고 (실패 사유, 로그 등)</summary>
    [ObservableProperty]
    public partial string ProgressNote { get; set; } = string.Empty;

    /// <summary>테스트 항목 상태 ("Pass", "Fail", "Untested")</summary>
    [ObservableProperty]
    public partial string Status { get; set; } = StatusUntested;

    /// <summary>연결된 작업의 제목 (표시 전용 — 영속화하지 않으며 TestPageViewModel이 설정, 빈 문자열이면 연결 없음).
    /// 링크는 TodoItem.LinkedTestId 단방향이라 이 값은 매 재구성마다 역참조로 다시 채운다.</summary>
    [ObservableProperty]
    public partial string LinkedTaskTitle { get; set; } = string.Empty;

    /// <summary>완료 여부 (Status == "Pass")</summary>
    public bool IsCompleted => Status == StatusPass;

    /// <summary>완료 처리된 일시 (미완료이면 null)</summary>
    [ObservableProperty]
    public partial DateTime? CompletedAt { get; set; }

    /// <summary>항목 등록 일시</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
