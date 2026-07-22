using System.Collections.Specialized;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using DevDashboard.Presentation.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace DevDashboard.Presentation.Views;

public sealed partial class DashboardView : UserControl
{
    // --- 드롭 플레이스홀더 상태 ---
    private readonly DropPlaceholder _dropPlaceholder = new();
    private int _dropPlaceholderIndex = -1;
    private string? _dropTargetId;
    private bool _dropIsLeft;

    // DragStarting에서 설정 — DragOver/Drop에서 동기적으로 사용
    private string? _draggedCardId;

    // 현재 구독 중인 VM (Loaded·Unloaded·DataContextChanged 경로에서 구독 상태를 추적)
    private MainViewModel? _subscribedVm;

    // 현재 구독 중인 카드 집합 (구독/해제 대칭 유지용)
    private readonly HashSet<ProjectCardViewModel> _subscribedCards = [];

    private MainViewModel? Vm => DataContext as MainViewModel;

    public DashboardView()
    {
        InitializeLocalizedResources();
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // 이 뷰는 MainWindow._dashboardView로 재사용된다 — 페이지 전환 시 트리에서 분리됐다 다시 붙는다.
    // DataContext(MainViewModel)가 바뀌지 않아 DataContextChanged가 재발동하지 않으므로,
    // 복귀 시 카드 이벤트 재구독 지점은 Loaded다. 구독 대상이 현재 DataContext와 어긋날 때만
    // 재구독해(Loaded/Unloaded 처리 순서가 뒤바뀌어도 안전) 중복 구독을 막는다.
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        if (ReferenceEquals(_subscribedVm, vm)) return;

        UnsubscribeAll();
        SubscribeAll(vm);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // 재사용 뷰이므로 Unloaded·DataContextChanged 핸들러 자체는 떼지 않는다
        // (떼면 복귀 시 Loaded/재구독 경로가 사라져 카드 버튼이 영구히 죽는다).
        UnsubscribeAll();
    }

    /// <summary>DataTemplate 내 요소는 x:Name 접근이 불가하여 InitializeComponent() 전
    /// this.Resources에 직접 주입합니다. StaticResource는 XAML 파싱 시 한 번만 resolve되므로
    /// 파싱 전에 값이 있어야 현지화가 적용됩니다.</summary>
    private void InitializeLocalizedResources()
    {
        this.Resources["ToolTip_ProjectPathNotFound"] = LocalizationService.Get("ToolTip_ProjectPathNotFound");
        this.Resources["ToolTip_PinUnpin"]            = LocalizationService.Get("ToolTip_PinUnpin");
        this.Resources["ToolTip_Delete"]              = LocalizationService.Get("ToolTip_Delete");
        this.Resources["ToolTip_ToDo"]                = LocalizationService.Get("ToolTip_ToDo");
        this.Resources["ToolTip_TestList"]            = LocalizationService.Get("ToolTip_TestList");
        this.Resources["ToolTip_More"]                = LocalizationService.Get("ToolTip_More");
        this.Resources["ToolTip_AddScript"]           = LocalizationService.Get("ToolTip_AddScript");
    }

