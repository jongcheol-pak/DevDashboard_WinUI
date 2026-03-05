using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>To-Do 상세보기 다이얼로그 뷰모델</summary>
public partial class TodoDetailDialogViewModel : ObservableObject
{
    private readonly TodoItem _todoItem;
    private string _originalDescription;

    public string Title => _todoItem.Text;

    public string CreatedAtText => _todoItem.CreatedAt.ToString("yyyy-MM-dd HH:mm");

    public string CompletedAtText => _todoItem.CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";

    /// <summary>현재 편집 모드 여부</summary>
    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    public TodoDetailDialogViewModel(TodoItem todoItem)
    {
        _todoItem = todoItem;
        Description = todoItem.Description;
        _originalDescription = todoItem.Description;

        // 저장된 값이 없으면 수정 모드로 시작
        IsEditing = string.IsNullOrWhiteSpace(todoItem.Description);
    }

    /// <summary>수정 모드 진입</summary>
    public void EnterEditMode()
    {
        _originalDescription = Description;
        IsEditing = true;
    }

    /// <summary>수정 취소 — 원래 값 복원</summary>
    public void CancelEdit()
    {
        Description = _originalDescription;
        IsEditing = false;
    }

    /// <summary>편집된 내용을 모델에 반영. 공백만 있으면 저장하지 않고 false 반환.</summary>
    public bool SaveToModel()
    {
        var trimmed = Description?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        Description = trimmed;
        _todoItem.Description = trimmed;
        _originalDescription = trimmed;
        IsEditing = false;
        return true;
    }
}
