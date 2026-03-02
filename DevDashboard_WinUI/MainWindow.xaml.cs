using System.Text.RegularExpressions;
using DevDashboard.Infrastructure.Persistence;
using Microsoft.Windows.AppLifecycle;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using DevDashboard.Presentation.Views;
using DevDashboard.Presentation.Views.Dialogs;
using Microsoft.UI.Dispatching;
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

        // 윈도우 타이틀을 로컬라이즈된 앱 이름으로 설정
        Title = LocalizationService.Get("AppDisplayName");

        // 타이틀 바 확장 설정
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // 초기 창 크기 및 위치
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        this.CenterOnScreen();

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

        // UI 준비 완료 후 Low 우선순위로 버전 체크 실행 (StoreContext 초기화 지연)
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
            () => _ = _viewModel.CheckLatestVersionAsync());

        // 저장된 그룹 탭 선택 복원 (ItemsRepeater 레이아웃 완료 후 실행)
        DispatcherQueue.TryEnqueue(RestoreGroupTabSelection);
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
    private const int SearchMaxLength = 30;

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_viewModel is null) return;
        // SuggestionChosen만 무시 — IME 조합(ProgrammaticChange)도 검색에 반영
        if (args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen) return;

        var raw = sender.Text ?? string.Empty;
        var filtered = _searchSanitizePattern.Replace(raw, string.Empty);

        if (filtered.Length > SearchMaxLength)
            filtered = filtered[..SearchMaxLength];

        // 정제된 텍스트가 다를 경우 표시 텍스트만 교정하고, SearchText는 항상 업데이트
        if (filtered != raw)
            sender.Text = filtered;

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
        if (dialog.ProjectsReset)
            await _viewModel.HardRefreshCommand.ExecuteAsync(null);
        if (dialog.LanguageChanged)
            await HandleLanguageChangedAsync();
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
        ApplyToolTips();
        DashboardContent.Content = new DashboardView { DataContext = _viewModel };
    }

    private async void ProjectHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;

        var projects = _viewModel.GetAllProjectItemsWithHistories();
        var dialog = new ProjectHistoryDialog(projects, _viewModel.GetProjectRepository());
        await dialog.ShowAsync();
    }

    // ─── 내보내기 / 가져오기 ─────────────────────────────────────────────

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
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

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
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
        try
        {
            importProjects = SqliteProjectRepository.ReadAllFromDb(file.Path);
            importGroups   = SqliteProjectRepository.ReadGroupsFromDb(file.Path);
        }
        catch
        {
            await DialogService.ShowErrorAsync(
                LocalizationService.Get("Import_InvalidFile"),
                LocalizationService.Get("Import_InvalidFileTitle"));
            return;
        }

        // ─── 그룹 이름 기준 매핑 (최대 10개 제한 준수) ──────────────────────
        // importedGroupId → 실제 사용할 GroupId
        // 이름이 같은 그룹이 있으면 기존 Id로 리매핑, 없으면 신규 생성
        var existingGroups = _viewModel.GetGroups();
        var existingByName = existingGroups.ToDictionary(g => g.Name, g => g.Id, StringComparer.OrdinalIgnoreCase);
        var groupIdRemap   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var groupsCreatedCount = 0;

        foreach (var group in importGroups)
        {
            if (existingByName.TryGetValue(group.Name, out var existingId))
            {
                // 이름이 같은 그룹 존재 → 기존 Id로 리매핑
                groupIdRemap[group.Id] = existingId;
            }
            else if (_viewModel.CanAddGroup)
            {
                // 신규 그룹 생성 — 가져온 UUID 그대로 사용
                _viewModel.AddOrUpdateGroup(group);
                existingByName[group.Name] = group.Id;
                groupIdRemap[group.Id] = group.Id;
                groupsCreatedCount++;
            }
            // CanAddGroup == false: 10개 초과 → GroupId를 빈 문자열로 리매핑
        }

        // 가져올 프로젝트의 GroupId를 리매핑된 Id로 교체
        foreach (var project in importProjects)
        {
            if (!string.IsNullOrEmpty(project.GroupId))
                project.GroupId = groupIdRemap.TryGetValue(project.GroupId, out var remapped)
                    ? remapped
                    : string.Empty;
        }

        var totalCount = importProjects.Count;
        var addedCount = 0;
        var overwrittenCount = 0;
        var skippedCount = 0;

        foreach (var project in importProjects)
        {
            var existingId = _projectRepository.FindProjectIdByName(project.Name);
            if (existingId is not null)
            {
                var result = await ShowOverwriteDialogAsync(project.Name);
                if (result == ContentDialogResult.Primary)
                {
                    _projectRepository.DeleteByNameAndInsert(existingId, project);
                    overwrittenCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            else
            {
                _projectRepository.Add(project);
                addedCount++;
            }
        }

        var message = string.Format(LocalizationService.Get("Import_CompleteFormat"),
            Environment.NewLine, totalCount, addedCount, overwrittenCount, skippedCount);

        if (groupsCreatedCount > 0)
            message += string.Format(LocalizationService.Get("Import_GroupsCreatedFormat"),
                Environment.NewLine, groupsCreatedCount);

        await DialogService.ShowErrorAsync(message, LocalizationService.Get("Import_CompleteTitle"));

        // 목록 새로고침
        _viewModel.HardRefreshCommand.Execute(null);
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
