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

    // 카드 우클릭 메뉴 라벨 (x:Bind 정적 참조)
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

    /// <summary>상태 dot 색 — 목록 뷰는 상태 그룹을 데이터로 돌리므로 열마다 고정인 칸반과 달리 상태로부터 골라야 한다.</summary>
    public static Brush StatusDotBrush(TodoStatus status) => status switch
    {
        TodoStatus.Active => _statusDotActive,
        TodoStatus.Completed => _statusDotCompleted,
        TodoStatus.Hold => _statusDotHold,
        _ => _statusDotWaiting,
    };

    /// <summary>상태를 Tag 문자열로 변환합니다 (ColumnAdd_Click·Column_Drop이 Tag: string → Enum.TryParse로 받는다).
    /// 칸반은 열마다 Tag를 리터럴로 적지만 목록은 데이터 주도라 변환이 필요하다.</summary>
    public static string StatusTag(TodoStatus status) => status.ToString();

    // 칸반 열 헤더 라벨 (x:Bind 정적 참조 — 목록 그룹 라벨은 VM이 그룹 데이터에 담아 넘긴다)
    public static string LabelWaiting { get; } = LocalizationService.Get("TaskStatus_Waiting");
    public static string LabelActive { get; } = LocalizationService.Get("TaskStatus_Active");
    public static string LabelCompleted { get; } = LocalizationService.Get("TaskStatus_Completed");
    public static string LabelHold { get; } = LocalizationService.Get("TaskStatus_Hold");

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

    /// <summary>카드·행의 날짜 범위 표시 ("MM-dd – MM-dd").
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

    // 테스트 배지 색 — 시안 기준: 100% 통과는 초록(#5DB463), 실행했으나 100% 미만이면 호박(#E8B45A).
    // 미실행("테스트 미실행")은 배지 자체가 별도 테두리형 Border라 배경·글자색을 XAML에 직접 적었다(아래 IdleTestBadgeVisibility 참조).
    // (우선순위 배지와 같은 이유로 정적 브러시: x:Bind 함수 바인딩은 ThemeResource를 받지 못한다)
    private static readonly SolidColorBrush _testBadgeBrush = new(ColorHelper.FromArgb(0xFF, 0x5D, 0xB4, 0x63));
    private static readonly SolidColorBrush _testBadgeSoftBrush = new(ColorHelper.FromArgb(0x28, 0x5D, 0xB4, 0x63));
    private static readonly SolidColorBrush _testBadgeAmberBrush = new(ColorHelper.FromArgb(0xFF, 0xE8, 0xB4, 0x5A));
    private static readonly SolidColorBrush _testBadgeAmberSoftBrush = new(ColorHelper.FromArgb(0x28, 0xE8, 0xB4, 0x5A));

    /// <summary>테스트 배지(실행됨)의 글자 색 — 100% 통과면 초록, 그 미만이면 호박</summary>
    public static Brush TestBadgeForeground(bool isFullPass)
        => isFullPass ? _testBadgeBrush : _testBadgeAmberBrush;

    /// <summary>테스트 배지(실행됨)의 배경 색 (같은 색상의 저채도 변형)</summary>
    public static Brush TestBadgeBackground(bool isFullPass)
        => isFullPass ? _testBadgeSoftBrush : _testBadgeAmberSoftBrush;

    /// <summary>"테스트 미실행" 테두리형 배지의 표시 여부 (배지 대상이고 아직 한 건도 실행 안 됐을 때만)</summary>
    public static Visibility IdleTestBadgeVisibility(string badge, bool hasTestResult)
        => !string.IsNullOrEmpty(badge) && !hasTestResult ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>통과율 채움형 배지의 표시 여부 (배지 대상이고 한 건이라도 실행됐을 때만)</summary>
    public static Visibility RanTestBadgeVisibility(string badge, bool hasTestResult)
        => !string.IsNullOrEmpty(badge) && hasTestResult ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>목록 상태 그룹의 "작업 없음" 안내 표시 여부</summary>
    public static Visibility EmptyGroupVisibility(bool isEmpty)
        => isEmpty ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>목록 상태 그룹의 카테고리 서브그룹 표시 여부 ("작업 없음" 안내와 배타)</summary>
    public static Visibility CategoriesVisibility(bool isEmpty)
        => isEmpty ? Visibility.Collapsed : Visibility.Visible;

    // ===== 네비게이션·뷰 =====

    private void Back_Click(object sender, RoutedEventArgs e)
        => (App.MainWindow as MainWindow)?.ShowDashboard();

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedIndex: var index } combo) return;
        Vm.SelectedCategoryFilter = index <= 0 ? null : combo.SelectedItem as string;
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

    // ===== 카드·행 조작 (클릭 = 편집, 우클릭 메뉴 = 편집·삭제·상태 변경) =====

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

    // ===== 목록 행 hover =====
    // DataTemplate 안에서는 VisualStateManager.GoToState가 동작하지 않아 포인터 이벤트로 직접 테두리를 바꾼다.

    private void ListRow_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
            border.BorderBrush = (Brush)Resources["ListRowHoverBorderBrush"];
    }

    private void ListRow_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
            border.BorderBrush = (Brush)Resources["ListRowBorderBrush"];
    }

    // ===== 칸반·목록 드래그앤드롭 (상태 열/그룹 간 이동, FR-T3) =====

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
