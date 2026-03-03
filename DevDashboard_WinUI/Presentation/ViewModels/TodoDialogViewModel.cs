using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Presentation.Models;

namespace DevDashboard.Presentation.ViewModels;

public partial class TodoDialogViewModel : ObservableObject
{
    private readonly ProjectItem _projectItem;
    private readonly Action<TodoItem>? _onTodoCompleted;

    /// <summary>프로젝트 항목 참조 (HistoryDialog 생성 시 사용)</summary>
    public ProjectItem ProjectItem => _projectItem;

    [ObservableProperty]
    private string _newTodoText = string.Empty;

    /// <summary>현재 탭 필터 ("All", "Active", "Completed")</summary>
    [ObservableProperty]
    private string _selectedTab = "Active";

    /// <summary>완료됨 탭에서 그룹화 기준 ("CreatedAt", "CompletedAt")</summary>
    [ObservableProperty]
    private string _completedGroupBy = "CompletedAt";

    public ObservableCollection<TodoItem> Todos { get; }

    /// <summary>현재 탭 필터가 적용된 항목 (수동 갱신)</summary>
    public ObservableCollection<TodoItem> FilteredTodos { get; } = [];

    /// <summary>날짜별로 그룹화된 필터 결과</summary>
    public ObservableCollection<TodoDateGroup> FilteredGroups { get; } = [];

    public TodoDialogViewModel(ProjectItem projectItem, Action<TodoItem>? onTodoCompleted = null)
    {
        _projectItem = projectItem;
        _onTodoCompleted = onTodoCompleted;
        Todos = new ObservableCollection<TodoItem>(_projectItem.Todos ?? []);
        RefreshFilter();
    }

    partial void OnSelectedTabChanged(string value)
    {
        RefreshFilter();
    }

    partial void OnCompletedGroupByChanged(string value)
    {
        if (SelectedTab == "Completed")
            RefreshFilter();
    }

    private void RefreshFilter()
    {
        FilteredTodos.Clear();
        FilteredGroups.Clear();

        var filtered = SelectedTab switch
        {
            "Active" => Todos.Where(t => !t.IsCompleted),
            "Completed" => Todos.Where(t => t.IsCompleted),
            _ => (IEnumerable<TodoItem>)Todos
        };

        foreach (var item in filtered)
            FilteredTodos.Add(item);

        // 날짜별 그룹화
        var groups = SelectedTab == "Completed"
            ? GroupByDate(filtered, CompletedGroupBy)
            : GroupByDate(filtered, "CreatedAt");

        foreach (var group in groups)
            FilteredGroups.Add(group);
    }

    /// <summary>지정된 날짜 필드 기준으로 항목을 그룹화합니다.</summary>
    private static IEnumerable<TodoDateGroup> GroupByDate(IEnumerable<TodoItem> items, string dateField)
    {
        return items
            .GroupBy(t => GetDateKey(t, dateField))
            .OrderByDescending(g => g.Key)
            .Select(g => new TodoDateGroup(g.Key.ToString("yyyy-MM-dd"), g));
    }

    private static DateTime GetDateKey(TodoItem item, string dateField)
    {
        var dt = dateField == "CompletedAt"
            ? item.CompletedAt ?? item.CreatedAt
            : item.CreatedAt;
        return dt.Date;
    }

    [RelayCommand]
    private void ChangeTab(string tab)
    {
        SelectedTab = tab;
    }

    [RelayCommand]
    private void AddTodo()
    {
        var text = NewTodoText?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        if (Todos.Any(t => t.Text.Equals(text, StringComparison.OrdinalIgnoreCase)))
            return; // 중복 방지

        var newTodo = new TodoItem { Text = text };
        Todos.Add(newTodo);
        NewTodoText = string.Empty;
        RefreshFilter();
    }

    [RelayCommand]
    private void DeleteTodo(TodoItem? todo)
    {
        if (todo is null) return;
        // ContentDialog 내부에서는 중첩 ContentDialog 사용 불가 — 바로 삭제
        Todos.Remove(todo);
        RefreshFilter();
    }

    [RelayCommand]
    private void ToggleTodo(TodoItem? todo)
    {
        if (todo is null) return;

        todo.IsCompleted = !todo.IsCompleted;
        todo.CompletedAt = todo.IsCompleted ? DateTime.Now : null;

        if (todo.IsCompleted)
            _onTodoCompleted?.Invoke(todo);

        if (SelectedTab != "All")
            RefreshFilter();
    }

    public void SaveToModel()
    {
        _projectItem.Todos = Todos.ToList();
    }

    /// <summary>항목 텍스트를 수정합니다.</summary>
    public void EditTodo(TodoItem todo, string newText)
    {
        ArgumentNullException.ThrowIfNull(todo);
        var trimmed = newText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed)) return;
        todo.Text = trimmed;
        RefreshFilter();
    }
}
