using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using DevDashboard.Presentation.Views.Dialogs;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

    // 칸반 카드 우클릭 메뉴 라벨 (x:Bind 정적 참조)
    public static string MenuEditText { get; } = LocalizationService.Get("TaskMenu_Edit");
    public static string MenuDeleteText { get; } = LocalizationService.Get("TaskMenu_Delete");
    public static string MenuMoveToText { get; } = LocalizationService.Get("TaskMenu_MoveTo");

    /// <summary>칸반 열 하단의 작업 추가 버튼 라벨</summary>
    public static string ColumnAddText { get; } = LocalizationService.Get("TaskColumnAdd");

    // 칸반/목록 세그먼트 토글 라벨 (아이콘과 함께 넣어야 해서 x:Uid 대신 정적 참조로 구성)
    public static string ViewKanbanText { get; } = LocalizationService.Get("TaskView_Kanban");
    public static string ViewListText { get; } = LocalizationService.Get("TaskView_List");

    // 칸반 열 헤더의 상태 dot 색 — Palette.xaml의 AppAccent/AppInfo/AppSuccess/AppWarning과 같은 값.
    // x:Bind는 ThemeResource를 받을 수 없어 우선순위 배지와 동일하게 정적 브러시로 둔다.
    private static readonly SolidColorBrush _statusDotWaiting = new(ColorHelper.FromArgb(0xFF, 0xF0, 0x71, 0x6A));
    private static readonly SolidColorBrush _statusDotActive = new(ColorHelper.FromArgb(0xFF, 0x5B, 0x93, 0xD8));
    private static readonly SolidColorBrush _statusDotCompleted = new(ColorHelper.FromArgb(0xFF, 0x5D, 0xB4, 0x63));
    private static readonly SolidColorBrush _statusDotHold = new(ColorHelper.FromArgb(0xFF, 0xD9, 0x95, 0x4A));

    public static Brush StatusDotWaiting => _statusDotWaiting;
    public static Brush StatusDotActive => _statusDotActive;
    public static Brush StatusDotCompleted => _statusDotCompleted;
    public static Brush StatusDotHold => _statusDotHold;

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

    /// <summary>칸반 카드의 날짜 범위 표시 ("MM-dd – MM-dd").
    /// 한쪽만 지정된 경우 그쪽만 표시해 "07-24 – " 같은 반쪽 표기를 만들지 않는다.</summary>
    public static string FormatDateRange(DateTime? start, DateTime? end)
    {
        if (start.HasValue && end.HasValue)
            return string.Format(LocalizationService.Get("TaskDateRange"), $"{start.Value:MM-dd}", $"{end.Value:MM-dd}");
        if (start.HasValue) return $"{start.Value:MM-dd}";
        if (end.HasValue) return $"{end.Value:MM-dd}";
        return string.Empty;
    }

    /// <summary>날짜 범위 표시 여부 (시작·종료 중 하나라도 있으면 표시).</summary>
    public static Visibility DateRangeVisibility(DateTime? start, DateTime? end)
        => start.HasValue || end.HasValue ? Visibility.Visible : Visibility.Collapsed;

    // 우선순위 배지 색 — x:Bind 함수 바인딩은 ThemeResource를 받을 수 없어
    // TestPage.StatusBrush와 동일하게 정적 브러시로 둔다(Palette.xaml 값과 수동으로 맞춘다).
    //   High   글자 AppWarningColor(#D9954A) / 배경 AppWarningSoftBrush(#28D9954A)
    //   Normal 글자 AppInfoColor(#5B93D8)    / 배경 AppInfoSoftBrush(#285B93D8)
    //   Low    글자는 배경 위 가독성을 위해 AppTextTertiary(#8A8890)를 쓰고(AppTextMuted #6F6D75는 너무 어둡다),
    //          배경만 AppMutedSoftBrush(#286F6D75)와 같은 값.
    private static readonly SolidColorBrush _priorityHighBrush = new(ColorHelper.FromArgb(0xFF, 0xD9, 0x95, 0x4A));
    private static readonly SolidColorBrush _priorityNormalBrush = new(ColorHelper.FromArgb(0xFF, 0x5B, 0x93, 0xD8));
    private static readonly SolidColorBrush _priorityLowBrush = new(ColorHelper.FromArgb(0xFF, 0x8A, 0x88, 0x90));
    private static readonly SolidColorBrush _priorityHighSoftBrush = new(ColorHelper.FromArgb(0x28, 0xD9, 0x95, 0x4A));
    private static readonly SolidColorBrush _priorityNormalSoftBrush = new(ColorHelper.FromArgb(0x28, 0x5B, 0x93, 0xD8));
    private static readonly SolidColorBrush _priorityLowSoftBrush = new(ColorHelper.FromArgb(0x28, 0x6F, 0x6D, 0x75));

    /// <summary>우선순위 배지의 글자 색</summary>
    public static Brush PriorityBrush(TaskPriority priority) => priority switch
    {
        TaskPriority.High => _priorityHighBrush,
        TaskPriority.Low => _priorityLowBrush,
        _ => _priorityNormalBrush,
    };

    /// <summary>우선순위 배지의 배경 색 (같은 색상의 저채도 변형)</summary>
    public static Brush PriorityBadgeBrush(TaskPriority priority) => priority switch
    {
        TaskPriority.High => _priorityHighSoftBrush,
        TaskPriority.Low => _priorityLowSoftBrush,
        _ => _priorityNormalSoftBrush,
    };

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

    /// <summary>칸반 열 하단의 "새 작업" 버튼 — 새 작업을 그 열의 상태로 만들어 추가합니다.</summary>
    private async void ColumnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string statusTag }) return;
        if (!Enum.TryParse<TodoStatus>(statusTag, out var status)) return;

        var dialog = new TaskEditDialog(null, _settings, status);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || dialog.ResultTodo is not { } todo) return;

        // 버튼이 속한 열의 상태를 이어받는다 (다이얼로그는 상태를 헤더에 표시만 하고 설정하지 않는다).
        todo.Status = status;
        Vm.AddTodo(todo);

        // FR-T6: "테스트 추가" 토글이 켜져 있으면 연결 테스트 생성 (헤더 버튼이 제거되면 이 경로가 유일한 생성 지점)
        if (dialog.AddTestRequested)
            Vm.CreateLinkedTest(todo);
    }

    /// <summary>편집 다이얼로그를 열고 결과를 반영합니다 (목록 뷰 버튼·칸반 카드 클릭·우클릭 메뉴 공용).</summary>
    private async Task EditTodoAsync(TodoItem todo)
    {
        var dialog = new TaskEditDialog(todo, _settings);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.ResultTodo is not null)
            Vm.UpdateTodo(todo);
    }

    /// <summary>삭제 확인 후 작업을 삭제합니다 (목록 뷰 버튼·칸반 우클릭 메뉴 공용).</summary>
    private async Task DeleteTodoAsync(TodoItem todo)
    {
        var confirmed = await DialogService.ShowConfirmAsync(
            string.Format(LocalizationService.Get("TaskDelete_Confirm"), todo.Text),
            LocalizationService.Get("TaskDelete_Title"));
        if (confirmed)
            Vm.DeleteTodo(todo);
    }

    private async void EditTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TodoItem todo })
            await EditTodoAsync(todo);
    }

    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TodoItem todo })
            await DeleteTodoAsync(todo);
    }

    // ===== 칸반 카드 조작 (클릭 = 편집, 우클릭 메뉴 = 편집·삭제·상태 변경) =====

    /// <summary>칸반 카드를 클릭하면 편집 다이얼로그를 연다.
    /// 드래그가 성립한 경우에는 Tapped가 발생하지 않으므로 드래그앤드롭과 충돌하지 않는다.</summary>
    private async void Card_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TodoItem todo })
            await EditTodoAsync(todo);
    }

    private async void CardMenuEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TodoItem todo })
            await EditTodoAsync(todo);
    }

    private async void CardMenuDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TodoItem todo })
            await DeleteTodoAsync(todo);
    }

    // 상태 변경 하위 메뉴 — MenuFlyoutItem은 Tag 하나만 쓸 수 있어 상태별로 얇은 핸들러를 둔다.
    private void CardMenuMoveWaiting_Click(object sender, RoutedEventArgs e) => MoveFromMenu(sender, TodoStatus.Waiting);
    private void CardMenuMoveActive_Click(object sender, RoutedEventArgs e) => MoveFromMenu(sender, TodoStatus.Active);
    private void CardMenuMoveCompleted_Click(object sender, RoutedEventArgs e) => MoveFromMenu(sender, TodoStatus.Completed);
    private void CardMenuMoveHold_Click(object sender, RoutedEventArgs e) => MoveFromMenu(sender, TodoStatus.Hold);

    private void MoveFromMenu(object sender, TodoStatus status)
    {
        if (sender is FrameworkElement { Tag: TodoItem todo })
            Vm.MoveToStatus(todo, status);
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
