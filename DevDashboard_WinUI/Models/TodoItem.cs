using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Models;

/// <summary>To-Do 항목 모델</summary>
public partial class TodoItem : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private DateTime? _completedAt;

    /// <summary>상세 설명 (최대 300자)</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
