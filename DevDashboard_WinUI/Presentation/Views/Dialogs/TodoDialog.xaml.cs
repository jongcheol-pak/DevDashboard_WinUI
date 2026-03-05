using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using WinUIEx;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class TodoDialog : WindowEx
{
    private const int MinW = 500;
    private const int InitW = 700;
    private const int InitH = 550;

    private TodoDialogViewModel Vm { get; }
    private readonly TaskCompletionSource _closedTcs = new();

    /// <summary>초기화 완료 플래그 — 목록 바인딩 시 이벤트 핸들러 무시</summary>
    private bool _isRefreshing;

    /// <summary>이번 세션에서 새로 생성된 작업 기록 항목</summary>
    public List<HistoryEntry> NewHistories { get; } = [];

    public TodoDialog(TodoDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
        Title = LocalizationService.Get("TodoDialogTitle");
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = Title;

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        RefreshList();
        Closed += (_, _) => _closedTcs.TrySetResult();
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
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
        AddTodoPanel.Visibility = Vm.SelectedTab == "Active" ? Visibility.Visible : Visibility.Collapsed;
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

    private async void TodoCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isRefreshing) return;
            if (sender is not CheckBox { DataContext: TodoItem todo } checkBox) return;

            var isChecked = checkBox.IsChecked == true;

            // 바인딩 초기화에 의한 중복 이벤트 무시 (체크박스 상태와 모델 상태가 이미 일치)
            if (isChecked == todo.IsCompleted) return;

            if (isChecked)
            {
                // 완료 처리 — _isRefreshing 가드로 내부 RefreshFilter 이벤트 재진입 방지
                _isRefreshing = true;
                Vm.ToggleTodoCommand.Execute(todo);
                _isRefreshing = false;

                // 설정에 따라 작업 기록 팝업 표시
                var settings = new JsonStorageService().Load();
                if (settings.ShowWorkLogPopupOnTodoComplete)
                {
                    var historyVm = new HistoryDialogViewModel(Vm.ProjectItem);
                    var historyDialog = new HistoryDialog(historyVm);
                    historyDialog.OpenAddPanel(todo.Text);
                    await historyDialog.ShowAsync();

                    // SaveToModel() 미호출 — OnTodoDialogClosed의 AddRange에서 신규 항목만 추가됨
                    NewHistories.AddRange(historyVm.NewEntries);
                }
            }
            else
            {
                // 완료 해제 확인
                var dialog = new ContentDialog
                {
                    Title = LocalizationService.Get("TodoUncompleteConfirmTitle"),
                    Content = LocalizationService.Get("TodoUncompleteConfirmMessage"),
                    PrimaryButtonText = LocalizationService.Get("Dialog_Yes"),
                    CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = Content.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    _isRefreshing = true;
                    Vm.ToggleTodoCommand.Execute(todo);
                    _isRefreshing = false;
                }
            }

            RefreshList();
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
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
                XamlRoot = Content.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            Vm.DeleteTodoCommand.Execute(todo);
            RefreshList();
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
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
                XamlRoot = Content.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var newText = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newText)) return;

            Vm.EditTodo(todo, newText);
            RefreshList();
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