    // ─── DataContext 변경 시 카드 이벤트 구독 관리 ──────────────────────

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // 이전 VM 해제 후 새 VM 구독. args.NewValue를 그대로 넘겨 현행 동작을 유지한다
        // (DataContext 프로퍼티가 이 시점에 갱신됐는지 가정하지 않는다).
        UnsubscribeAll();
        SubscribeAll(args.NewValue as MainViewModel);
    }

    private void OnDisplayCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Reset 액션은 OldItems/NewItems가 null — 카드 구독만 통째로 재구축한다.
        // (VM 수준 SubscribeAll/UnsubscribeAll을 쓰면 자기가 처리 중인 CollectionChanged를
        //  해제·재등록하고 _subscribedVm을 null로 만들었다 되살리는 다른 동작이 된다.)
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UnsubscribeCards();
            SubscribeCards();
            return;
        }

        if (e.NewItems is not null)
            foreach (var item in e.NewItems)
                if (item is ProjectCardViewModel card)
                {
                    SubscribeCardEvents(card);
                    _subscribedCards.Add(card);
                }

        if (e.OldItems is not null)
            foreach (var item in e.OldItems)
                if (item is ProjectCardViewModel card)
                {
                    UnsubscribeCardEvents(card);
                    _subscribedCards.Remove(card);
                }
    }

    // ─── 구독 수명 관리 (카드 수준 / VM 수준 2단) ──────────────────────

    /// <summary>현재 구독 중인 VM의 카드 전부에 이벤트를 구독합니다.</summary>
    private void SubscribeCards()
    {
        if (_subscribedVm is null) return;
        foreach (var item in _subscribedVm.DisplayCards)
            if (item is ProjectCardViewModel card)
            {
                SubscribeCardEvents(card);
                _subscribedCards.Add(card);
            }
    }

    /// <summary>구독 중인 카드 전부의 이벤트를 해제합니다.</summary>
    private void UnsubscribeCards()
    {
        foreach (var card in _subscribedCards)
            UnsubscribeCardEvents(card);
        _subscribedCards.Clear();
    }

    /// <summary>VM 수준 구독: CollectionChanged + 현재 카드 전부. vm이 null이면 아무것도 하지 않습니다.</summary>
    private void SubscribeAll(MainViewModel? vm)
    {
        if (vm is null) return;
        _subscribedVm = vm;
        vm.DisplayCards.CollectionChanged += OnDisplayCardsChanged;
        SubscribeCards();
    }

    /// <summary>VM 수준 해제: 카드 전부 + CollectionChanged + _subscribedVm 정리.</summary>
    private void UnsubscribeAll()
    {
        if (_subscribedVm is null) return;
        _subscribedVm.DisplayCards.CollectionChanged -= OnDisplayCardsChanged;
        UnsubscribeCards();
        _subscribedVm = null;
    }

    private void SubscribeCardEvents(ProjectCardViewModel card)
    {
        card.ShowGitStatusRequested += OnShowGitStatusRequested;
        card.OpenTodoRequested += OnOpenTodoRequested;
        card.OpenHistoryRequested += OnOpenHistoryRequested;
        card.OpenTestListRequested += OnOpenTestListRequested;
        card.ConfigureCommandSlotRequested += OnConfigureCommandSlotRequested;
        card.ChangeCommandIconRequested += OnChangeCommandIconRequested;
    }

    private void UnsubscribeCardEvents(ProjectCardViewModel card)
    {
        card.ShowGitStatusRequested -= OnShowGitStatusRequested;
        card.OpenTodoRequested -= OnOpenTodoRequested;
        card.OpenHistoryRequested -= OnOpenHistoryRequested;
        card.OpenTestListRequested -= OnOpenTestListRequested;
        card.ConfigureCommandSlotRequested -= OnConfigureCommandSlotRequested;
        card.ChangeCommandIconRequested -= OnChangeCommandIconRequested;
    }

    // ─── 추가 카드 클릭 ────────────────────────────────────────────────

    private void AddCard_Click(object sender, RoutedEventArgs e)
        => Vm?.RequestAddProjectCommand.Execute(null);

    // ─── 다이얼로그 핸들러 ────────────────────────────────────────────

    private async void OnShowGitStatusRequested(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not ProjectCardViewModel card) return;
            var dialog = new GitStatusDialog(card);
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private async void OnOpenTodoRequested(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not ProjectCardViewModel card || Vm is null) return;
            // 작업(To-Do)은 다이얼로그 대신 전체 페이지로 전환한다 (FR-C3/FR-T2)
            var page = new TaskPage(card.CreateTaskPageViewModel(), Vm.GetSettings());
            (App.MainWindow as MainWindow)?.ShowPage(page);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private async void OnOpenHistoryRequested(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not ProjectCardViewModel card) return;
            var dialogVm = card.CreateHistoryDialogViewModel();
            var dialog = new HistoryDialog(dialogVm);
            await dialog.ShowAsync();
            card.OnHistoryDialogClosed(dialogVm);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private async void OnOpenTestListRequested(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not ProjectCardViewModel card) return;
            // 테스트는 다이얼로그 대신 전체 페이지로 전환한다 (FR-E1)
            var page = new TestPage(card.CreateTestPageViewModel());
            (App.MainWindow as MainWindow)?.ShowPage(page);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private async void OnConfigureCommandSlotRequested(object? sender, int slotIndex)
    {
        try
        {
            if (sender is not ProjectCardViewModel card) return;
            var existing = card.GetCommandScriptForDialog(slotIndex);
            var dialog = new CommandScriptDialog(existing);
            await dialog.ShowAsync();
            if (dialog.ResultScript is not null)
                card.ApplyCommandScriptResult(slotIndex, dialog.ResultScript);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private async void OnChangeCommandIconRequested(object? sender, int slotIndex)
    {
        try
        {
            if (sender is not ProjectCardViewModel card) return;
            var dialog = new IconPickerDialog();
            await dialog.ShowAsync();
            if (dialog.SelectedGlyph is not null)
                card.ApplyCommandIconResult(slotIndex, dialog.SelectedGlyph);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    /// <summary>
    /// 미설정 슬롯의 빈 컨텍스트 메뉴를 차단합니다.
    /// Visibility 바인딩은 첫 오픈 시 아직 평가되지 않을 수 있으므로
    /// ViewModel 상태(GetCommandScriptForDialog)를 직접 확인합니다.
    /// </summary>
    private void CmdFlyout_Opening(object sender, object e)
    {
        if (sender is not MenuFlyout flyout) return;
        if (flyout.Target is Button { DataContext: ProjectCardViewModel vm, CommandParameter: string param }
            && int.TryParse(param, out var index)
            && vm.GetCommandScriptForDialog(index) is null)
            flyout.Hide();
    }

    /// <summary>미설정 커맨드 슬롯의 빈 컨텍스트 메뉴 표시를 차단합니다.</summary>
    private void CmdSlot_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
    {
        args.Handled = true;

        if (sender is Button { DataContext: ProjectCardViewModel vm, CommandParameter: string param } btn
            && int.TryParse(param, out var index)
            && vm.GetCommandScriptForDialog(index) is not null
            && btn.ContextFlyout is { } flyout)
        {
            var options = new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions();
            if (args.TryGetPosition(btn, out var point))
                options.Position = point;

            flyout.ShowAt(btn, options);
        }
    }

    // ─── 드래그앤드롭 ───────────────────────────────────────────────────

    private void Card_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if (sender is FrameworkElement element &&
            element.DataContext is ProjectCardViewModel card && card.IsPinned)
        {
            _draggedCardId = card.Id;
            e.Data.SetText(card.Id);
            e.Data.RequestedOperation = DataPackageOperation.Move;
            // WinUI 3에서는 SetContentFromElement 없음 — 기본 드래그 비주얼 사용
        }
        else
        {
            e.Cancel = true;
        }
    }

    /// <summary>드래그가 드롭 없이 종료되거나 취소될 때 플레이스홀더를 정리합니다.</summary>
    // 카드 hover 테두리 (시안 :352). DataTemplate 안에서는 VisualStateManager가 동작하지 않아
    // 포인터 이벤트로 직접 브러시를 바꾼다(TestPage·TaskPage 행 hover와 같은 방식).
    private void Card_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border card)
            card.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Resources["CardHoverBorderBrush"];
    }

    private void Card_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border card)
            card.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Resources["CardBorderBrush"];
    }

    // "새 프로젝트 추가" 카드 hover — 시안(:347)은 액센트 테두리로 강조한다
    private void AddCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border card)
            card.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppAccentBrush"];
    }

    private void AddCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border card)
            card.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Resources["CardBorderBrush"];
    }

    // 삭제 버튼은 헤더 밴드 위에 있어 평소엔 흰색, hover에서만 위험색으로 바꾼다(시안 :360).
    // 공용 CardIconButtonStyle을 여러 버튼이 공유하므로 템플릿 대신 여기서 전경만 바꾼다.
    private void DeleteButton_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        => SetDeleteGlyphBrush(sender, "HeaderIconDangerBrush");

    private void DeleteButton_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        => SetDeleteGlyphBrush(sender, "HeaderIconBrush");

    private void SetDeleteGlyphBrush(object sender, string resourceKey)
    {
        if (sender is Button { Content: FontIcon glyph })
            glyph.Foreground = (Microsoft.UI.Xaml.Media.Brush)Resources[resourceKey];
    }

    private void Card_DropCompleted(UIElement sender, DropCompletedEventArgs e)
    {
        RemoveDropPlaceholder();
        _draggedCardId = null;
    }

    private void Card_DragOver(object sender, DragEventArgs e)
    {
        if (string.IsNullOrEmpty(_draggedCardId)) return;
        if (sender is not FrameworkElement element) return;
        if (element.DataContext is not ProjectCardViewModel target || !target.IsPinned) return;
        if (_draggedCardId == target.Id) return;

        e.AcceptedOperation = DataPackageOperation.Move;
        ShowDropPlaceholder(target, e.GetPosition(element), element.ActualWidth);
    }

    private void Card_Drop(object sender, DragEventArgs e)
    {
        var draggedId = _draggedCardId;
        var targetId = _dropTargetId;
        var insertAfter = !_dropIsLeft;

        RemoveDropPlaceholder();

        if (!string.IsNullOrEmpty(draggedId) &&
            !string.IsNullOrEmpty(targetId) &&
            draggedId != targetId)
        {
            Vm?.MovePinnedCard(draggedId, targetId, insertAfter);
        }

        e.Handled = true;
    }

    // ─── 플레이스홀더 드래그 이벤트 핸들러 ────────────────────────────────
    // 플레이스홀더 위에서도 AcceptedOperation을 설정해야 드롭이 허용됨

    private void Placeholder_DragOver(object sender, DragEventArgs e)
    {
        if (string.IsNullOrEmpty(_draggedCardId)) return;
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private void Placeholder_Drop(object sender, DragEventArgs e)
    {
        var draggedId = _draggedCardId;
        var targetId = _dropTargetId;
        var insertAfter = !_dropIsLeft;

        RemoveDropPlaceholder();

        if (!string.IsNullOrEmpty(draggedId) &&
            !string.IsNullOrEmpty(targetId) &&
            draggedId != targetId)
        {
            Vm?.MovePinnedCard(draggedId, targetId, insertAfter);
        }

        e.Handled = true;
    }

    // ─── 드롭 플레이스홀더 관리 ────────────────────────────────────────

    /// <summary>대상 카드의 좌/우에 빈 카드 플레이스홀더를 삽입합니다.</summary>
    /// <param name="targetWidth">대상 카드의 실제 폭 — 카드 폭이 창 크기에 따라 변하므로 상수로 둘 수 없다.</param>
    private void ShowDropPlaceholder(ProjectCardViewModel target, Windows.Foundation.Point posInTarget, double targetWidth)
    {
        if (Vm is null) return;

        // 폭을 아직 못 재는 시점(레이아웃 전)이면 좌측으로 본다
        var isLeft = targetWidth <= 0 || posInTarget.X < targetWidth / 2;

        // 같은 대상, 같은 방향이면 재삽입 불필요
        if (_dropTargetId == target.Id && _dropIsLeft == isLeft && _dropPlaceholderIndex >= 0)
            return;

        RemoveDropPlaceholder();

        var targetIndex = Vm.DisplayCards.IndexOf(target);
        if (targetIndex < 0) return;

        var insertIndex = isLeft ? targetIndex : targetIndex + 1;
        if (insertIndex >= 0 && insertIndex <= Vm.DisplayCards.Count)
        {
            Vm.DisplayCards.Insert(insertIndex, _dropPlaceholder);
            _dropPlaceholderIndex = insertIndex;
            _dropTargetId = target.Id;
            _dropIsLeft = isLeft;
        }
    }

    /// <summary>드롭 플레이스홀더를 제거합니다.</summary>
    private void RemoveDropPlaceholder()
    {
        if (_dropPlaceholderIndex < 0) return;
        Vm?.DisplayCards.Remove(_dropPlaceholder);
        _dropPlaceholderIndex = -1;
        _dropTargetId = null;
    }
}
