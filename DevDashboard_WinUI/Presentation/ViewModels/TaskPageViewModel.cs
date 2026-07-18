using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Services;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 작업(칸반) 전체 페이지 뷰모델 — 한 프로젝트의 To-Do를 칸반(4열)/목록으로 표시하고 편집합니다.
/// 변경 시 즉시 영속화(증분 저장)하고, 카드 상태(HasActiveTodo)를 갱신합니다.
/// </summary>
public partial class TaskPageViewModel : ObservableObject
{
    private readonly ProjectItem _project;
    private readonly IProjectRepository _repository;
    private readonly AppSettings _settings;
    private readonly Action _refreshCardState;

    // LinkedTestId -> TestItem.Status 조회용 (작업 카드 테스트 배지, T9에서 소비)
    private readonly Dictionary<string, string> _testStatusById = new(StringComparer.OrdinalIgnoreCase);

    // 저장 직렬화 체인 — SaveTodos/SaveHistories는 delete+reinsert 전체 스냅샷 방식이라
    // 연속 변경(드래그 등) 시 순서가 뒤바뀌면 최신 변경이 유실될 수 있어 FIFO로 이어 실행한다.
    // 모든 저장 트리거는 UI 스레드에서 호출되므로 _saveChain 재대입은 단일 스레드에서만 일어난다.
    private Task _saveChain = Task.CompletedTask;

    /// <summary>대상 프로젝트 (테스트 자동 생성·작업기록 병합 등에 사용)</summary>
    public ProjectItem Project => _project;

    /// <summary>완료 전환 시 작업기록 팝업 표시가 필요할 때 발생 (View가 HistoryDialog 표시 후 CommitWorkLog 호출)</summary>
    public event EventHandler<TodoItem>? WorkLogRequested;

    // 칸반 4열
    public ObservableCollection<TodoItem> WaitingItems { get; } = [];
    public ObservableCollection<TodoItem> ActiveItems { get; } = [];
    public ObservableCollection<TodoItem> CompletedItems { get; } = [];
    public ObservableCollection<TodoItem> HoldItems { get; } = [];

    /// <summary>목록 뷰 — 카테고리별 그룹</summary>
    public ObservableCollection<TaskCategoryGroup> CategoryGroups { get; } = [];

    // 상태별 개수 (현재 카테고리 필터 반영)
    [ObservableProperty] public partial int WaitingCount { get; set; }
    [ObservableProperty] public partial int ActiveCount { get; set; }
    [ObservableProperty] public partial int CompletedCount { get; set; }
    [ObservableProperty] public partial int HoldCount { get; set; }
    [ObservableProperty] public partial int TotalCount { get; set; }

    /// <summary>칸반 뷰(true) / 목록 뷰(false)</summary>
    [ObservableProperty] public partial bool IsKanbanView { get; set; } = true;

    /// <summary>선택된 카테고리 필터 (null이면 전체)</summary>
    [ObservableProperty] public partial string? SelectedCategoryFilter { get; set; }

    /// <summary>필터·다이얼로그에서 선택 가능한 카테고리 목록 (기본 + 사용자 정의)</summary>
    public IReadOnlyList<string> AvailableCategories { get; }

    public TaskPageViewModel(ProjectItem project, IProjectRepository repository, AppSettings settings, Action refreshCardState)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(refreshCardState);

        _project = project;
        _repository = repository;
        _settings = settings;
        _refreshCardState = refreshCardState;

