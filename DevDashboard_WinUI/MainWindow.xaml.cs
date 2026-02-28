using System.Text.RegularExpressions;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using DevDashboard.Presentation.Views;
using DevDashboard.Presentation.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace DevDashboard;

public sealed partial class MainWindow : WindowEx
{
    private MainViewModel? _viewModel;
    private readonly AppSettings _settings;
    private readonly JsonStorageService _storageService;
    private readonly IProjectRepository? _projectRepository;
    private readonly string? _dbErrorMessage;

    // SortOrder 상수 — XAML x:Bind CommandParameter용
    public SortOrder SortByName { get; } = SortOrder.Name;
    public SortOrder SortByCategory { get; } = SortOrder.Category;
    public SortOrder SortByDate { get; } = SortOrder.CreatedAt;

    public MainWindow(AppSettings settings, JsonStorageService storageService,
        IProjectRepository? projectRepository, string? dbErrorMessage = null)
    {
        _settings = settings;
        _storageService = storageService;
        _projectRepository = projectRepository;
        _dbErrorMessage = dbErrorMessage;

        InitializeComponent();

        // 타이틀 바 확장 설정
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // 초기 창 크기
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));

        RootGrid.Loaded += OnRootGridLoaded;
        Closed += OnWindowClosed;
    }

    private async void OnRootGridLoaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= OnRootGridLoaded;

        // 다이얼로그 독립 창의 소유자 창 등록 (RootGrid를 전달하여 XAML 오버레이 입력 차단에 사용)
        DialogWindowHost.SetOwnerWindow(this, RootGrid);

        // Run.Text는 x:Uid를 지원하지 않으므로 ResourceLoader로 직접 설정
        ProjectCountSuffixRun.Text = LocalizationService.Get("MainWindow_ProjectCountSuffix");

        // [ToolTipService.ToolTip] x:Uid는 런타임 오류를 발생시키므로 코드비하인드로 설정
        ApplyToolTips();

        // DB 초기화 오류 처리
        if (_dbErrorMessage is not null)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("DatabaseInitError"), _dbErrorMessage),
                LocalizationService.Get("StartupError"));
            Close();
            return;
        }

        // ViewModel 초기화
        _viewModel = new MainViewModel(_settings, _storageService, _projectRepository!);
        RootGrid.DataContext = _viewModel;

        // 이벤트 구독
        _viewModel.EditProjectRequested += OnEditProjectRequested;
        _viewModel.AddProjectRequested += OnAddProjectRequested;

        // DashboardView 주입
        DashboardContent.Content = new DashboardView { DataContext = _viewModel };

        // 비동기 초기화
        await _viewModel.InitializeAsync();
        _ = _viewModel.CheckLatestVersionAsync();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (_viewModel is not null)
        {
            _viewModel.EditProjectRequested -= OnEditProjectRequested;
            _viewModel.AddProjectRequested -= OnAddProjectRequested;
        }
    }

    /// <summary>[ToolTipService.ToolTip] x:Uid는 런타임 XamlParseException을 발생시키므로
    /// 코드비하인드에서 LocalizationService로 직접 설정합니다.</summary>
    private void ApplyToolTips()
    {
        ToolTipService.SetToolTip(GridViewButton, LocalizationService.Get("ToolTip_GridView"));
        ToolTipService.SetToolTip(ListViewButton, LocalizationService.Get("ToolTip_ListView"));
        ToolTipService.SetToolTip(SortButton, LocalizationService.Get("ToolTip_Sort"));
        ToolTipService.SetToolTip(AllHistoryButton, LocalizationService.Get("ToolTip_AllHistory"));
        ToolTipService.SetToolTip(AppSettingsButton, LocalizationService.Get("ToolTip_AppSettings"));
        ToolTipService.SetToolTip(AddGroupButton, LocalizationService.Get("ToolTip_AddGroup"));
        ToolTipService.SetToolTip(RefreshButton, LocalizationService.Get("ToolTip_Refresh"));
    }

    // ─── 검색창 ─────────────────────────────────────────────────────────

    private static readonly Regex _searchSanitizePattern =
        new(@"['\"";\\\-\-\/\*=<>\x00]", RegexOptions.Compiled);
    private const int SearchMaxLength = 30;

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_viewModel is null) return;
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

        var raw = sender.Text ?? string.Empty;
        var filtered = _searchSanitizePattern.Replace(raw, string.Empty);

        if (filtered.Length > SearchMaxLength)
            filtered = filtered[..SearchMaxLength];

        if (filtered != raw)
        {
            sender.Text = filtered;
            return;
        }

        _viewModel.SearchText = filtered;
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_viewModel is null) return;
        if (e.Key != Windows.System.VirtualKey.Enter) return;

        var firstCard = _viewModel.FilteredCards.FirstOrDefault();
        if (firstCard is null) return;

        firstCard.OpenInDevToolCommand.Execute(null);
        e.Handled = true;
    }

    // ─── 뷰 모드 ────────────────────────────────────────────────────────

    private void GridViewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.ViewMode != ViewMode.Grid)
            _viewModel?.ToggleViewModeCommand.Execute(null);
    }

    private void ListViewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.ViewMode != ViewMode.List)
            _viewModel?.ToggleViewModeCommand.Execute(null);
    }

    // ─── 그룹 탭 ────────────────────────────────────────────────────────

    private void AllGroupTab_Checked(object sender, RoutedEventArgs e)
    {
        _viewModel?.SelectGroupCommand.Execute(null);
    }

    private void GroupTab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string groupId)
            _viewModel?.SelectGroupCommand.Execute(groupId);
    }

    private void GroupTab_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ProjectGroup group })
            _ = OpenGroupDialogAsync(group);
    }

    private async void GroupTabRename_Click(object sender, RoutedEventArgs e)
    {
        var group = GetGroupFromMenuFlyoutItem(sender);
        if (group is not null)
            await OpenGroupDialogAsync(group);
    }

    private async void GroupTabDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;
        var group = GetGroupFromMenuFlyoutItem(sender);
        if (group is null) return;

        var confirmed = await DialogService.ShowConfirmAsync(
            string.Format(LocalizationService.Get("DeleteGroupConfirmFormat"), group.Name, Environment.NewLine),
            LocalizationService.Get("DeleteGroupTitle"));

        if (confirmed)
        {
            _viewModel.DeleteGroup(group.Id);
            AllGroupTab.IsChecked = true;
        }
    }

    private static ProjectGroup? GetGroupFromMenuFlyoutItem(object sender)
    {
        if (sender is MenuFlyoutItem { DataContext: ProjectGroup group })
            return group;
        return null;
    }

    private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        => _ = OpenGroupDialogAsync(null);

    private async Task OpenGroupDialogAsync(ProjectGroup? existing)
    {
        if (_viewModel is null) return;

        var dialog = new GroupDialog(_viewModel.GetGroups(), existing);
        await dialog.ShowAsync();
        if (dialog.ResultGroup is not null)
            _viewModel.AddOrUpdateGroup(dialog.ResultGroup);
    }

    // ─── 앱 설정 ────────────────────────────────────────────────────────

    private async void AppSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;

        var dialog = new AppSettingsDialog(_viewModel.GetSettings());
        await dialog.ShowAsync();
        if (dialog.ResultSettings is not null)
            _viewModel.SaveAppSettings(dialog.ResultSettings);
    }

    // ─── 전체 작업 기록 ─────────────────────────────────────────────────

    private async void ProjectHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;

        var projects = _viewModel.GetAllProjectItemsWithHistories();
        var dialog = new ProjectHistoryDialog(projects, _viewModel.GetProjectRepository());
        await dialog.ShowAsync();
    }

    // ─── 프로젝트 추가/편집 ──────────────────────────────────────────────

    private void AddProjectButton_Click(object sender, RoutedEventArgs e)
        => _ = OpenProjectSettingsDialogAsync(null);

    private void OnAddProjectRequested(object? sender, EventArgs e)
        => _ = OpenProjectSettingsDialogAsync(null);

    private void OnEditProjectRequested(object? sender, ProjectCardViewModel card)
        => _ = OpenProjectSettingsDialogAsync(card);

    private async Task OpenProjectSettingsDialogAsync(ProjectCardViewModel? card)
    {
        if (_viewModel is null) return;

        var existingNames = _viewModel.GetProjectNames();
        var dialog = new ProjectSettingsDialog(
            _viewModel.GetGroups(), _viewModel.GetTools(), existingNames,
            _viewModel.GetSettings(), card?.ToModel(),
            card is null ? _viewModel.SelectedGroupId : null);
        await dialog.ShowAsync();
        if (dialog.ResultItem is not null)
            _viewModel.AddOrUpdateProject(dialog.ResultItem);
    }
}
