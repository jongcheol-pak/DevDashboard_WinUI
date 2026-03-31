using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Text;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class TodoDialog : ContentDialog
{
    private TodoDialogViewModel Vm { get; }

    /// <summary>초기화 완료 플래그 — 목록 바인딩 시 이벤트 핸들러 무시</summary>
    private bool _isRefreshing;

    /// <summary>중첩 다이얼로그 완료 대기용 TCS</summary>
    private TaskCompletionSource<bool>? _nestedTcs;

    /// <summary>Todo 완료 시 작업 기록 팝업 표시 여부 (생성자에서 캐시)</summary>
    private readonly bool _showWorkLogPopup;

    /// <summary>이번 세션에서 새로 생성된 작업 기록 항목</summary>
    public List<HistoryEntry> NewHistories { get; } = [];

    public TodoDialog(TodoDialogViewModel vm)
    {
        Vm = vm;
        _showWorkLogPopup = new JsonStorageService().Load().ShowWorkLogPopupOnTodoComplete;
        InitializeComponent();

        Title = LocalizationService.Get("TodoDialogTitle");
        CloseButtonText = LocalizationService.Get("Dialog_Close");

        RefreshList();
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        ContentDialogResult result;
        do
        {
            result = await base.ShowAsync();
            if (_nestedTcs is not null)
            {
                await _nestedTcs.Task;
                _nestedTcs = null;
                continue;
            }
            break;
        } while (true);
        return result;
    }

    /// <summary>현재 다이얼로그를 숨기고 중첩 다이얼로그를 표시한 후 재표시합니다.</summary>
    private async Task<ContentDialogResult> ShowNestedDialogAsync(ContentDialog dialog)
    {
        _nestedTcs = new TaskCompletionSource<bool>();
        Hide();
        dialog.XamlRoot = App.MainWindow?.Content?.XamlRoot;
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            _nestedTcs.TrySetResult(true);
        }
    }

    private Task ShowNestedErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("Dialog_DefaultErrorTitle"),
            Content = string.Format(LocalizationService.Get("UnexpectedError"), message),
            CloseButtonText = LocalizationService.Get("Dialog_OK"),
        };
        return ShowNestedDialogAsync(dialog);
    }

    // --- x:Bind 함수 바인딩용 정적 헬퍼 ---

    /// <summary>완료 여부에 따른 텍스트 장식 (취소선)</summary>
    public static TextDecorations GetTextDecorations(bool isCompleted)
        => isCompleted ? TextDecorations.Strikethrough : TextDecorations.None;

    /// <summary>완료 여부에 따른 투명도</summary>
    public static double GetOpacity(bool isCompleted)
        => isCompleted ? 0.5 : 1.0;

    /// <summary>bool 반전 (함수 바인딩용)</summary>
    public static bool InvertBool(bool value) => !value;

    /// <summary>시작 날짜 포맷 (함수 바인딩용)</summary>
    public static string FormatCreatedAt(DateTime createdAt)
        => $"{LocalizationService.Get("TodoLabel_Start")} {createdAt:yyyy-MM-dd HH:mm}";

    /// <summary>완료 날짜 포맷 (함수 바인딩용)</summary>
    public static string FormatCompletedAt(DateTime? completedAt)
        => completedAt is null
            ? string.Empty
            : $"{LocalizationService.Get("TodoLabel_Completed")} {completedAt:yyyy-MM-dd HH:mm}";

    /// <summary>완료 날짜 표시 여부 (함수 바인딩용)</summary>
    public static Visibility GetCompletedVisibility(bool isCompleted)
        => isCompleted ? Visibility.Visible : Visibility.Collapsed;

    private void RefreshList()
    {
        _isRefreshing = true;
        TodoList.ItemsSource = null;
        TodoList.ItemsSource = Vm.FilteredGroups;
        EmptyText.Visibility = Vm.FilteredGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        CompletedGroupByPanel.Visibility = Vm.SelectedTab == "Completed" ? Visibility.Visible : Visibility.Collapsed;
        AddTodoPanel.Visibility = Vm.SelectedTab == "Waiting" ? Visibility.Visible : Visibility.Collapsed;

        // 탭 라벨에 개수 표시
        TabWaiting.Content = Vm.WaitingTabLabel;
        TabActive.Content = Vm.ActiveTabLabel;
        TabDone.Content = Vm.CompletedTabLabel;
        TabAll.Content = Vm.AllTabLabel;

        _isRefreshing = false;
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tab })
        {
            Vm.ChangeTabCommand.Execute(tab);
            RefreshList();
        }
    }

    private void CompletedGroupBy_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string groupBy })
        {
            Vm.CompletedGroupBy = groupBy;
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

    /// <summary>콤보박스 로드 시 ItemsSource 및 선택 항목 초기화</summary>
    private void StatusComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox { Tag: TodoItem todo } comboBox) return;
        _isRefreshing = true;
        comboBox.ItemsSource = TodoDialogViewModel.StatusOptions;
        comboBox.SelectedItem = TodoDialogViewModel.StatusOptions.FirstOrDefault(s => s.Value == todo.Status);
        _isRefreshing = false;
    }

    private async void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_isRefreshing) return;
            if (sender is not ComboBox { Tag: TodoItem todo, SelectedItem: TodoStatusItem selected }) return;

            // 현재 상태와 동일하면 무시
            if (selected.Value == todo.Status) return;

            var newStatus = selected.Value;

            if (newStatus == TodoStatus.Completed)
            {
                // 완료 처리
                _isRefreshing = true;
                try { Vm.ChangeStatus(todo, newStatus); }
                finally { _isRefreshing = false; }

                // 설정에 따라 작업 기록 팝업 표시
                if (_showWorkLogPopup)
                {
                    var historyVm = new HistoryDialogViewModel(Vm.ProjectItem);
                    var historyDialog = new HistoryDialog(historyVm);
                    historyDialog.OpenAddPanel(todo.Text);
                    await ShowNestedDialogAsync(historyDialog);

                    NewHistories.AddRange(historyVm.NewEntries);
                }
            }
            else if (todo.Status == TodoStatus.Completed)
            {
                // 완료 해제 확인
                var dialog = new ContentDialog
                {
                    Title = LocalizationService.Get("TodoStatusChangeConfirmTitle"),
                    Content = LocalizationService.Get("TodoStatusChangeConfirmMessage"),
                    PrimaryButtonText = LocalizationService.Get("Dialog_Yes"),
                    CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                    DefaultButton = ContentDialogButton.Primary,
                };

                if (await ShowNestedDialogAsync(dialog) == ContentDialogResult.Primary)
                {
                    _isRefreshing = true;
                    try { Vm.ChangeStatus(todo, newStatus); }
                    finally { _isRefreshing = false; }
                }
            }
            else
            {
                // 대기 ↔ 진행 중 전환은 확인 없이 바로 변경
                _isRefreshing = true;
                try { Vm.ChangeStatus(todo, newStatus); }
                finally { _isRefreshing = false; }
            }

            RefreshList();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    private async void DeleteTodo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: TodoItem todo }) return;

            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("DeleteConfirmTitle"),
                Content = LocalizationService.Get("DeleteConfirmMessage"),
                PrimaryButtonText = LocalizationService.Get("Dialog_Delete"),
                CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                DefaultButton = ContentDialogButton.Close,
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            Vm.DeleteTodoCommand.Execute(todo);
            RefreshList();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    private async void EditTodo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: TodoItem todo }) return;

            var textBox = new TextBox { Text = todo.Text, MaxLength = 200 };

            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("TodoEditTitle"),
                Content = textBox,
                PrimaryButtonText = LocalizationService.Get("Dialog_Save"),
                CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            var newText = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newText)) return;

            Vm.EditTodo(todo, newText);
            RefreshList();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }
}