        AvailableCategories = AppSettingsDialogViewModel.DefaultTaskCategories
            .Concat(settings.TaskCategories)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        BuildTestStatusLookup();
        Rebuild();
    }

    /// <summary>연결된 테스트의 로드된 카테고리에서 testId->status 조회 테이블을 구성합니다.</summary>
    private void BuildTestStatusLookup()
    {
        _testStatusById.Clear();
        foreach (var cat in _project.TestCategories ?? [])
            foreach (var item in cat.Items)
                _testStatusById[item.Id] = item.Status;
    }

    /// <summary>작업에 연결된 테스트의 상태를 반환합니다. 연결이 없거나 대상 테스트가 없으면 null.</summary>
    public string? GetLinkedTestStatus(TodoItem todo)
    {
        if (todo is null || string.IsNullOrEmpty(todo.LinkedTestId)) return null;
        return _testStatusById.TryGetValue(todo.LinkedTestId, out var status) ? status : null;
    }

    /// <summary>연결 테스트 상태(통과/실패/미실행)를 작업 카드 배지 텍스트로 변환합니다.</summary>
    private static string MapTestBadge(string? status) => status switch
    {
        TestItem.StatusPass => LocalizationService.Get("TaskTestBadge_Pass"),
        TestItem.StatusFail => LocalizationService.Get("TaskTestBadge_Fail"),
        TestItem.StatusUntested => LocalizationService.Get("TaskTestBadge_Untested"),
        _ => string.Empty,
    };

    partial void OnSelectedCategoryFilterChanged(string? value) => Rebuild();

    /// <summary>현재 카테고리 필터를 적용해 칸반 4열·목록 그룹·개수를 재구성합니다.</summary>
    private void Rebuild()
    {
        var todos = _project.Todos ?? [];
        foreach (var t in todos)
            t.LinkedTestBadge = MapTestBadge(GetLinkedTestStatus(t));

        var filtered = SelectedCategoryFilter is null
            ? todos
            : todos.Where(t => string.Equals(t.Category, SelectedCategoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        FillColumn(WaitingItems, filtered, TodoStatus.Waiting);
        FillColumn(ActiveItems, filtered, TodoStatus.Active);
        FillColumn(CompletedItems, filtered, TodoStatus.Completed);
        FillColumn(HoldItems, filtered, TodoStatus.Hold);

        WaitingCount = WaitingItems.Count;
        ActiveCount = ActiveItems.Count;
        CompletedCount = CompletedItems.Count;
        HoldCount = HoldItems.Count;
        TotalCount = filtered.Count();

        RebuildCategoryGroups(filtered);
    }

    private static void FillColumn(ObservableCollection<TodoItem> column, IEnumerable<TodoItem> source, TodoStatus status)
    {
        column.Clear();
        foreach (var t in source.Where(t => t.Status == status).OrderByDescending(t => t.CreatedAt))
            column.Add(t);
    }

    /// <summary>목록 뷰용 카테고리 그룹을 구성합니다 (빈 카테고리는 "미분류").</summary>
    private void RebuildCategoryGroups(IEnumerable<TodoItem> filtered)
    {
        CategoryGroups.Clear();
        var groups = filtered
            .GroupBy(t => string.IsNullOrEmpty(t.Category) ? LocalizationService.Get("TaskCategory_None") : t.Category)
            .OrderBy(g => g.Key, StringComparer.CurrentCulture);

        foreach (var g in groups)
            CategoryGroups.Add(new TaskCategoryGroup(g.Key, g.OrderByDescending(t => t.CreatedAt).ToList()));
    }

    [RelayCommand]
    private void ShowKanban() => IsKanbanView = true;

    [RelayCommand]
    private void ShowList() => IsKanbanView = false;

    /// <summary>작업의 상태를 변경합니다 (칸반 드래그·상태 콤보에서 호출). 저장·카드 갱신·완료 훅을 처리합니다.</summary>
    public void MoveToStatus(TodoItem todo, TodoStatus newStatus)
    {
        if (todo is null || todo.Status == newStatus) return;

        var wasCompleted = todo.Status == TodoStatus.Completed;
        todo.Status = newStatus;
        Rebuild();
        PersistTodos();

        // 완료로 전환됐고 설정이 켜져 있으면 작업기록 팝업 요청
        if (newStatus == TodoStatus.Completed && !wasCompleted && _settings.ShowWorkLogPopupOnTodoComplete)
            WorkLogRequested?.Invoke(this, todo);
    }

    /// <summary>새 작업을 추가합니다 (편집 다이얼로그 결과 반영).</summary>
    public void AddTodo(TodoItem todo)
    {
        if (todo is null) return;
        (_project.Todos ??= []).Add(todo);
        Rebuild();
        PersistTodos();
    }

    /// <summary>작업에 연결된 테스트를 생성합니다 (FR-T6 "테스트 추가" 토글).
    /// 프로젝트의 "작업" 테스트 카테고리(없으면 자동 생성)에 작업 제목과 같은 테스트 항목을 추가하고 LinkedTestId를 연결합니다.</summary>
    public void CreateLinkedTest(TodoItem todo)
    {
        if (todo is null || !string.IsNullOrEmpty(todo.LinkedTestId)) return; // 이미 연결됨 → 중복 생성 방지

        var categories = _project.TestCategories ??= [];
        var categoryName = LocalizationService.Get("TaskLinkedTestCategory");
        var workCategory = categories.FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));
        if (workCategory is null)
        {
            workCategory = new TestCategory { Name = categoryName };
            categories.Add(workCategory);
        }

        var test = new TestItem { CategoryId = workCategory.Id, Text = todo.Text };
        workCategory.Items.Add(test);
        todo.LinkedTestId = test.Id;

        BuildTestStatusLookup();
        Rebuild();

        var categoriesSnapshot = categories.ToList();
        var todosSnapshot = (_project.Todos ?? []).ToList();
        QueueSave(() => _repository.SaveTestCategories(_project.Id, categoriesSnapshot));
        QueueSave(() => _repository.SaveTodos(_project.Id, todosSnapshot));
    }

    /// <summary>기존 작업 편집 결과를 반영합니다 (편집은 항목 in-place 수정이므로 재구성·저장만 수행).</summary>
    public void UpdateTodo(TodoItem todo)
    {
        if (todo is null) return;
        Rebuild();
        PersistTodos();
    }

    /// <summary>작업을 삭제합니다 (삭제 확인은 View에서 처리).</summary>
    public void DeleteTodo(TodoItem todo)
    {
        if (todo is null) return;
        _project.Todos?.Remove(todo);
        Rebuild();
        PersistTodos();
    }

    /// <summary>작업기록 팝업에서 추가된 기록을 프로젝트에 병합하고 영속화합니다 (완료 훅 — plan M1).</summary>
    public void CommitWorkLog(IList<HistoryEntry> newEntries)
    {
        if (newEntries is not { Count: > 0 }) return;
        (_project.Histories ??= []).AddRange(newEntries);
        var histories = _project.Histories.ToList();
        QueueSave(() => _repository.SaveHistories(_project.Id, histories));
    }

    /// <summary>현재 작업 목록을 백그라운드에서 저장하고 카드 상태를 갱신합니다.</summary>
    private void PersistTodos()
    {
        _refreshCardState();
        var todos = (_project.Todos ?? []).ToList();
        QueueSave(() => _repository.SaveTodos(_project.Id, todos));
    }

    /// <summary>저장 작업을 직렬화 체인에 이어 붙여 순서대로(FIFO) 실행합니다.
    /// 호출 시점에 스냅샷을 잡아 넘기므로, 앞선 저장이 끝난 뒤 다음 저장이 최신 스냅샷을 씁니다.
    /// 실패는 삼키지 않고 디버그 로그로 남겨 조용한 유실을 막습니다.</summary>
    private void QueueSave(Action saveAction)
    {
        _saveChain = _saveChain.ContinueWith(_ =>
        {
            try
            {
                saveAction();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskPage] 저장 실패: {ex}");
            }
        }, TaskScheduler.Default);
    }
}

/// <summary>목록 뷰에서 카테고리별로 묶은 작업 그룹</summary>
public sealed record TaskCategoryGroup(string CategoryName, IReadOnlyList<TodoItem> Items);
