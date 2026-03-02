using System.Collections.ObjectModel;

namespace DevDashboard.Presentation.Models;

/// <summary>
/// 날짜별 To-Do 항목 그룹. UI에서 날짜 헤더 + 하위 항목 목록을 표시하는 데 사용합니다.
/// </summary>
public sealed class TodoDateGroup
{
    /// <summary>그룹 헤더에 표시할 날짜 텍스트 (예: "2026-01-12")</summary>
    public string DateLabel { get; }

    /// <summary>해당 날짜에 속하는 To-Do 항목 목록</summary>
    public ObservableCollection<TodoItem> Items { get; }

    public TodoDateGroup(string dateLabel, IEnumerable<TodoItem> items)
    {
        DateLabel = dateLabel;
        Items = new ObservableCollection<TodoItem>(items);
    }
}
