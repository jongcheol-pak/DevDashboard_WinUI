using System.Text.RegularExpressions;
using DevDashboard.Infrastructure.Persistence;
using Microsoft.Windows.AppLifecycle;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using DevDashboard.Presentation.Views;
using DevDashboard.Presentation.Views.Dialogs;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace DevDashboard;

public sealed partial class MainWindow : WindowEx
{
    private MainViewModel? _viewModel;
    private LauncherViewModel? _launcherViewModel;
    private readonly AppSettings _settings;
    private readonly JsonStorageService _storageService;
    private readonly IProjectRepository? _projectRepository;
    private readonly LauncherRepository? _launcherRepository;
    private readonly string? _dbErrorMessage;
    private SizeChangedEventHandler? _headerBorderSizeChanged;
    private SizeChangedEventHandler? _groupTabsPanelSizeChanged;

    // 런처 호버 애니메이션 캐싱
    private CompositionEasingFunction? _launcherEasing;
    private static readonly TimeSpan LauncherAnimDuration = TimeSpan.FromMilliseconds(200);
    private const int LauncherAnimRange = 3;
    private readonly HashSet<UIElement> _translationEnabledElements = [];
    private readonly HashSet<UIElement> _animatedLauncherItems = [];

    // 검색 디바운스
    private DispatcherQueueTimer? _searchDebounceTimer;
    private string _pendingSearchText = string.Empty;

    // 런처 PropertyChanged 핸들러 (해제를 위해 필드에 저장)
    private System.ComponentModel.PropertyChangedEventHandler? _launcherPropertyChanged;

    // SortOrder 상수 — XAML x:Bind CommandParameter용
    public SortOrder SortByName { get; } = SortOrder.Name;
    public SortOrder SortByCategory { get; } = SortOrder.Category;
    public SortOrder SortByDate { get; } = SortOrder.CreatedAt;

    public MainWindow(AppSettings settings, JsonStorageService storageService,
        IProjectRepository? projectRepository, LauncherRepository? launcherRepository,
        string? dbErrorMessage = null)
    {
        _settings = settings;
        _storageService = storageService;
        _projectRepository = projectRepository;
        _launcherRepository = launcherRepository;
        _dbErrorMessage = dbErrorMessage;

        InitializeComponent();

        this.SetIcon("Assets/dashboard.ico");
        // 윈도우 타이틀을 로컬라이즈된 앱 이름으로 설정
        Title = LocalizationService.Get("AppDisplayName");

        // 타이틀 바 확장 설정 — SetDragRectangles로 드래그 영역 직접 관리
        ExtendsContentIntoTitleBar = true;

        // 초기 창 크기 및 위치 (DIP 단위 — DPI 스케일 자동 적용)
        this.SetWindowSize(1200, 800);
        this.CenterOnScreen();

        RootGrid.Loaded += OnRootGridLoaded;
        Closed += OnWindowClosed;
    }

    private async void OnRootGridLoaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= OnRootGridLoaded;

        try
        {
            // Run.Text는 x:Uid를 지원하지 않으므로 ResourceLoader로 직접 설정
            ProjectCountSuffixRun.Text = LocalizationService.Get("MainWindow_ProjectCountSuffix");
            LauncherCountSuffixRun.Text = LocalizationService.Get("MainWindow_LauncherCountSuffix");

            // [ToolTipService.ToolTip] x:Uid는 런타임 오류를 발생시키므로 코드비하인드로 설정
            ApplyToolTips();

            // 타이틀바 드래그 영역 초기 계산 및 창 크기 변경 시 재계산
            UpdateTitleBarDragRegion();
            _headerBorderSizeChanged = (_, _) => UpdateTitleBarDragRegion();
            HeaderBorder.SizeChanged += _headerBorderSizeChanged;

            // 그룹 탭 콘텐츠 크기 변경 시 스크롤 버튼 가시성 재계산
            _groupTabsPanelSizeChanged = (_, _) => UpdateGroupScrollButtonVisibility();
            GroupTabsPanel.SizeChanged += _groupTabsPanelSizeChanged;

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

            // 런처 사이드바 초기화
            _launcherViewModel = new LauncherViewModel(_launcherRepository);
            LauncherRepeater.ItemsSource = _launcherViewModel.DisplayItems;
            LauncherAddButton.DataContext = _launcherViewModel;
            ApplyLauncherSidebarVisibility();
            _launcherPropertyChanged = (_, args) =>
            {
                if (args.PropertyName == nameof(LauncherViewModel.ItemCount))
                    UpdateLauncherCount();
            };
            _launcherViewModel.PropertyChanged += _launcherPropertyChanged;
            UpdateLauncherCount();

            // 비동기 초기화
            await _viewModel.InitializeAsync();

            // UI 준비 완료 후 Low 우선순위로 버전 체크 실행 (StoreContext 초기화 지연)
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
                () => _ = _viewModel.CheckLatestVersionAsync());

