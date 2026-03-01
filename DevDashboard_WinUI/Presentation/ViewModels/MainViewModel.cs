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
    private bool _isInitializing = true;

    // 전체 카드 원본 목록 (필터/정렬 전)
    private readonly List<ProjectCardViewModel> _allCards = [];

    [ObservableProperty]
    private BulkObservableCollection<ProjectCardViewModel> _filteredCards = [];

    /// <summary>UI에 바인딩되는 카드 목록 (FilteredCards + AddCardPlaceholder 마지막 항목)</summary>
    public BulkObservableCollection<object> DisplayCards { get; } = [];

    [ObservableProperty]
    private ObservableCollection<ProjectGroup> _groups = [];

    [ObservableProperty]
    private string? _selectedGroupId;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private SortOrder _currentSortOrder = SortOrder.Name;

    [ObservableProperty]
    private int _projectCount;

    // --- 최신 버전 확인 ---

    [ObservableProperty]
    private bool _hasNewVersion;

    private string _latestReleaseUrl = string.Empty;

    /// <summary>GitHub Releases API를 통해 최신 버전을 확인합니다.</summary>
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
                _settings.Groups.Add(new ProjectGroup { Name = LocalizationService.Get("DefaultGroupName") });

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
            catch { return new List<ProjectItem>(); }
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
        {
            AddCardInternal(card);
            card.StartIconLoad();
            card.StartGitStatusLoad();
        }
        vmSw.Stop();

        var bindSw = Stopwatch.StartNew();
        ApplyFilterAndSort();
        bindSw.Stop();

        IsInitializing = false;
        initSw.Stop();
        PerfLog($"Initialize: cards={projects.Count}, db+tools={dbSw.ElapsedMilliseconds}ms, vm={vmSw.ElapsedMilliseconds}ms, bind={bindSw.ElapsedMilliseconds}ms, total={initSw.ElapsedMilliseconds}ms");
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
    }

    private void UnsubscribeFromCard(ProjectCardViewModel card)
    {
        card.DeleteRequested -= OnCardDeleteRequested;
        card.EditRequested -= OnCardEditRequested;
        card.PinToggled -= OnCardPinToggled;
        card.CommandScriptChanged -= OnCommandScriptChanged;
        card.TodoChanged -= OnTodoChanged;
        card.HistoryChanged -= OnHistoryChanged;
    }

    private static void ShowDbError(Exception ex)
    {
        _ = DialogService.ShowErrorAsync(ex.Message, LocalizationService.Get("SaveError"));
    }

    private void OnCardDeleteRequested(object? sender, EventArgs e)
    {
        if (sender is not ProjectCardViewModel card) return;
        UnsubscribeFromCard(card);

        try
        {
            _projectRepository.Delete(card.Id);
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

    private void OnCardPinToggled(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            try
            {
                _projectRepository.UpdatePinned(card.Id, card.IsPinned);
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        ApplyFilterAndSort();
        SaveSettings();
    }

    private void OnCommandScriptChanged(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            var model = card.ToModel();
            try
            {
                _projectRepository.SaveCommandScripts(model.Id, model.CommandScripts);
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }
    }

    private void OnTodoChanged(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            var model = card.ToModel();
            try
            {
                _projectRepository.SaveTodos(model.Id, model.Todos);
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        if (sender is ProjectCardViewModel card)
        {
            var model = card.ToModel();
            try
            {
                _projectRepository.SaveHistories(model.Id, model.Histories);
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }
    }

    /// <summary>핀 고정된 카드의 순서를 변경합니다.</summary>
    public void MovePinnedCard(string draggedCardId, string targetCardId, bool insertAfter = false)
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
            _projectRepository.UpdatePinOrder(pinnedIds);
        }
        catch (Exception ex)
        {
            ShowDbError(ex);
        }

        ApplyFilterAndSort();
        SaveSettings();
    }

    /// <summary>프로젝트를 추가하거나 수정합니다.</summary>
    public void AddOrUpdateProject(ProjectItem item)
    {
        var existing = _allCards.FirstOrDefault(c => c.Id == item.Id);
        if (existing is not null)
        {
            // ProjectSettingsDialog는 CommandScripts를 관리하지 않으므로 기존 값을 보존
            item.CommandScripts = existing.ToModel().CommandScripts;

            try
            {
                _projectRepository.Update(item);
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
                _projectRepository.Add(item);
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

    /// <summary>그룹을 추가하거나 수정합니다.</summary>
    public void AddOrUpdateGroup(ProjectGroup group)
    {
        var existing = Groups.FirstOrDefault(g => g.Id == group.Id);
        if (existing is not null)
            Groups[Groups.IndexOf(existing)] = group;
        else
            Groups.Add(group);

        SaveSettings();
    }

    /// <summary>그룹을 삭제하고 해당 그룹에 속한 프로젝트의 GroupId를 초기화합니다.</summary>
    public void DeleteGroup(string groupId)
    {
        var group = Groups.FirstOrDefault(g => g.Id == groupId);
        if (group is null) return;

        Groups.Remove(group);

        var affectedCards = _allCards.Where(c => c.GroupId == groupId).ToList();
        foreach (var card in affectedCards)
        {
            card.UpdateGroupId(string.Empty);
            try
            {
                _projectRepository.Update(card.ToModel());
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        if (SelectedGroupId == groupId)
            SelectedGroupId = null;

        ApplyFilterAndSort();
        SaveSettings();
    }

    public int GroupCount => Groups.Count;

    /// <summary>그룹 추가 가능 여부 (최대 10개 제한)</summary>
    public bool CanAddGroup => Groups.Count < 10;

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
            {
                AddCardInternal(card);
                card.StartIconLoad();
                card.StartGitStatusLoad();
            }

            ApplyFilterAndSort();
            refreshSw.Stop();
            PerfLog($"Refresh: cards={models.Count}, total={refreshSw.ElapsedMilliseconds}ms");
        }
        finally
        {
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
                catch { return new List<ProjectItem>(); }
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
            {
                AddCardInternal(card);
                card.StartIconLoad();
                card.StartGitStatusLoad();
            }

            ApplyFilterAndSort();
            hardSw.Stop();
            PerfLog($"HardRefresh: cards={projects.Count}, total={hardSw.ElapsedMilliseconds}ms");
        }
        finally
        {
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
}
