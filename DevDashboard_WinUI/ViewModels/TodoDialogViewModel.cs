using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Models;

namespace DevDashboard.ViewModels;

public partial class TodoDialogViewModel : ObservableObject
{
    private readonly ProjectItem _projectItem;
    private readonly Action<TodoItem>? _onTodoCompleted;

    [ObservableProperty]
    private string _newTodoText = string.Empty;

    /// <summary>현재 탭 필터 ("All", "Active", "Completed")</summary>
    [ObservableProperty]
    private string _selectedTab = "All";

    public ObservableCollection<TodoItem> Todos { get; }

    /// <summary>현재 탭 필터가 적용된 항목 (수동 갱신)</summary>
    public ObservableCollection<TodoItem> FilteredTodos { get; } = [];

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

    private void RefreshFilter()
    {
        FilteredTodos.Clear();
        var filtered = SelectedTab switch
        {
            "Active" => Todos.Where(t => !t.IsCompleted),
            "Completed" => Todos.Where(t => t.IsCompleted),
            _ => (IEnumerable<TodoItem>)Todos
        };
        foreach (var item in filtered)
            FilteredTodos.Add(item);
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
}