            // 저장된 그룹 탭 선택 복원 (ItemsRepeater 레이아웃 완료 후 실행)
            DispatcherQueue.TryEnqueue(RestoreGroupTabSelection);
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
        }
    }

    /// <summary>앱 재시작 시 저장된 그룹 탭 선택을 RadioButton 시각 상태에 복원합니다.</summary>
    private void RestoreGroupTabSelection()
    {
        if (_viewModel is null) return;
        var selectedGroupId = _viewModel.SelectedGroupId;
        if (selectedGroupId is null) return;

        var groups = _viewModel.Groups;
        for (var i = 0; i < groups.Count; i++)
        {
            if (groups[i].Id != selectedGroupId) continue;
            if (GroupTabsRepeater.TryGetElement(i) is RadioButton rb)
                rb.IsChecked = true;
            break;
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (_headerBorderSizeChanged is not null)
            HeaderBorder.SizeChanged -= _headerBorderSizeChanged;
        if (_groupTabsPanelSizeChanged is not null)
            GroupTabsPanel.SizeChanged -= _groupTabsPanelSizeChanged;

        if (_viewModel is not null)
        {
            _viewModel.EditProjectRequested -= OnEditProjectRequested;
            _viewModel.AddProjectRequested -= OnAddProjectRequested;
        }

        if (_searchDebounceTimer is not null)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Tick -= SearchDebounceTimer_Tick;
        }

        if (_launcherViewModel is not null && _launcherPropertyChanged is not null)
            _launcherViewModel.PropertyChanged -= _launcherPropertyChanged;

        _translationEnabledElements.Clear();
        _animatedLauncherItems.Clear();
        _launcherEasing = null;
    }

    /// <summary>SearchBox·버튼 영역을 제외한 헤더 빈 공간만 드래그 영역으로 등록합니다.
    /// 창 크기 변경 시 재호출되어 좌표를 갱신합니다.</summary>
    private void UpdateTitleBarDragRegion()
    {
        if (AppWindow?.TitleBar is null || RootGrid.XamlRoot is null) return;
        if (SearchBox.ActualWidth == 0 || HeaderButtonsPanel.ActualWidth == 0) return;

        try
        {

        var scale = (float)RootGrid.XamlRoot.RasterizationScale;
        var headerHeight = (int)(48 * scale);

        // RootGrid 기준 DIP 좌표 → 물리 픽셀 변환
        var searchOrigin = SearchBox.TransformToVisual(RootGrid)
            .TransformPoint(new Windows.Foundation.Point(0, 0));
        var buttonsOrigin = HeaderButtonsPanel.TransformToVisual(RootGrid)
            .TransformPoint(new Windows.Foundation.Point(0, 0));

        int searchLeft  = (int)(searchOrigin.X * scale);
        int searchRight = (int)((searchOrigin.X + SearchBox.ActualWidth) * scale);
        int buttonsLeft = (int)(buttonsOrigin.X * scale);
        int buttonsRight = (int)((buttonsOrigin.X + HeaderButtonsPanel.ActualWidth) * scale);

        var rects = new List<Windows.Graphics.RectInt32>();

        // 헤더 좌측 ~ SearchBox 시작 전 (AppTitleBar + 좌측 여백)
        if (searchLeft > 0)
            rects.Add(new Windows.Graphics.RectInt32(0, 0, searchLeft, headerHeight));

        // SearchBox 끝 ~ 버튼 StackPanel 시작 전 빈 공간
        if (buttonsLeft > searchRight)
            rects.Add(new Windows.Graphics.RectInt32(searchRight, 0, buttonsLeft - searchRight, headerHeight));

        // 버튼 StackPanel 끝 ~ 창 컨트롤 버튼(닫기/최대화/최소화) 좌측 사이 빈 공간
        int rightInset = AppWindow.TitleBar.RightInset;
        int windowWidth = AppWindow.Size.Width;
        if (windowWidth - rightInset > buttonsRight)
            rects.Add(new Windows.Graphics.RectInt32(buttonsRight, 0, windowWidth - rightInset - buttonsRight, headerHeight));

        // 창 컨트롤 버튼 아래 빈 공간 (버튼 높이가 헤더보다 낮을 때 생기는 영역)
        int captionButtonHeight = AppWindow.TitleBar.Height;
        if (headerHeight > captionButtonHeight && rightInset > 0)
            rects.Add(new Windows.Graphics.RectInt32(windowWidth - rightInset, captionButtonHeight, rightInset, headerHeight - captionButtonHeight));

        AppWindow.TitleBar.SetDragRectangles([.. rects]);
        }
        catch (ArgumentException)
        {
            // 레이아웃 전환 중 TransformToVisual 좌표 계산 실패 — 다음 SizeChanged에서 재시도
        }
    }

    /// <summary>[ToolTipService.ToolTip] x:Uid는 런타임 XamlParseException을 발생시키므로
    /// 코드비하인드에서 LocalizationService로 직접 설정합니다.</summary>
    private void ApplyToolTips()
    {
        ToolTipService.SetToolTip(SortButton, LocalizationService.Get("ToolTip_Sort"));
        ToolTipService.SetToolTip(AllHistoryButton, LocalizationService.Get("ToolTip_AllHistory"));
        ToolTipService.SetToolTip(ExportImportButton, LocalizationService.Get("ToolTip_ExportImport"));
        ToolTipService.SetToolTip(AppSettingsButton, LocalizationService.Get("ToolTip_AppSettings"));
        ToolTipService.SetToolTip(AddGroupButton, LocalizationService.Get("ToolTip_AddGroup"));
        ToolTipService.SetToolTip(RefreshButton, LocalizationService.Get("ToolTip_Refresh"));
    }

    // ─── 검색창 ─────────────────────────────────────────────────────────

    private static readonly Regex _searchSanitizePattern =
        new(@"['\"";\\\-\-\/\*=<>\x00]", RegexOptions.Compiled);

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_viewModel is null) return;
        // SuggestionChosen만 무시 — IME 조합(ProgrammaticChange)도 검색에 반영
        if (args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen) return;

        var raw = sender.Text ?? string.Empty;
        var filtered = _searchSanitizePattern.Replace(raw, string.Empty);

        // 정제된 텍스트가 다를 경우 표시 텍스트만 교정
        if (filtered != raw)
            sender.Text = filtered;

        // 디바운스: 300ms 이내 연속 입력 시 마지막 입력만 반영
        _pendingSearchText = filtered;
        _searchDebounceTimer?.Stop();
        if (_searchDebounceTimer is null)
        {
            _searchDebounceTimer = DispatcherQueue.CreateTimer();
            _searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(300);
            _searchDebounceTimer.IsRepeating = false;
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
        }
        _searchDebounceTimer.Start();
    }

    private void SearchDebounceTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        if (_viewModel is not null)
            _viewModel.SearchText = _pendingSearchText;
    }


    // ─── 그룹 탭 스크롤 ──────────────────────────────────────────────────

    /// <summary>1회 클릭당 스크롤 이동량 (DIP)</summary>
    private const double GroupScrollStep = 120;

    private void GroupScrollLeft_Click(object sender, RoutedEventArgs e)
    {
        GroupTabsScrollViewer.ChangeView(
            Math.Max(0, GroupTabsScrollViewer.HorizontalOffset - GroupScrollStep), null, null);
    }

    private void GroupScrollRight_Click(object sender, RoutedEventArgs e)
    {
        GroupTabsScrollViewer.ChangeView(
            GroupTabsScrollViewer.HorizontalOffset + GroupScrollStep, null, null);
    }

    private void GroupTabsScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        UpdateGroupScrollButtonVisibility();
    }

    /// <summary>스크롤 위치에 따라 좌/우 화살표 버튼 가시성을 업데이트합니다.</summary>
    private void UpdateGroupScrollButtonVisibility()
    {
        try
        {
            var canScrollLeft = GroupTabsScrollViewer.HorizontalOffset > 0;
            var canScrollRight = GroupTabsScrollViewer.HorizontalOffset
                < GroupTabsScrollViewer.ScrollableWidth - 1;

            GroupScrollLeftButton.Visibility = canScrollLeft
                ? Visibility.Visible : Visibility.Collapsed;
            GroupScrollRightButton.Visibility = canScrollRight
                ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (ArgumentException)
        {
            // 레이아웃 전환 중 시각적 트리 미연결 — 무시
        }
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
        try
        {
            var group = GetGroupFromMenuFlyoutItem(sender);
            if (group is not null)
                await OpenGroupDialogAsync(group);
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
        }
    }

    private async void GroupTabDelete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel is null) return;
            var group = GetGroupFromMenuFlyoutItem(sender);
            if (group is null) return;

            var confirmed = await DialogService.ShowConfirmAsync(
                string.Format(LocalizationService.Get("DeleteGroupConfirmFormat"), group.Name, Environment.NewLine),
                LocalizationService.Get("DeleteGroupTitle"));

            if (confirmed)
            {
                await _viewModel.DeleteGroupAsync(group.Id);
                AllGroupTab.IsChecked = true;
            }
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
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
        try
        {
            if (_viewModel is null) return;

            var dialog = new GroupDialog(_viewModel.GetGroups(), existing);
            await dialog.ShowAsync();
            if (dialog.ResultGroup is not null)
            {
                await _viewModel.AddOrUpdateGroupAsync(dialog.ResultGroup);

                // 새 그룹 추가 시 스크롤을 오른쪽 끝으로 이동하여 새 탭이 보이도록 함
                if (existing is null)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        GroupTabsScrollViewer.UpdateLayout();
                        GroupTabsScrollViewer.ChangeView(
                            GroupTabsScrollViewer.ScrollableWidth, null, null);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
        }
    }

    // ─── 앱 설정 ────────────────────────────────────────────────────────

    private async void AppSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel is null) return;

            var dialog = new AppSettingsDialog(_viewModel.GetSettings());
            await dialog.ShowAsync();
            if (dialog.ResultSettings is not null)
            {
                _viewModel.SaveAppSettings(dialog.ResultSettings);
                ApplyLauncherSidebarVisibility();
            }
            // ProjectsReset을 SettingsReset보다 먼저 처리:
            // ResetGroups()는 _allCards를 순회하며 DB Update를 호출하므로,
            // DB가 삭제된 상태에서 실행하면 FK 제약 조건 위반 발생
            if (dialog.ProjectsReset)
                await _viewModel.HardRefreshCommand.ExecuteAsync(null);
            if (dialog.SettingsReset)
            {
                await _viewModel.ResetGroupsAsync();
                AllGroupTab.IsChecked = true;
            }
            if (dialog.LauncherReset)
                _launcherViewModel?.Clear();
            if (dialog.LanguageChanged)
                await HandleLanguageChangedAsync();
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
        }
    }

    // ─── 전체 작업 기록 ─────────────────────────────────────────────────

    /// <summary>언어 변경 후 재시작 여부를 사용자에게 확인하고 처리합니다.</summary>
    private async Task HandleLanguageChangedAsync()
    {
        var confirmDialog = new ContentDialog
        {
            Title             = LocalizationService.Get("LanguageRestart_Title"),
            Content           = LocalizationService.Get("LanguageRestart_Message"),
            PrimaryButtonText = LocalizationService.Get("Dialog_Yes"),
            CloseButtonText   = LocalizationService.Get("Dialog_No"),
            DefaultButton     = ContentDialogButton.Primary,
            XamlRoot          = Content.XamlRoot
        };

        if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            AppInstance.Restart(string.Empty);
        else
            ReloadLanguageUI();  // 거부 시 코드비하인드 텍스트만 부분 갱신
    }

    /// <summary>언어 변경 후 UI를 새 언어로 재생성합니다.</summary>
    private void ReloadLanguageUI()
    {
        if (_viewModel is null) return;

        ProjectCountSuffixRun.Text = LocalizationService.Get("MainWindow_ProjectCountSuffix");
        LauncherCountSuffixRun.Text = LocalizationService.Get("MainWindow_LauncherCountSuffix");
        ApplyToolTips();
        DashboardContent.Content = new DashboardView { DataContext = _viewModel };
    }

    private void UpdateLauncherCount()
    {
        LauncherCountRun.Text = (_launcherViewModel?.ItemCount ?? 0).ToString();
    }

    private void ApplyLauncherSidebarVisibility()
    {
        var settings = _viewModel?.GetSettings() ?? _settings;
        LauncherSidebar.Visibility = settings.ShowLauncherSidebar
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private async void ProjectHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel is null) return;

            var projects = _viewModel.GetAllProjectItemsWithHistories();
            var dialog = new ProjectHistoryDialog(projects, _viewModel.GetProjectRepository());
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
        }
    }

    // ─── 내보내기 / 가져오기 ─────────────────────────────────────────────

    private void ExportImportFlyout_Opening(object? sender, object e)
    {
        bool hasProjects = _viewModel?.HasAnyProjects == true;
        bool hasLauncherItems = _launcherViewModel?.Items.Count > 0;
        ExportMenuItem.IsEnabled = hasProjects || hasLauncherItems;
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = "projects";
            picker.FileTypeChoices.Add("Database", [".db"]);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file is null) return;

            try
            {
                // VACUUM INTO: WAL 포함 완전한 스냅샷을 단일 파일로 내보내기
                await Task.Run(() => DatabaseContext.ExportTo(file.Path));

                await DialogService.ShowErrorAsync(
                    LocalizationService.Get("Export_Success"),
                    LocalizationService.Get("Export_SuccessTitle"));
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync(
                    string.Format(LocalizationService.Get("Export_Failed"), ex.Message),
                    LocalizationService.Get("Export_FailedTitle"));
            }
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel is null || _projectRepository is null) return;

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".db");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            List<ProjectItem> importProjects;
            List<ProjectGroup> importGroups;
            List<LauncherItem> importLauncherItems;
            try
            {
                // DB 읽기를 백그라운드에서 실행 (UI 프리징 방지)
                (importProjects, importGroups, importLauncherItems) = await Task.Run(() =>
                (
                    SqliteProjectRepository.ReadAllFromDb(file.Path),
                    SqliteProjectRepository.ReadGroupsFromDb(file.Path),
                    LauncherRepository.ReadAllFromDb(file.Path)
                ));
            }
            catch
            {
                await DialogService.ShowErrorAsync(
                    LocalizationService.Get("Import_InvalidFile"),
                    LocalizationService.Get("Import_InvalidFileTitle"));
                return;
            }

            var totalCount = 0;
            var addedCount = 0;
            var overwrittenCount = 0;
            var skippedCount = 0;
            var groupsCreatedCount = 0;

            // ─── 1. 프로젝트 목록 가져오기 ───────────────────────────────────
            if (importProjects.Count > 0)
            {
                var addProjects = await DialogService.ShowConfirmAsync(
                    LocalizationService.Get("Import_ConfirmProjects"),
                    LocalizationService.Get("Import_Title"));

                if (addProjects)
                {
                    // 그룹 이름 기준 매핑 (최대 10개 제한 준수)
                    var existingGroups = _viewModel.GetGroups();
                    var existingByName = existingGroups.ToDictionary(g => g.Name, g => g.Id, StringComparer.OrdinalIgnoreCase);
                    var groupIdRemap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var group in importGroups)
                    {
                        if (existingByName.TryGetValue(group.Name, out var existingId))
                        {
                            groupIdRemap[group.Id] = existingId;
                        }
                        else if (_viewModel.CanAddGroup)
                        {
                            await _viewModel.AddOrUpdateGroupAsync(group);
                            existingByName[group.Name] = group.Id;
                            groupIdRemap[group.Id] = group.Id;
                            groupsCreatedCount++;
                        }
                    }

                    // 프로젝트 초기화 후 가져오기 시 DB Groups 테이블이 비어 있을 수 있으므로
                    // 메모리의 그룹 목록을 DB에 동기화
                    await _viewModel.SyncGroupsToDbAsync();

                    foreach (var project in importProjects)
                    {
                        if (!string.IsNullOrEmpty(project.GroupId))
                            project.GroupId = groupIdRemap.TryGetValue(project.GroupId, out var remapped)
                                ? remapped
                                : string.Empty;
                    }

                    totalCount = importProjects.Count;

                    // 중복 체크를 백그라운드에서 일괄 수행 (UI 프리징 방지)
                    var existingIdMap = await Task.Run(() =>
                        importProjects.ToDictionary(p => p, p => _projectRepository.FindProjectIdByName(p.Name)));

                    foreach (var project in importProjects)
                    {
                        var existingId = existingIdMap[project];
                        if (existingId is not null)
                        {
                            var result = await ShowOverwriteDialogAsync(project.Name);
                            if (result == ContentDialogResult.Primary)
                            {
                                await Task.Run(() => _projectRepository.DeleteByNameAndInsert(existingId, project));
                                overwrittenCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        else
                        {
                            await Task.Run(() => _projectRepository.Add(project));
                            addedCount++;
                        }
                    }
                }
            }

            // ─── 2. 런처 항목 가져오기 ───────────────────────────────────────
            var launcherAddedCount = 0;
            var launcherTotalCount = 0;
            var launcherFilteredCount = 0;
            var launcherDuplicateCount = 0;
            if (_launcherViewModel is not null && importLauncherItems.Count > 0)
            {
                var addLauncher = await DialogService.ShowConfirmAsync(
                    LocalizationService.Get("Import_ConfirmLauncher"),
                    LocalizationService.Get("Import_Title"));

                if (addLauncher)
                {
                    launcherTotalCount = importLauncherItems.Count;

                    // 유효한 항목 필터링 (미설치 앱/파일 없는 항목 제외, 백그라운드 실행)
                    var validItems = await Task.Run(() => importLauncherItems.Where(item =>
                    {
                        if (string.IsNullOrEmpty(item.ExecutablePath)) return false;

                        // shell:AppsFolder(UWP/MSIX) 앱은 무조건 수용 — 실행 시점에 설치 여부 확인
                        if (item.ExecutablePath.StartsWith("shell:AppsFolder", StringComparison.OrdinalIgnoreCase))
                            return true;

                        return File.Exists(item.ExecutablePath);
                    }).ToList());

                    launcherFilteredCount = launcherTotalCount - validItems.Count;

                    // 아이콘 캐시가 없는 항목만 병렬 추출 (최대 4개 동시)
                    var needIcon = validItems
                        .Where(i => string.IsNullOrEmpty(i.IconCachePath) || !File.Exists(i.IconCachePath))
                        .ToList();
                    if (needIcon.Count > 0)
                    {
                        await Parallel.ForEachAsync(needIcon,
                            new ParallelOptions { MaxDegreeOfParallelism = 4 },
                            async (item, _) =>
                            {
                                var iconPath = await IconCacheService.GetIconPathAsync(item.ExecutablePath);
                                item.IconCachePath = iconPath ?? string.Empty;
                            });
                    }

                    // DB 추가 및 UI 업데이트 순차 처리
                    foreach (var item in validItems)
                    {
                        if (!_launcherViewModel.CanAdd) break;
                        item.SortOrder = _launcherViewModel.Items.Count;
                        if (await _launcherViewModel.AddItemAsync(item))
                            launcherAddedCount++;
                        else
                            launcherDuplicateCount++;
                    }
                }
            }

            // ─── 3. 결과 메시지 ──────────────────────────────────────────────
            var messageParts = new List<string>();

            if (totalCount > 0)
                messageParts.Add(string.Format(LocalizationService.Get("Import_ProjectResultFormat"),
                    totalCount, addedCount, overwrittenCount, skippedCount));

            if (groupsCreatedCount > 0)
                messageParts.Add(string.Format(LocalizationService.Get("Import_GroupsCreatedFormat"),
                    string.Empty, groupsCreatedCount));

            if (launcherTotalCount > 0)
                messageParts.Add(string.Format(LocalizationService.Get("Import_LauncherResultFormat"),
                    launcherTotalCount, launcherAddedCount, launcherFilteredCount, launcherDuplicateCount));

            if (messageParts.Count > 0)
            {
                var message = string.Join(Environment.NewLine, messageParts);
                await DialogService.ShowErrorAsync(message, LocalizationService.Get("Import_Title"));
            }

            // 목록 새로고침
            if (totalCount > 0)
                await _viewModel.HardRefreshCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
        }
    }

    private static async Task<ContentDialogResult> ShowOverwriteDialogAsync(string projectName)
    {
        var xamlRoot = App.MainWindow?.Content?.XamlRoot;
        if (xamlRoot is null) return ContentDialogResult.Secondary;

        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("Import_DuplicateTitle"),
            Content = string.Format(LocalizationService.Get("Import_DuplicateFormat"), projectName),
            PrimaryButtonText = LocalizationService.Get("Import_Overwrite"),
            CloseButtonText = LocalizationService.Get("Import_Skip"),
            XamlRoot = xamlRoot
        };
        return await dialog.ShowAsync();
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
        try
        {
            if (_viewModel is null) return;

            var existingNames = _viewModel.GetProjectNames();
            var dialog = new ProjectSettingsDialog(
                _viewModel.GetGroups(), _viewModel.GetTools(), existingNames,
                _viewModel.GetSettings(), card?.ToModel(),
                card is null ? _viewModel.SelectedGroupId : null);
            await dialog.ShowAsync();
            if (dialog.ResultItem is not null)
                await _viewModel.AddOrUpdateProjectAsync(dialog.ResultItem);
        }
        catch (Exception ex)
        {
            await ShowUnexpectedErrorAsync(ex);
        }
    }

    private static async Task ShowUnexpectedErrorAsync(Exception ex)
    {
        await DialogService.ShowErrorAsync(
            string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
    }

    // ===== 런처 사이드바 이벤트 핸들러 =====

    private async void LauncherAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_launcherViewModel is null) return;
        await _launcherViewModel.AddCommand.ExecuteAsync(null);
    }

    private void LauncherAddButton_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Link;
            e.DragUIOverride.Caption = LocalizationService.Get("Launcher_DropToAdd");
        }
    }

    private async void LauncherAddButton_Drop(object sender, DragEventArgs e)
    {
        if (_launcherViewModel is null) return;
        if (!e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        foreach (var storageItem in items)
        {
            if (storageItem is not Windows.Storage.StorageFile file) continue;
            if (!_launcherViewModel.CanAdd) break;

            var filePath = file.Path;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

            // .lnk 바로가기인 경우 대상 경로로 resolve
            if (filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                var target = ShellIconNativeMethods.ResolveShortcutTarget(filePath);
                if (string.IsNullOrEmpty(target) || !File.Exists(target)) continue;
                filePath = target;
            }

            // 아이콘 추출
            var iconPath = await IconCacheService.GetIconPathAsync(filePath);

            var launcherItem = new LauncherItem
            {
                DisplayName = Path.GetFileNameWithoutExtension(filePath),
                ExecutablePath = filePath,
                IconCachePath = iconPath ?? string.Empty,
                SortOrder = _launcherViewModel.Items.Count
            };

            // AddItemAsync 내부에서 중복/100개 제한 체크
            await _launcherViewModel.AddItemAsync(launcherItem);
        }
    }

    private void LauncherItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not LauncherItemViewModel item) return;
        _launcherViewModel?.LaunchCommand.Execute(item);
    }

    // 런처 아이콘 호버 시 확대 + 인접 아이콘 밀어내기 애니메이션
    private void LauncherItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;

        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        // CenterPoint: 레이아웃 미완료 시 고정 크기(44x44) fallback
        var size = visual.Size;
        if (size.X == 0 || size.Y == 0) size = new Vector2(44, 44);
        visual.CenterPoint = new Vector3(size.X / 2, size.Y / 2, 0);

        var easing = GetOrCreateEasing(compositor);

        // 확대 애니메이션
        var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.InsertKeyFrame(1.0f, new Vector3(1.3f, 1.3f, 1.0f), easing);
        scaleAnim.Duration = LauncherAnimDuration;
        visual.StartAnimation("Scale", scaleAnim);

        // 호버된 아이콘 기준으로 인접 아이템만 밀어내기 (범위 제한으로 성능 최적화)
        var hoveredIndex = LauncherRepeater.GetElementIndex(element);
        if (hoveredIndex < 0) return;

        const float pushDistance = 8f;
        var itemCount = _launcherViewModel?.DisplayItems.Count ?? 0;
        var startIdx = Math.Max(0, hoveredIndex - LauncherAnimRange);
        var endIdx = Math.Min(itemCount - 1, hoveredIndex + LauncherAnimRange);

        _animatedLauncherItems.Clear();
        for (var i = startIdx; i <= endIdx; i++)
        {
            if (i == hoveredIndex) continue;
            if (LauncherRepeater.TryGetElement(i) is not UIElement neighbor) continue;

            EnsureTranslationEnabled(neighbor);
            _animatedLauncherItems.Add(neighbor);

            // 거리 기반 감소: 가까울수록 많이, 멀수록 적게 밀어냄
            var distance = Math.Abs(i - hoveredIndex);
            var magnitude = pushDistance / distance;
            var offset = i < hoveredIndex ? -magnitude : magnitude;
            AnimateLauncherTranslationY(neighbor, offset, easing);
        }

        // 추가 버튼도 거리 기반으로 밀어내기
        EnsureTranslationEnabled(LauncherAddButton);
        var addBtnDistance = Math.Max(1, itemCount - hoveredIndex);
        AnimateLauncherTranslationY(LauncherAddButton, pushDistance / addBtnDistance, easing);
    }

    private void LauncherItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;

        var visual = ElementCompositionPreview.GetElementVisual(element);
        var easing = GetOrCreateEasing(visual.Compositor);

        // 축소 애니메이션
        var scaleAnim = visual.Compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f), easing);
        scaleAnim.Duration = LauncherAnimDuration;
        visual.StartAnimation("Scale", scaleAnim);

        // PointerEntered에서 애니메이션된 아이콘만 원위치 복원
        foreach (var neighbor in _animatedLauncherItems)
        {
            AnimateLauncherTranslationY(neighbor, 0f, easing);
        }
        _animatedLauncherItems.Clear();

        // 추가 버튼도 원위치 복원
        AnimateLauncherTranslationY(LauncherAddButton, 0f, easing);
    }

    /// <summary>런처 호버 애니메이션용 easing 함수를 캐싱하여 반환합니다.</summary>
    private CompositionEasingFunction GetOrCreateEasing(Compositor compositor)
    {
        return _launcherEasing ??= compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0), new Vector2(0, 1));
    }

    /// <summary>Translation 애니메이션 사용을 위해 최초 1회만 활성화합니다.</summary>
    private void EnsureTranslationEnabled(UIElement element)
    {
        if (_translationEnabledElements.Add(element))
            ElementCompositionPreview.SetIsTranslationEnabled(element, true);
    }

    /// <summary>런처 아이콘의 Y축 Translation을 애니메이션합니다.</summary>
    private static void AnimateLauncherTranslationY(UIElement target, float y,
        CompositionEasingFunction easing)
    {
        var visual = ElementCompositionPreview.GetElementVisual(target);
        var anim = visual.Compositor.CreateVector3KeyFrameAnimation();
        anim.InsertKeyFrame(1.0f, new Vector3(0, y, 0), easing);
        anim.Duration = LauncherAnimDuration;
        visual.StartAnimation("Translation", anim);
    }

    private void LauncherRunAsAdmin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not LauncherItemViewModel item) return;
        _launcherViewModel?.LaunchAsAdminCommand.Execute(item);
    }

    private void LauncherDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not LauncherItemViewModel item) return;
        _launcherViewModel?.DeleteCommand.Execute(item);
    }

    // ===== 런처 사이드바 드래그&드롭 =====

    private readonly DropPlaceholder _launcherDropPlaceholder = new();
    private int _launcherDropPlaceholderIndex = -1;
    private string? _launcherDropTargetId;
    private bool _launcherDropIsAbove;
    private string? _draggedLauncherId;

    private void LauncherItem_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not LauncherItemViewModel item) return;

        _draggedLauncherId = item.Id;
        args.Data.SetText(item.Id);
        args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;

        if (!string.IsNullOrEmpty(item.IconCachePath) && File.Exists(item.IconCachePath))
        {
            var dragIcon = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(item.IconCachePath));
            args.DragUI.SetContentFromBitmapImage(dragIcon);
        }

        // 드래그 중인 아이콘을 반투명으로 표시
        sender.Opacity = 0.3;
    }

    private void LauncherItem_DropCompleted(UIElement sender, DropCompletedEventArgs args)
    {
        sender.Opacity = 1.0;
        RemoveLauncherDropPlaceholder();
        _draggedLauncherId = null;
    }

    private void LauncherItem_DragOver(object sender, DragEventArgs e)
    {
        if (_draggedLauncherId is null || _launcherViewModel is null) return;
        if (sender is not FrameworkElement fe || fe.DataContext is not LauncherItemViewModel target) return;
        if (target.Id == _draggedLauncherId) return;

        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        e.DragUIOverride.IsCaptionVisible = false;
        e.DragUIOverride.IsGlyphVisible = false;
        e.Handled = true;

        var position = e.GetPosition(fe);
        ShowLauncherDropPlaceholder(target, position);
    }

    private void LauncherItem_Drop(object sender, DragEventArgs e)
    {
        if (_draggedLauncherId is null || _launcherViewModel is null) return;
        if (sender is not FrameworkElement fe || fe.DataContext is not LauncherItemViewModel target) return;

        var draggedId = _draggedLauncherId;
        var targetId = target.Id;
        bool insertAfter = !_launcherDropIsAbove;

        RemoveLauncherDropPlaceholder();

        if (!string.IsNullOrEmpty(draggedId) && !string.IsNullOrEmpty(targetId) && draggedId != targetId)
            _launcherViewModel.MoveItem(draggedId, targetId, insertAfter);

        e.Handled = true;
    }

    private void LauncherPlaceholder_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        e.DragUIOverride.IsCaptionVisible = false;
        e.DragUIOverride.IsGlyphVisible = false;
        e.Handled = true;
    }

    private void LauncherPlaceholder_Drop(object sender, DragEventArgs e)
    {
        if (_draggedLauncherId is null || _launcherViewModel is null) return;

        var draggedId = _draggedLauncherId;
        var targetId = _launcherDropTargetId;
        bool insertAfter = !_launcherDropIsAbove;

        RemoveLauncherDropPlaceholder();

        if (!string.IsNullOrEmpty(draggedId) && !string.IsNullOrEmpty(targetId) && draggedId != targetId)
            _launcherViewModel.MoveItem(draggedId, targetId, insertAfter);

        e.Handled = true;
    }

    private void ShowLauncherDropPlaceholder(LauncherItemViewModel target, Windows.Foundation.Point posInTarget)
    {
        // 상반부(Y < 22) → 위에 삽입, 하반부 → 아래에 삽입
        bool isAbove = posInTarget.Y < 22;

        if (_launcherDropTargetId == target.Id && _launcherDropIsAbove == isAbove)
            return; // 동일 위치면 스킵

        RemoveLauncherDropPlaceholder();

        var displayItems = _launcherViewModel!.DisplayItems;
        int targetIndex = -1;
        for (int i = 0; i < displayItems.Count; i++)
        {
            if (displayItems[i] is LauncherItemViewModel vm && vm.Id == target.Id)
            {
                targetIndex = i;
                break;
            }
        }
        if (targetIndex < 0) return;

        int insertIndex = isAbove ? targetIndex : targetIndex + 1;
        displayItems.Insert(insertIndex, _launcherDropPlaceholder);

        _launcherDropPlaceholderIndex = insertIndex;
        _launcherDropTargetId = target.Id;
        _launcherDropIsAbove = isAbove;
    }

    private void RemoveLauncherDropPlaceholder()
    {
        if (_launcherDropPlaceholderIndex >= 0)
        {
            _launcherViewModel?.DisplayItems.Remove(_launcherDropPlaceholder);
            _launcherDropPlaceholderIndex = -1;
            _launcherDropTargetId = null;
        }
    }
}
