using System.Collections.Specialized;
using DevDashboard.Models;
using DevDashboard.Services;
using DevDashboard.ViewModels;
using DevDashboard.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace DevDashboard.Views;

public sealed partial class DashboardView : UserControl
{
    // --- 드롭 플레이스홀더 상태 ---
    private readonly DropPlaceholder _dropPlaceholder = new();
    private int _dropPlaceholderIndex = -1;
    private string? _dropTargetId;
    private bool _dropIsLeft;

    // DragStarting에서 설정 — DragOver/Drop에서 동기적으로 사용
    private string? _draggedCardId;

    // DataContextChanged 시 이전 구독 해제용
    private MainViewModel? _subscribedVm;

    private MainViewModel? Vm => DataContext as MainViewModel;

    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // ─── DataContext 변경 시 카드 이벤트 구독 관리 ──────────────────────

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // 이전 VM 구독 해제
        if (_subscribedVm is not null)
        {
            _subscribedVm.DisplayCards.CollectionChanged -= OnDisplayCardsChanged;
            foreach (var item in _subscribedVm.DisplayCards)
                if (item is ProjectCardViewModel card)
                    UnsubscribeCardEvents(card);
            _subscribedVm = null;
        }

        // 새 VM 구독
        if (args.NewValue is MainViewModel vm)
        {
            _subscribedVm = vm;
            vm.DisplayCards.CollectionChanged += OnDisplayCardsChanged;
            foreach (var item in vm.DisplayCards)
                if (item is ProjectCardViewModel card)
                    SubscribeCardEvents(card);
        }
    }

    private void OnDisplayCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (var item in e.NewItems)
                if (item is ProjectCardViewModel card)
                    SubscribeCardEvents(card);

        if (e.OldItems is not null)
            foreach (var item in e.OldItems)
                if (item is ProjectCardViewModel card)
                    UnsubscribeCardEvents(card);
    }

    private void SubscribeCardEvents(ProjectCardViewModel card)
    {
        card.ShowGitStatusRequested += OnShowGitStatusRequested;
        card.OpenTodoRequested += OnOpenTodoRequested;
        card.OpenHistoryRequested += OnOpenHistoryRequested;
        card.ConfigureCommandSlotRequested += OnConfigureCommandSlotRequested;
        card.ChangeCommandIconRequested += OnChangeCommandIconRequested;
    }

    private void UnsubscribeCardEvents(ProjectCardViewModel card)
    {
        card.ShowGitStatusRequested -= OnShowGitStatusRequested;
        card.OpenTodoRequested -= OnOpenTodoRequested;
        card.OpenHistoryRequested -= OnOpenHistoryRequested;
        card.ConfigureCommandSlotRequested -= OnConfigureCommandSlotRequested;
        card.ChangeCommandIconRequested -= OnChangeCommandIconRequested;
    }

    // ─── 추가 카드 클릭 ────────────────────────────────────────────────

    private void AddCard_Click(object sender, RoutedEventArgs e)
        => Vm?.RequestAddProjectCommand.Execute(null);

    // ─── 다이얼로그 핸들러 ────────────────────────────────────────────

    private async void OnShowGitStatusRequested(object? sender, EventArgs e)
    {
        if (sender is not ProjectCardViewModel card) return;
        var dialog = new GitStatusDialog(card);
        await dialog.ShowAsync();
    }

    private async void OnOpenTodoRequested(object? sender, EventArgs e)
    {
        if (sender is not ProjectCardViewModel card) return;
        var dialogVm = card.CreateTodoDialogViewModel();
        var dialog = new TodoDialog(dialogVm);
        await dialog.ShowAsync();
        card.OnTodoDialogClosed(dialogVm, dialog.NewHistories);
    }

    private async void OnOpenHistoryRequested(object? sender, EventArgs e)
    {
        if (sender is not ProjectCardViewModel card) return;
        var dialogVm = card.CreateHistoryDialogViewModel();
        var dialog = new HistoryDialog(dialogVm);
        await dialog.ShowAsync();
        card.OnHistoryDialogClosed(dialogVm);
    }

    private async void OnConfigureCommandSlotRequested(object? sender, int slotIndex)
    {
        if (sender is not ProjectCardViewModel card) return;
        var existing = card.GetCommandScriptForDialog(slotIndex);
        var dialog = new CommandScriptDialog(existing);
        await dialog.ShowAsync();
        if (dialog.ResultScript is not null)
            card.ApplyCommandScriptResult(slotIndex, dialog.ResultScript);
    }

    private async void OnChangeCommandIconRequested(object? sender, int slotIndex)
    {
        if (sender is not ProjectCardViewModel card) return;
        var dialog = new IconPickerDialog();
        await dialog.ShowAsync();
        if (dialog.SelectedGlyph is not null)
            card.ApplyCommandIconResult(slotIndex, dialog.SelectedGlyph);
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
        ShowDropPlaceholder(target, e.GetPosition(element));
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

    // ─── 드롭 플레이스홀더 관리 ────────────────────────────────────────

    /// <summary>대상 카드의 좌/우에 빈 카드 플레이스홀더를 삽입합니다.</summary>
    private void ShowDropPlaceholder(ProjectCardViewModel target, Windows.Foundation.Point posInTarget)
    {
        if (Vm is null) return;

        var isLeft = posInTarget.X < 165; // 카드 폭(330)의 절반

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
