using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Domain.Entities;

/// <summary>
/// To-Do 항목 엔티티.
/// ProjectItem의 자식 엔티티이며, UI 바인딩을 위해 ObservableObject를 상속합니다.
/// </summary>
public partial class TodoItem : ObservableObject
{
    /// <summary>항목 고유 식별자 (UUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>할 일 본문 텍스트</summary>
    [ObservableProperty]
    private string _text = string.Empty;

    /// <summary>완료 여부</summary>
    [ObservableProperty]
    private bool _isCompleted;

    /// <summary>완료 처리된 일시 (미완료이면 null)</summary>
    [ObservableProperty]
    private DateTime? _completedAt;

    /// <summary>상세 설명 (최대 300자)</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>항목 등록 일시</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
