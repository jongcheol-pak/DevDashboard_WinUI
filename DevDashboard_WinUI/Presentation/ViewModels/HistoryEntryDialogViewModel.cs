using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>작업 기록 등록/수정 다이얼로그 뷰모델</summary>
public partial class HistoryEntryDialogViewModel : ObservableObject
{
    private readonly HistoryEntry? _existing;

    /// <summary>수정 모드 여부</summary>
    public bool IsEditMode => _existing is not null;

    /// <summary>작업 완료 날짜 (null이면 오늘로 처리)</summary>
    [ObservableProperty]
    public partial DateTime? CompletedAt { get; set; } = DateTime.Today;

    /// <summary>작업 제목</summary>
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    /// <summary>상세 설명</summary>
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    public HistoryEntryDialogViewModel(HistoryEntry? existing = null)
    {
        _existing = existing;

        if (existing is not null)
        {
            CompletedAt = (DateTime?)existing.CompletedAt;
            Title = existing.Title;
            Description = existing.Description;
        }
    }

    /// <summary>입력값 유효성 검사</summary>
    public bool Validate()
    {
        return !string.IsNullOrWhiteSpace(Title);
    }

    /// <summary>입력된 내용으로 HistoryEntry를 생성하거나 기존 항목을 갱신합니다.</summary>
    public HistoryEntry ToModel()
    {
        var trimmedTitle = Title.Trim();
        var trimmedDesc = Description?.Trim() ?? string.Empty;
        var completedAt = CompletedAt ?? DateTime.Today;

        if (_existing is not null)
        {
            _existing.Title = trimmedTitle;
            _existing.Description = trimmedDesc;
            _existing.CompletedAt = completedAt;
            return _existing;
        }

        return new HistoryEntry
        {
            Title = trimmedTitle,
            Description = trimmedDesc,
            CompletedAt = completedAt
        };
    }
}
