using DevDashboard.Models;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Views.Dialogs;

public sealed partial class TodoDialog : ContentDialog
{
    private TodoDialogViewModel Vm { get; }

    /// <summary>이번 세션에서 새로 생성된 작업 기록 항목 (WinUI 3 제약으로 항상 빈 리스트)</summary>
    public List<HistoryEntry> NewHistories { get; } = [];

    public TodoDialog(TodoDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
        RefreshList();
    }

    private void RefreshList()
    {
        TodoList.ItemsSource = null;
        TodoList.ItemsSource = Vm.FilteredTodos;
        EmptyText.Visibility = Vm.FilteredTodos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tab })
        {
            Vm.ChangeTabCommand.Execute(tab);
            RefreshList();
        }
    }

    private void AddTodo_Click(object sender, RoutedEventArgs e)
    {
        Vm.NewTodoText = NewTodoBox.Text;
        Vm.AddTodoCommand.Execute(null);
        NewTodoBox.Text = string.Empty;
        RefreshList();
    }

    private void TodoCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: TodoItem todo })
        {
            Vm.ToggleTodoCommand.Execute(todo);
            RefreshList();
        }
    }

    private void DeleteTodo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TodoItem todo })
        {
            Vm.DeleteTodoCommand.Execute(todo);
            RefreshList();
        }
    }
}
