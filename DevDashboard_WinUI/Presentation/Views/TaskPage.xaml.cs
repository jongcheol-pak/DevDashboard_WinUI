using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using DevDashboard.Presentation.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace DevDashboard.Presentation.Views;

/// <summary>작업(칸반) 전체 페이지 — 프로젝트 카드의 "작업" 진입점에서 표시됩니다.</summary>
public sealed partial class TaskPage : UserControl
{
    private readonly AppSettings _settings;

    /// <summary>페이지 뷰모델 (x:Bind 대상)</summary>
    public TaskPageViewModel Vm { get; }

    // 카드 액션 버튼 툴팁 (x:Bind 정적 참조)
    public static string EditTooltip { get; } = LocalizationService.Get("TaskEdit_Tooltip");
    public static string DeleteTooltip { get; } = LocalizationService.Get("TaskDelete_Tooltip");

    // 칸반 열 헤더 라벨 (x:Bind 정적 참조 — StatusOptions와 동일 리소스 키 재사용)
    public static string LabelWaiting { get; } = LocalizationService.Get("TaskStatus_Waiting");
    public static string LabelActive { get; } = LocalizationService.Get("TaskStatus_Active");
    public static string LabelCompleted { get; } = LocalizationService.Get("TaskStatus_Completed");
    public static string LabelHold { get; } = LocalizationService.Get("TaskStatus_Hold");

    /// <summary>상태 콤보박스 항목 (예정/진행 중/완료/보류)</summary>
    public static IReadOnlyList<TaskStatusOption> StatusOptions { get; } =
    [
        new(TodoStatus.Waiting, LocalizationService.Get("TaskStatus_Waiting")),
        new(TodoStatus.Active, LocalizationService.Get("TaskStatus_Active")),
        new(TodoStatus.Completed, LocalizationService.Get("TaskStatus_Completed")),
        new(TodoStatus.Hold, LocalizationService.Get("TaskStatus_Hold")),
    ];

    public TaskPage(TaskPageViewModel vm, AppSettings settings)
    {
        Vm = vm;
        _settings = settings;
        InitializeComponent();
        DataContext = vm;

        // 카테고리 필터 콤보 구성 (전체 + 카테고리)
        var options = new List<string> { LocalizationService.Get("TaskFilter_All") };
        options.AddRange(vm.AvailableCategories);
        CategoryFilterCombo.ItemsSource = options;
        CategoryFilterCombo.SelectedIndex = 0;

        Vm.WorkLogRequested += OnWorkLogRequested;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        Vm.WorkLogRequested -= OnWorkLogRequested;
    }

    // ===== x:Bind 정적 헬퍼 =====

    /// <summary>우선순위 표시 텍스트 (높음/보통/낮음)</summary>
    public static string PriorityText(TaskPriority priority)
        => LocalizationService.Get($"TaskPriority_{priority}");

    /// <summary>시작일 표시 ("시작 : yyyy-MM-dd", 미지정이면 빈 문자열)</summary>
    public static string FormatStart(DateTime? date)
        => date.HasValue ? $"{LocalizationService.Get("TaskLabel_Start")} {date.Value:yyyy-MM-dd}" : string.Empty;

    /// <summary>종료일 표시 ("종료 : yyyy-MM-dd", 미지정이면 빈 문자열)</summary>
    public static string FormatEnd(DateTime? date)
        => date.HasValue ? $"{LocalizationService.Get("TaskLabel_End")} {date.Value:yyyy-MM-dd}" : string.Empty;

    /// <summary>날짜 표시 여부 (지정된 날짜가 있을 때만 표시). x:Bind 함수 바인딩은 Converter를 적용하지 않아 직접 Visibility를 반환한다.</summary>
    public static Visibility DateVisibility(DateTime? date)
        => date.HasValue ? Visibility.Visible : Visibility.Collapsed;

    // ===== 네비게이션·뷰 =====

    private void Back_Click(object sender, RoutedEventArgs e)
        => (App.MainWindow as MainWindow)?.ShowDashboard();

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedIndex: var index } combo) return;
        Vm.SelectedCategoryFilter = index <= 0 ? null : combo.SelectedItem as string;
    }

    // ===== 상태 콤보 =====

    private void StatusCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox { Tag: TodoItem todo } combo) return;
        combo.ItemsSource = StatusOptions;
        combo.SelectedItem = StatusOptions.FirstOrDefault(o => o.Value == todo.Status);
    }

    private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { Tag: TodoItem todo, SelectedItem: TaskStatusOption option }) return;
        // MoveToStatus는 동일 상태면 무시하므로 Loaded 초기 선택으로 인한 불필요한 저장이 없다.
        Vm.MoveToStatus(todo, option.Value);
    }

    // ===== 작업 추가/편집/삭제 =====

    private async void AddTask_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TaskEditDialog(null, _settings);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.ResultTodo is { } todo)
        {
            Vm.AddTodo(todo);
            // FR-T6: "테스트 추가" 토글이 켜져 있으면 연결 테스트 생성
            if (dialog.AddTestRequested)
                Vm.CreateLinkedTest(todo);
        }
    }

    private async void EditTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TodoItem todo }) return;
        var dialog = new TaskEditDialog(todo, _settings);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.ResultTodo is not null)
            Vm.UpdateTodo(todo);
    }

    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TodoItem todo }) return;
        var confirmed = await DialogService.ShowConfirmAsync(
            string.Format(LocalizationService.Get("TaskDelete_Confirm"), todo.Text),
            LocalizationService.Get("TaskDelete_Title"));
        if (confirmed)
            Vm.DeleteTodo(todo);
    }

    // ===== 칸반 드래그앤드롭 (상태 열 간 이동, FR-T3) =====

    // DragStarting에서 설정 — Drop에서 동기적으로 사용
    private string? _draggedTodoId;

    private void Card_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TodoItem todo })
        {
            _draggedTodoId = todo.Id;
            e.Data.SetText(todo.Id);
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
        else
        {
            e.Cancel = true;
        }
    }

    private void Column_DragOver(object sender, DragEventArgs e)
    {
        if (!string.IsNullOrEmpty(_draggedTodoId))
            e.AcceptedOperation = DataPackageOperation.Move;
    }

    private void Column_Drop(object sender, DragEventArgs e)
    {
        var draggedId = _draggedTodoId;
        _draggedTodoId = null;
        if (string.IsNullOrEmpty(draggedId)) return;
        if (sender is not FrameworkElement { Tag: string statusTag }) return;
        if (!Enum.TryParse<TodoStatus>(statusTag, out var status)) return;

        var todo = Vm.Project.Todos?.FirstOrDefault(t => t.Id == draggedId);
        if (todo is not null)
            Vm.MoveToStatus(todo, status);
    }

    // ===== 완료 → 작업기록 팝업 (현행 훅 보존, plan M1) =====

    private async void OnWorkLogRequested(object? sender, TodoItem todo)
    {
        var historyVm = new HistoryDialogViewModel(Vm.Project, _settings);
        var dialog = new HistoryDialog(historyVm);
        dialog.OpenAddPanel(todo.Text);
        await dialog.ShowAsync();
        Vm.CommitWorkLog(historyVm.NewEntries);
    }
}

/// <summary>상태 콤보박스 항목</summary>
public sealed record TaskStatusOption(TodoStatus Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
