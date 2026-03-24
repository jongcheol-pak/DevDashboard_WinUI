using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Shared.Collections;
using DevDashboard.Infrastructure.Services;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>메인 창 뷰모델 — 그룹 탭, 검색, 정렬, 뷰모드, 카드 목록 관리</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly JsonStorageService _storageService;
    private readonly IProjectRepository _projectRepository;
    private AppSettings _settings;
    private bool _isInitialized;
    private bool _suppressReactiveUpdates;
    private IReadOnlyList<ExternalTool>? _toolsCache;

    [Conditional("DEBUG")]
    private static void PerfLog(string message)
        => Debug.WriteLine($"[Perf][MainViewModel] {message}");

    [ObservableProperty]
    public partial bool IsInitializing { get; set; } = true;

    // 전체 카드 원본 목록 (필터/정렬 전)
    private readonly List<ProjectCardViewModel> _allCards = [];

    [ObservableProperty]
    public partial BulkObservableCollection<ProjectCardViewModel> FilteredCards { get; set; } = [];

    /// <summary>UI에 바인딩되는 카드 목록 (FilteredCards + AddCardPlaceholder 마지막 항목)</summary>
    public BulkObservableCollection<object> DisplayCards { get; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProjectGroup> Groups { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedGroupId { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SortOrder CurrentSortOrder { get; set; } = SortOrder.Name;

    [ObservableProperty]
    public partial int ProjectCount { get; set; }

    /// <summary>프로젝트가 하나 이상 존재하는지 여부</summary>
    [ObservableProperty]
    public partial bool HasAnyProjects { get; set; }

    // --- 최신 버전 확인 ---

    [ObservableProperty]
    public partial bool HasNewVersion { get; set; }

    private string _latestReleaseUrl = string.Empty;

    /// <summary>MS Store를 통해 최신 버전을 확인합니다 (세션 내 캐시 적용).</summary>
    public async Task CheckLatestVersionAsync()
    {
        var result = await VersionCheckService.CheckLatestVersionAsync();
        if (result is not null)
        {
            _latestReleaseUrl = result.ReleaseUrl;
            HasNewVersion = true;
        }
    }

    [RelayCommand]
    private void OpenLatestRelease() => VersionCheckService.OpenUrl(_latestReleaseUrl);

    private readonly AddCardPlaceholder _addCardPlaceholder = new();

    /// <summary>편집 다이얼로그 열기 요청 이벤트 — View에서 처리</summary>
    public event EventHandler<ProjectCardViewModel>? EditProjectRequested;

    /// <summary>새 프로젝트 추가 요청 이벤트 — View에서 처리</summary>
    public event EventHandler? AddProjectRequested;

    public MainViewModel(AppSettings settings, JsonStorageService storageService, IProjectRepository projectRepository)
    {
        _storageService = storageService;
        _projectRepository = projectRepository;
        _settings = settings;
        ApplyInitialSettings();
        ApplyFilterAndSort();

        Groups.CollectionChanged += OnGroupsCollectionChanged;
    }

    private void OnGroupsCollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(CanAddGroup));

    private void ApplyInitialSettings()
    {
        _suppressReactiveUpdates = true;
        try
        {
            CurrentSortOrder = _settings.SortOrder;

            if (_settings.Groups.Count == 0)
                _settings.Groups.Add(new ProjectGroup { Name = LocalizationService.Get("DefaultGroupName"), IsDefault = true });

            Groups.Clear();
            foreach (var g in _settings.Groups)
                Groups.Add(g);

            SelectedGroupId = Groups.Any(g => g.Id == _settings.SelectedGroupId)
                ? _settings.SelectedGroupId
                : null;
        }
        finally
        {
            _suppressReactiveUpdates = false;
        }
    }

    /// <summary>초기 카드 로드를 비동기로 수행합니다.</summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        _isInitialized = true;
        IsInitializing = true;

        var initSw = Stopwatch.StartNew();

        var dbSw = Stopwatch.StartNew();
        var dbTask = Task.Run(() =>
        {
            try { return _projectRepository.GetAll(); }
            catch (Exception ex) { Debug.WriteLine($"[MainViewModel] DB 로드 실패: {ex.Message}"); return new List<ProjectItem>(); }
        });
        var toolTask = Task.Run(() => GetTools());
        await Task.WhenAll(dbTask, toolTask);
        var projects = dbTask.Result;
        var tools = toolTask.Result;
        dbSw.Stop();

        var vmSw = Stopwatch.StartNew();
        var cards = await Task.Run(() =>
        {
            var list = new List<ProjectCardViewModel>(projects.Count);
            foreach (var item in projects)
                list.Add(new ProjectCardViewModel(item, tools, _projectRepository, _settings));
            return list;
        });

        _allCards.Clear();
        foreach (var card in cards)
            AddCardInternal(card);
        vmSw.Stop();

        var bindSw = Stopwatch.StartNew();
        ApplyFilterAndSort();
        bindSw.Stop();

        // UI를 먼저 표시한 후 동시성 제한된 백그라운드 작업 시작
        IsInitializing = false;
        initSw.Stop();
        PerfLog($"Initialize: cards={projects.Count}, db+tools={dbSw.ElapsedMilliseconds}ms, vm={vmSw.ElapsedMilliseconds}ms, bind={bindSw.ElapsedMilliseconds}ms, total={initSw.ElapsedMilliseconds}ms");

        foreach (var card in _allCards)
        {
            card.StartIconLoad();
            card.StartGitStatusLoad();
            card.StartValidation();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_suppressReactiveUpdates) return;
        ApplyFilterAndSort();
    }

    partial void OnSelectedGroupIdChanged(string? value)
    {
        if (_suppressReactiveUpdates) return;
        ApplyFilterAndSort();
        SaveSettings();
    }

    partial void OnCurrentSortOrderChanged(SortOrder value)
    {
        if (_suppressReactiveUpdates) return;
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        var sw = Stopwatch.StartNew();
        IEnumerable<ProjectCardViewModel> query = _allCards;

        if (!string.IsNullOrEmpty(SelectedGroupId))
            query = query.Where(c => c.GroupId == SelectedGroupId);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = SearchText.Trim();
            query = query.Where(c =>
                c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        List<ProjectCardViewModel> pinned = [], unpinned = [];
        foreach (var c in query)
        {
            if (c.IsPinned) pinned.Add(c);
            else unpinned.Add(c);
        }

        IEnumerable<ProjectCardViewModel> sortedUnpinned = CurrentSortOrder switch
        {
            SortOrder.Name => unpinned.OrderBy(c => c.Name),
            SortOrder.Category => unpinned.OrderBy(c => c.Category).ThenBy(c => c.Name),
            SortOrder.CreatedAt => unpinned.OrderByDescending(c => c.CreatedAt),
            _ => unpinned
        };

        var sorted = pinned.Concat(sortedUnpinned).ToList();
        FilteredCards.ResetWith(sorted);
        ProjectCount = sorted.Count;
        HasAnyProjects = _allCards.Count > 0;
        DisplayCards.ResetWith(sorted.Cast<object>().Append(_addCardPlaceholder));

        sw.Stop();
        PerfLog($"ApplyFilterAndSort: all={_allCards.Count}, filtered={sorted.Count}, total={sw.ElapsedMilliseconds}ms");
    }

    private void SaveSettings()
    {
        _settings.Groups = [.. Groups];
        _settings.SortOrder = CurrentSortOrder;
        _settings.SelectedGroupId = SelectedGroupId;
        _storageService.Save(_settings);
    }

    private void AddCardInternal(ProjectCardViewModel card)
    {
        SubscribeToCard(card);
        _allCards.Add(card);
    }

    private void SubscribeToCard(ProjectCardViewModel card)
    {
        card.DeleteRequested += OnCardDeleteRequested;
        card.EditRequested += OnCardEditRequested;
        card.PinToggled += OnCardPinToggled;
        card.CommandScriptChanged += OnCommandScriptChanged;
        card.TodoChanged += OnTodoChanged;
        card.HistoryChanged += OnHistoryChanged;
        card.TestChanged += OnTestChanged;
    }

    private void UnsubscribeFromCard(ProjectCardViewModel card)
    {
        card.DeleteRequested -= OnCardDeleteRequested;
        card.EditRequested -= OnCardEditRequested;
        card.PinToggled -= OnCardPinToggled;
        card.CommandScriptChanged -= OnCommandScriptChanged;
        card.TodoChanged -= OnTodoChanged;
        card.HistoryChanged -= OnHistoryChanged;
        card.TestChanged -= OnTestChanged;
    }

    private static void ShowDbError(Exception ex)
    {
        _ = DialogService.ShowErrorAsync(ex.Message, LocalizationService.Get("SaveError"));
    }

    private async void OnCardDeleteRequested(object? sender, EventArgs e)
    {
        if (sender is not ProjectCardViewModel card) return;
        UnsubscribeFromCard(card);

        try
        {
            await Task.Run(() => _projectRepository.Delete(card.Id));
        }
        catch (Exception ex)
        {
            SubscribeToCard(card);
            ShowDbError(ex);
            return;
        }

        _allCards.Remove(card);
        ApplyFilterAndSort();
        SaveSettings();
    }

    private void OnCardEditRequested(object? sender, EventArgs e)
    {
        if (sender is not ProjectCardViewModel card) return;
        EditProjectRequested?.Invoke(this, card);
    }

    private async void OnCardPinToggled(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            try
            {
                await Task.Run(() => _projectRepository.UpdatePinned(card.Id, card.IsPinned));
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        ApplyFilterAndSort();
        SaveSettings();
    }

    private async void OnCommandScriptChanged(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            var model = card.ToModel();
            try
            {
                await Task.Run(() => _projectRepository.SaveCommandScripts(model.Id, model.CommandScripts));
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }
    }

    private async void OnTodoChanged(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            var model = card.ToModel();
            try
            {
                await Task.Run(() => _projectRepository.SaveTodos(model.Id, model.Todos));
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }
    }

    private async void OnHistoryChanged(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            var model = card.ToModel();
            try
            {
                await Task.Run(() => _projectRepository.SaveHistories(model.Id, model.Histories));
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }
    }

    private async void OnTestChanged(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            var model = card.ToModel();
            try
            {
                await Task.Run(() => _projectRepository.SaveTestCategories(model.Id, model.TestCategories));
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }
    }

    /// <summary>핀 고정된 카드의 순서를 변경합니다.</summary>
    public async void MovePinnedCard(string draggedCardId, string targetCardId, bool insertAfter = false)
    {
        var dragged = _allCards.FirstOrDefault(c => c.Id == draggedCardId);
        var target = _allCards.FirstOrDefault(c => c.Id == targetCardId);
        if (dragged is null || target is null) return;
        if (!dragged.IsPinned || !target.IsPinned) return;

        var fromIndex = _allCards.IndexOf(dragged);
        var toIndex = _allCards.IndexOf(target);
        if (fromIndex == toIndex) return;

        _allCards.RemoveAt(fromIndex);
        toIndex = _allCards.IndexOf(target);
        var finalIndex = insertAfter ? toIndex + 1 : toIndex;
        _allCards.Insert(finalIndex, dragged);

        var pinnedIds = _allCards
            .Where(c => c.IsPinned)
            .Select(c => c.Id)
            .ToList();
        try
        {
            await Task.Run(() => _projectRepository.UpdatePinOrder(pinnedIds));
        }
        catch (Exception ex)
        {
            ShowDbError(ex);
        }

        ApplyFilterAndSort();
        SaveSettings();
    }

    /// <summary>프로젝트를 추가하거나 수정합니다.</summary>
    public async Task AddOrUpdateProjectAsync(ProjectItem item)
    {
        var existing = _allCards.FirstOrDefault(c => c.Id == item.Id);
        if (existing is not null)
        {
            // ProjectSettingsDialog는 CommandScripts를 관리하지 않으므로 기존 값을 보존
            item.CommandScripts = existing.ToModel().CommandScripts;

            try
            {
                await Task.Run(() => _projectRepository.Update(item));
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
                return;
            }

            var idx = _allCards.IndexOf(existing);
            UnsubscribeFromCard(existing);

            var updated = new ProjectCardViewModel(item, GetTools(), _projectRepository, _settings);
            SubscribeToCard(updated);
            _allCards.Insert(idx, updated);
            _allCards.RemoveAt(idx + 1);
            updated.StartIconLoad();
            updated.StartGitStatusLoad();
        }
        else
        {
            try
            {
                await Task.Run(() => _projectRepository.Add(item));
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
                return;
            }

            var newCard = new ProjectCardViewModel(item, GetTools(), _projectRepository, _settings);
            AddCardInternal(newCard);
            newCard.StartIconLoad();
            newCard.StartGitStatusLoad();
        }

        ApplyFilterAndSort();
        SaveSettings();
    }

    /// <summary>현재 메모리의 그룹 목록을 DB에 동기화합니다 (가져오기 후 호출).</summary>
    public async Task SyncGroupsToDbAsync()
        => await Task.Run(() => _projectRepository.SyncGroups([.. Groups]));

    /// <summary>그룹을 추가하거나 수정합니다.</summary>
    public async Task AddOrUpdateGroupAsync(ProjectGroup group)
    {
        var existing = Groups.FirstOrDefault(g => g.Id == group.Id);
        if (existing is not null)
            Groups[Groups.IndexOf(existing)] = group;
        else
            Groups.Add(group);

        SaveSettings();
        await Task.Run(() => _projectRepository.SyncGroups([.. Groups]));
    }

    /// <summary>그룹을 삭제하고 해당 그룹에 속한 프로젝트의 GroupId를 초기화합니다.</summary>
    public async Task DeleteGroupAsync(string groupId)
    {
        var group = Groups.FirstOrDefault(g => g.Id == groupId);
        if (group is null || group.IsDefault) return;

        Groups.Remove(group);

        var affectedCards = _allCards.Where(c => c.GroupId == groupId).ToList();
        foreach (var card in affectedCards)
            card.UpdateGroupId(string.Empty);

        // 영향받는 카드 DB 업데이트 + 그룹 동기화를 백그라운드에서 일괄 처리
        var models = affectedCards.Select(c => c.ToModel()).ToList();
        var groups = Groups.ToList();
        try
        {
            await Task.Run(() =>
            {
                foreach (var model in models)
                    _projectRepository.Update(model);
                _projectRepository.SyncGroups(groups);
            });
        }
        catch (Exception ex)
        {
            ShowDbError(ex);
        }

        if (SelectedGroupId == groupId)
            SelectedGroupId = null;

        ApplyFilterAndSort();
        SaveSettings();
    }

    /// <summary>사용자 추가 그룹 수 (기본 그룹 제외)</summary>
    public int UserGroupCount => Groups.Count(g => !g.IsDefault);

    /// <summary>그룹 추가 가능 여부 (사용자 그룹 최대 10개)</summary>
    public bool CanAddGroup => UserGroupCount < 10;

    [RelayCommand]
    private void RequestAddProject() => AddProjectRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void SetSortOrder(SortOrder order)
    {
        CurrentSortOrder = order;
        SaveSettings();
    }

    [RelayCommand]
    private void SelectGroup(string? groupId) => SelectedGroupId = groupId;

    /// <summary>카드 목록을 새로고침합니다.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        var refreshSw = Stopwatch.StartNew();
        IsInitializing = true;

        try
        {
            var models = _allCards.Select(c => c.ToModel()).ToList();

            foreach (var card in _allCards)
                UnsubscribeFromCard(card);
            _allCards.Clear();

            DevToolDetector.InvalidateCache();
            InvalidateToolsCache();
            ProjectCardViewModel.ClearIconCache();
            var tools = await Task.Run(() => GetTools());

            var cards = await Task.Run(() =>
            {
                var list = new List<ProjectCardViewModel>(models.Count);
                foreach (var model in models)
                    list.Add(new ProjectCardViewModel(model, tools, _projectRepository, _settings));
                return list;
            });

            foreach (var card in cards)
                AddCardInternal(card);

            ApplyFilterAndSort();
            refreshSw.Stop();
            PerfLog($"Refresh: cards={models.Count}, total={refreshSw.ElapsedMilliseconds}ms");

            // UI를 먼저 표시한 후 동시성 제한된 백그라운드 작업 시작
            IsInitializing = false;
            foreach (var card in _allCards)
            {
                card.StartIconLoad();
                card.StartGitStatusLoad();
                card.StartValidation();
            }
        }
        finally
        {
            // 예외 발생 시 IsInitializing이 true로 남지 않도록 보장
            IsInitializing = false;
        }
    }

    [RelayCommand]
    private async Task HardRefresh()
    {
        var hardSw = Stopwatch.StartNew();
        IsInitializing = true;

        try
        {
            DevToolDetector.InvalidateCache();
            InvalidateToolsCache();
            ProjectCardViewModel.ClearIconCache();

            var dbTask = Task.Run(() =>
            {
                try { return _projectRepository.GetAll(); }
                catch (Exception ex) { Debug.WriteLine($"[MainViewModel] HardRefresh DB 로드 실패: {ex.Message}"); return new List<ProjectItem>(); }
            });
            var toolTask = Task.Run(() => GetTools());
            await Task.WhenAll(dbTask, toolTask);
            var projects = dbTask.Result;
            var tools = toolTask.Result;

            foreach (var card in _allCards)
                UnsubscribeFromCard(card);
            _allCards.Clear();

            var cards = await Task.Run(() =>
            {
                var list = new List<ProjectCardViewModel>(projects.Count);
                foreach (var item in projects)
                    list.Add(new ProjectCardViewModel(item, tools, _projectRepository, _settings));
                return list;
            });

            foreach (var card in cards)
                AddCardInternal(card);

            ApplyFilterAndSort();
            hardSw.Stop();
            PerfLog($"HardRefresh: cards={projects.Count}, total={hardSw.ElapsedMilliseconds}ms");

            // UI를 먼저 표시한 후 동시성 제한된 백그라운드 작업 시작
            IsInitializing = false;
            foreach (var card in _allCards)
            {
                card.StartIconLoad();
                card.StartGitStatusLoad();
                card.StartValidation();
            }
        }
        finally
        {
            // 예외 발생 시 IsInitializing이 true로 남지 않도록 보장
            IsInitializing = false;
        }
    }

    public IReadOnlyList<ProjectGroup> GetGroups() => [.. Groups];

    public List<ProjectItem> GetAllProjectItems() => _allCards.Select(c => c.ToModel()).ToList();

    /// <summary>ProjectHistoryDialog용 — 모든 프로젝트의 Histories를 DB에서 로드하여 반환합니다.</summary>
    public List<ProjectItem> GetAllProjectItemsWithHistories()
    {
        var items = _allCards.Select(c => c.ToModel()).ToList();
        foreach (var item in items)
            item.Histories = _projectRepository.GetHistories(item.Id);
        return items;
    }

    public IReadOnlyList<string> GetProjectNames() => _allCards.Select(c => c.Name).ToList();

    private void InvalidateToolsCache() => _toolsCache = null;

    /// <summary>셸 도구 + 자동 감지 도구 + AppSettings 사용자 도구를 병합하여 반환합니다.</summary>
    public IReadOnlyList<ExternalTool> GetTools()
    {
        if (_toolsCache is not null)
            return _toolsCache;

        var all = new List<ExternalTool>
        {
            new() { Name = ProjectSettingsDialogViewModel.PowerShellToolName, ExecutablePath = "powershell.exe" },
            new() { Name = ProjectSettingsDialogViewModel.CmdToolName, ExecutablePath = "cmd.exe" }
        };

        var detected = DevToolDetector.DetectInstalledTools();
        foreach (var t in detected)
            all.Add(t);

        var existingNames = new HashSet<string>(
            all.Select(d => d.Name),
            StringComparer.OrdinalIgnoreCase);
        foreach (var t in _settings.Tools)
        {
            if (existingNames.Add(t.Name))
                all.Add(t);
        }

        _toolsCache = all.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return _toolsCache;
    }

    public AppSettings GetSettings() => _settings;

    public IProjectRepository GetProjectRepository() => _projectRepository;

    public void SaveAppSettings(AppSettings settings)
    {
        _settings = settings;
        // 태그 애니메이션 설정 즉시 전파
        foreach (var card in _allCards)
            card.EnableTagAnimation = settings.EnableTagAnimation;
        InvalidateToolsCache();
        SaveSettings();
    }

    /// <summary>그룹을 기본값으로 초기화하고 모든 카드를 기본 그룹에 재배치합니다.</summary>
    public async Task ResetGroupsAsync()
    {
        var defaultGroup = new ProjectGroup { Name = LocalizationService.Get("DefaultGroupName"), IsDefault = true };

        _suppressReactiveUpdates = true;
        try
        {
            Groups.Clear();
            Groups.Add(defaultGroup);
            SelectedGroupId = null;
        }
        finally
        {
            _suppressReactiveUpdates = false;
        }

        foreach (var card in _allCards)
            card.UpdateGroupId(defaultGroup.Id);

        // 카드 DB 업데이트 + 그룹 동기화를 백그라운드에서 일괄 처리
        var models = _allCards.Select(c => c.ToModel()).ToList();
        var groups = Groups.ToList();
        try
        {
            await Task.Run(() =>
            {
                foreach (var model in models)
                    _projectRepository.Update(model);
                _projectRepository.SyncGroups(groups);
            });
        }
        catch (Exception ex)
        {
            ShowDbError(ex);
        }

        ApplyFilterAndSort();
        SaveSettings();
    }
}
