using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

    // 칸반 4열 — 각 열은 카테고리 그룹의 목록이다(시안: 열 안에서 카테고리별로 묶어 표시)
    public ObservableCollection<TaskColumnGroup> WaitingItems { get; } = [];
    public ObservableCollection<TaskColumnGroup> ActiveItems { get; } = [];
    public ObservableCollection<TaskColumnGroup> CompletedItems { get; } = [];
    public ObservableCollection<TaskColumnGroup> HoldItems { get; } = [];

    /// <summary>목록 뷰 — 상태별 그룹 (각 그룹 안에 카테고리 서브그룹). 항목이 없는 상태도 항상 포함한다.</summary>
    public ObservableCollection<TaskListStatusGroup> ListStatusGroups { get; } = [];

    // 상태별 개수 (현재 카테고리 필터 반영)
    [ObservableProperty] public partial int WaitingCount { get; set; }
    [ObservableProperty] public partial int ActiveCount { get; set; }
    [ObservableProperty] public partial int CompletedCount { get; set; }
    [ObservableProperty] public partial int HoldCount { get; set; }

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

        AvailableCategories = AppSettingsDialogViewModel.ResolveTaskCategories(settings);

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

        BuildColumnGroups(WaitingItems, filtered, TodoStatus.Waiting);
        BuildColumnGroups(ActiveItems, filtered, TodoStatus.Active);
        BuildColumnGroups(CompletedItems, filtered, TodoStatus.Completed);
        BuildColumnGroups(HoldItems, filtered, TodoStatus.Hold);

        // 열은 그룹의 목록이므로 컬렉션의 Count는 카테고리 수다 — 열 헤더에 쓰는 개수는 작업 수여야 하니 그룹별 항목을 합산한다.
        WaitingCount = CountItems(WaitingItems);
        ActiveCount = CountItems(ActiveItems);
        CompletedCount = CountItems(CompletedItems);
        HoldCount = CountItems(HoldItems);

        BuildListStatusGroups(filtered);
    }

    /// <summary>목록 뷰용 상태 그룹 4개를 구성합니다 (각 그룹 안은 칸반과 같은 카테고리 서브그룹).
    /// 항목이 0건인 상태도 그룹을 만든다 — 목록은 4개 상태를 항상 보여주고 빈 그룹에 "작업 없음"을 표시한다.</summary>
    private void BuildListStatusGroups(IEnumerable<TodoItem> filtered)
    {
        ListStatusGroups.Clear();
        foreach (var status in (TodoStatus[])[TodoStatus.Waiting, TodoStatus.Active, TodoStatus.Completed, TodoStatus.Hold])
        {
            // 칸반 열과 같은 그룹 구성을 재사용한다(정렬·배지·"미분류" 처리가 두 뷰에서 어긋나지 않게).
            var categories = new ObservableCollection<TaskColumnGroup>();
            BuildColumnGroups(categories, filtered, status);

            var count = CountItems(categories);
            ListStatusGroups.Add(new TaskListStatusGroup(
                status,
                LocalizationService.Get($"TaskStatus_{status}"),
                count,
                count == 0,
                categories));
        }
    }

    /// <summary>한 상태 열을 카테고리 그룹으로 묶어 채웁니다 (빈 카테고리는 "미분류" 그룹).</summary>
    private void BuildColumnGroups(ObservableCollection<TaskColumnGroup> column, IEnumerable<TodoItem> source, TodoStatus status)
    {
        column.Clear();
        var groups = source
            .Where(t => t.Status == status)
            .GroupBy(t => t.Category ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.CurrentCulture);

        foreach (var g in groups)
        {
            var displayName = string.IsNullOrEmpty(g.Key) ? LocalizationService.Get("TaskCategory_None") : g.Key;
            var (badgeText, hasResult, isFullPass) = BuildPassRateBadge(g.Key);
            column.Add(new TaskColumnGroup(
                displayName,
                badgeText,
                hasResult,
                isFullPass,
                g.OrderByDescending(t => t.CreatedAt).ToList()));
        }
    }

    private static int CountItems(IEnumerable<TaskColumnGroup> groups) => groups.Sum(g => g.Items.Count);

    /// <summary>카테고리 그룹 헤더에 표시할 테스트 배지를 만듭니다 (텍스트 + 통과율을 아는지 여부 + 100% 통과 여부).
    /// 같은 이름의 테스트 카테고리를 기준으로 하며, 테스트가 없거나 한 건도 실행되지 않았으면
    /// "테스트 미실행" 배지를 낸다(통과율 0%와 구분 — 실행해서 다 실패한 것과 아직 안 돌린 것은 다르다).
    /// "미분류"는 실제 카테고리가 아니므로 배지를 달지 않는다.
    /// 통과율은 **실행된(미실행 제외) 건수 대비**로 계산한다(시안 기준 — 전체 건수로 나누면 미실행이 섞여 값이 낮게 나온다).</summary>
    private (string Text, bool HasResult, bool IsFullPass) BuildPassRateBadge(string category)
    {
        if (string.IsNullOrEmpty(category)) return (string.Empty, false, false);

        var testCategory = (_project.TestCategories ?? [])
            .FirstOrDefault(c => string.Equals(c.Name, category, StringComparison.OrdinalIgnoreCase));

        var items = testCategory?.Items;
        var total = items?.Count ?? 0;
        var executed = items?.Count(t => t.Status != TestItem.StatusUntested) ?? 0;
        if (total == 0 || executed == 0)
            return (LocalizationService.Get("TaskNoTestBadge"), false, false);

        var pass = items!.Count(t => t.Status == TestItem.StatusPass);
        var rate = (double)pass / executed * 100d;
        var roundedRate = Math.Round(rate, MidpointRounding.AwayFromZero);
        return (string.Format(LocalizationService.Get("TaskPassRateBadge"), $"{roundedRate:0}", total), true, roundedRate >= 100);
    }

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
    /// 작업의 카테고리와 같은 이름의 테스트 스위트(없으면 자동 생성)에 작업 제목과 같은 테스트 항목을 추가하고 LinkedTestId를 연결합니다.
    /// 같은 이름 스위트에 넣어야 칸반 카테고리 그룹의 통과율 배지(FR-T8)에 반영됩니다.</summary>
    public void CreateLinkedTest(TodoItem todo)
    {
        if (todo is null || !string.IsNullOrEmpty(todo.LinkedTestId)) return; // 이미 연결됨 → 중복 생성 방지

        var categories = _project.TestCategories ??= [];
        // 배지(FR-T8)는 작업 카테고리와 같은 이름의 테스트 스위트를 찾으므로, 링크 테스트를 작업의 카테고리 이름 스위트에 넣는다.
        // 카테고리가 빈(미분류) 작업은 배지 대상이 아니므로 기존 "작업"(TaskLinkedTestCategory) 스위트로 모은다.
        var categoryName = string.IsNullOrEmpty(todo.Category)
            ? LocalizationService.Get("TaskLinkedTestCategory")
            : todo.Category;
        var suite = categories.FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));
        if (suite is null)
        {
            suite = new TestCategory { Name = categoryName };
            categories.Add(suite);
        }

        var test = new TestItem { CategoryId = suite.Id, Text = todo.Text };
        suite.Items.Add(test);
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

