using CommunityToolkit.Mvvm.ComponentModel;
using DevDashboard.Models;

namespace DevDashboard.ViewModels;

/// <summary>작업 기록 등록/수정 다이얼로그 뷰모델</summary>
public partial class HistoryEntryDialogViewModel : ObservableObject
{
    private readonly HistoryEntry? _existing;

    /// <summary>수정 모드 여부</summary>
    public bool IsEditMode => _existing is not null;

    /// <summary>작업 완료 날짜 (null이면 오늘로 처리)</summary>
    [ObservableProperty]
    private DateTime? _completedAt = DateTime.Today;

    /// <summary>작업 제목</summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>상세 설명</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    public HistoryEntryDialogViewModel(HistoryEntry? existing = null)
    {
        _existing = existing;

        if (existing is not null)
        {
            _completedAt = (DateTime?)existing.CompletedAt;
            _title = existing.Title;
            _description = existing.Description;
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
