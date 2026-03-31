using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.Models;

namespace DevDashboard.Presentation.ViewModels;

public partial class TodoDialogViewModel : ObservableObject
{
    private readonly ProjectItem _projectItem;
    private readonly Action<TodoItem>? _onTodoCompleted;

    /// <summary>프로젝트 항목 참조 (HistoryDialog 생성 시 사용)</summary>
    public ProjectItem ProjectItem => _projectItem;

    [ObservableProperty]
    public partial string NewTodoText { get; set; } = string.Empty;

    /// <summary>현재 탭 필터 ("All", "Waiting", "Active", "Completed")</summary>
    [ObservableProperty]
    public partial string SelectedTab { get; set; } = "Waiting";

    /// <summary>완료 탭에서 그룹화 기준 ("CreatedAt", "CompletedAt")</summary>
    [ObservableProperty]
    public partial string CompletedGroupBy { get; set; } = "CompletedAt";

    /// <summary>탭별 개수 표시 텍스트</summary>
    [ObservableProperty]
    public partial string WaitingTabLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActiveTabLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CompletedTabLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AllTabLabel { get; set; } = string.Empty;

    public ObservableCollection<TodoItem> Todos { get; }

    /// <summary>현재 탭 필터가 적용된 항목 (수동 갱신)</summary>
    public ObservableCollection<TodoItem> FilteredTodos { get; } = [];

    /// <summary>날짜별로 그룹화된 필터 결과</summary>
    public ObservableCollection<TodoDateGroup> FilteredGroups { get; } = [];

    /// <summary>상태 콤보박스 항목 목록</summary>
    public static List<TodoStatusItem> StatusOptions { get; } =
    [
        new(TodoStatus.Waiting, LocalizationService.Get("TodoStatus_Waiting")),
        new(TodoStatus.Active, LocalizationService.Get("TodoStatus_Active")),
        new(TodoStatus.Completed, LocalizationService.Get("TodoStatus_Completed")),
    ];

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
            "Waiting" => Todos.Where(t => t.Status == TodoStatus.Waiting),
            "Active" => Todos.Where(t => t.Status == TodoStatus.Active),
            "Completed" => Todos.Where(t => t.Status == TodoStatus.Completed),
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

        UpdateTabCounts();
    }

    /// <summary>탭 라벨의 개수 텍스트를 갱신합니다.</summary>
    private void UpdateTabCounts()
    {
        var waitingCount = Todos.Count(t => t.Status == TodoStatus.Waiting);
        var activeCount = Todos.Count(t => t.Status == TodoStatus.Active);
        var completedCount = Todos.Count(t => t.Status == TodoStatus.Completed);
        var allCount = Todos.Count;

        WaitingTabLabel = FormatTabLabel(LocalizationService.Get("TodoTab_Waiting"), waitingCount);
        ActiveTabLabel = FormatTabLabel(LocalizationService.Get("TodoTab_Active"), activeCount);
        CompletedTabLabel = FormatTabLabel(LocalizationService.Get("TodoTab_Completed"), completedCount);
        AllTabLabel = FormatTabLabel(LocalizationService.Get("TodoTab_All"), allCount);
    }

    private static string FormatTabLabel(string label, int count)
        => count > 0 ? $"{label}({count})" : label;

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

        var newTodo = new TodoItem { Text = text, Status = TodoStatus.Waiting };
        Todos.Add(newTodo);
        NewTodoText = string.Empty;
        RefreshFilter();
    }

    [RelayCommand]
    private void DeleteTodo(TodoItem? todo)
    {
        if (todo is null) return;
        Todos.Remove(todo);
        RefreshFilter();
    }

    [RelayCommand]
    private void ToggleTodo(TodoItem? todo)
    {
        if (todo is null) return;

        todo.IsCompleted = !todo.IsCompleted;
        todo.CompletedAt = todo.IsCompleted ? DateTime.Now : null;
        todo.Status = todo.IsCompleted ? TodoStatus.Completed : TodoStatus.Waiting;

        if (todo.IsCompleted)
            _onTodoCompleted?.Invoke(todo);

        if (SelectedTab != "All")
            RefreshFilter();
    }

    /// <summary>항목 상태를 변경합니다.</summary>
    public void ChangeStatus(TodoItem todo, TodoStatus newStatus)
    {
        ArgumentNullException.ThrowIfNull(todo);
        if (todo.Status == newStatus) return;

        var wasCompleted = todo.Status == TodoStatus.Completed;
        todo.Status = newStatus;

        if (newStatus == TodoStatus.Completed && !wasCompleted)
            _onTodoCompleted?.Invoke(todo);

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

/// <summary>상태 콤보박스에 표시할 항목</summary>
public sealed record TodoStatusItem(TodoStatus Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
