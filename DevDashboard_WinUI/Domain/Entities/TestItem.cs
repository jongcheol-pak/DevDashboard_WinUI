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

    // --- 구 상태 상수 (Phase 3 전환 병존 — 참조부 이전 후 T6에서 구 VM/Dialog와 함께 삭제) ---
    /// <summary>구 상태 상수: 테스트(신 모델 미실행에 대응). T6에서 제거 예정</summary>
    public const string StatusTesting = "Testing";

    /// <summary>구 상태 상수: 수정(신 모델 실패에 대응). T6에서 제거 예정</summary>
    public const string StatusFix = "Fix";

    /// <summary>구 상태 상수: 완료(신 모델 통과에 대응). T6에서 제거 예정</summary>
    public const string StatusDone = "Done";

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

    /// <summary>완료 여부 (Status == "Pass")</summary>
    public bool IsCompleted => Status == StatusPass;

    /// <summary>완료 처리된 일시 (미완료이면 null)</summary>
    [ObservableProperty]
    public partial DateTime? CompletedAt { get; set; }

    /// <summary>항목 등록 일시</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