/// <summary>카테고리별로 묶은 작업 그룹 — 칸반 열과 목록 상태 그룹이 함께 쓴다.
/// 그룹 헤더에 표시할 테스트 배지를 함께 갖는다
/// (`PassRateBadge`가 빈 문자열이면 배지 미표시, `HasTestResult`가 false면 "테스트 미실행" 배지라 색이 다르다,
/// `IsFullPass`는 `HasTestResult`가 true일 때만 의미 있으며 100% 통과 여부로 초록/호박 색을 가른다).</summary>
public sealed record TaskColumnGroup(string CategoryName, string PassRateBadge, bool HasTestResult, bool IsFullPass, IReadOnlyList<TodoItem> Items);

/// <summary>목록 뷰의 상태 그룹 (예정/진행 중/완료/보류) — 헤더 표시값과 카테고리 서브그룹을 함께 갖는다.
/// dot 색은 상태(Status)로부터 View가 정한다(VM에 Brush를 두지 않기 위함).
/// `IsEmpty`가 true면 서브그룹 대신 "작업 없음" 안내를 표시한다.</summary>
public sealed record TaskListStatusGroup(
    TodoStatus Status,
    string StatusLabel,
    int Count,
    bool IsEmpty,
    IReadOnlyList<TaskColumnGroup> Categories);
