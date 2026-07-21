using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 테스트 전체 페이지 뷰모델 — 한 프로젝트의 테스트를 스위트(카테고리) 그룹·상태 통계·통과율로 표시하고 편집합니다.
/// 변경 시 즉시 영속화(증분 저장, FIFO 체인)하고, 카드 상태(HasActiveTest)를 갱신합니다.
/// </summary>
public partial class TestPageViewModel : ObservableObject
{
    private readonly ProjectItem _project;
    private readonly IProjectRepository _repository;
    private readonly Action _refreshCardState;

    // 저장 직렬화 체인 — SaveTestCategories는 delete+reinsert 전체 스냅샷 방식이라
    // 연속 변경 시 순서가 뒤바뀌면 최신 변경이 유실될 수 있어 FIFO로 이어 실행한다.
    // 모든 저장 트리거는 UI 스레드에서 호출되므로 _saveChain 재대입은 단일 스레드에서만 일어난다.
    private Task _saveChain = Task.CompletedTask;

    /// <summary>대상 프로젝트</summary>
    public ProjectItem Project => _project;

    /// <summary>스위트(카테고리) 그룹 — 현재 상태 필터 반영, 각 그룹에 통과율 포함</summary>
    public ObservableCollection<TestSuiteGroup> SuiteGroups { get; } = [];

    // 상태별 개수 (프로젝트 전체 기준 — 통계 카드용, 필터와 무관)
    [ObservableProperty] public partial int PassCount { get; set; }
    [ObservableProperty] public partial int FailCount { get; set; }
    [ObservableProperty] public partial int UntestedCount { get; set; }
    [ObservableProperty] public partial int TotalCount { get; set; }

    /// <summary>선택된 상태 필터 (null이면 전체, "Pass"/"Fail"/"Untested")</summary>
    [ObservableProperty] public partial string? SelectedStatus { get; set; }

    /// <summary>선택된 스위트 필터 (null이면 전체). 상태 필터와 직교하게 함께 적용됩니다.</summary>
    [ObservableProperty] public partial string? SelectedSuiteFilter { get; set; }

    /// <summary>스위트로 선택 가능한 작업 카테고리 목록 (기본 + 사용자 정의).
    /// 테스트 스위트를 작업 카테고리에 맞춰야 칸반 카테고리 그룹의 통과율 배지(FR-T8)에 반영된다.</summary>
    public IReadOnlyList<string> AvailableCategories { get; }

    public TestPageViewModel(ProjectItem project, IProjectRepository repository, AppSettings settings, Action refreshCardState)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(refreshCardState);

        _project = project;
        _repository = repository;
        _refreshCardState = refreshCardState;

        AvailableCategories = AppSettingsDialogViewModel.ResolveTaskCategories(settings);

        Rebuild();
    }

    partial void OnSelectedStatusChanged(string? value) => Rebuild();

    partial void OnSelectedSuiteFilterChanged(string? value) => Rebuild();

    /// <summary>상태·스위트 필터를 적용해 스위트 그룹·상태별 통계·통과율을 재구성합니다.
    /// 통계·통과율은 프로젝트/스위트 전체 기준(필터 무관), 표시 대상만 필터를 적용합니다.</summary>
    private void Rebuild()
    {
        var categories = _project.TestCategories ?? [];

        // 상태별 통계 (프로젝트 전체 기준 — 통계 카드·탭 개수 공용이라 필터를 반영하지 않는다)
        var allItems = categories.SelectMany(c => c.Items).ToList();
        PassCount = allItems.Count(t => t.Status == TestItem.StatusPass);
        FailCount = allItems.Count(t => t.Status == TestItem.StatusFail);
        UntestedCount = allItems.Count(t => t.Status == TestItem.StatusUntested);
        TotalCount = allItems.Count;

        // 스위트 필터 (null이면 전체) — 상태 필터와 직교하게 함께 적용된다
        var visibleCategories = SelectedSuiteFilter is null
            ? categories
            : categories.Where(c => string.Equals(c.Name, SelectedSuiteFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        // 스위트 그룹 (표시 항목은 필터 적용, 통과율은 스위트 전체 기준)
        SuiteGroups.Clear();
        foreach (var cat in visibleCategories)
        {
            var items = SelectedStatus is null
                ? cat.Items.ToList()
                : cat.Items.Where(t => t.Status == SelectedStatus).ToList();

            // 필터가 걸린 상태에서 표시 항목이 없는 스위트는 숨긴다 (전체 필터에선 빈 스위트도 노출)
            if (items.Count == 0 && SelectedStatus is not null) continue;

            var total = cat.Items.Count;
            var pass = cat.Items.Count(t => t.Status == TestItem.StatusPass);
            var passRate = total == 0 ? 0d : (double)pass / total * 100d;

            SuiteGroups.Add(new TestSuiteGroup(
                cat,
                new ObservableCollection<TestItem>(items.OrderByDescending(t => t.CreatedAt)),
                pass, total, passRate));
        }
    }

    // --- 스위트(카테고리) 편집 ---

    /// <summary>새 스위트를 추가합니다 (이름 중복 시 무시).</summary>
    public void AddSuite(string name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        var categories = _project.TestCategories ??= [];
        if (categories.Any(c => c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase))) return;

        categories.Add(new TestCategory { Name = trimmed });
        Rebuild();
        Persist();
    }

    /// <summary>스위트 이름을 수정합니다.</summary>
    public void RenameSuite(TestCategory category, string newName)
    {
        if (category is null) return;
        var trimmed = newName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        category.Name = trimmed;
        Rebuild();
        Persist();
    }

    /// <summary>스위트를 삭제합니다 (소속 테스트도 함께 삭제, 삭제 확인은 View에서 처리).</summary>
    public void DeleteSuite(TestCategory category)
    {
        if (category is null) return;
        _project.TestCategories?.Remove(category);
        Rebuild();
        Persist();
    }

    // --- 테스트 항목 편집 ---

    /// <summary>스위트 이름으로 새 테스트 항목을 추가합니다 (스위트가 없으면 생성). 등록 다이얼로그 결과 반영.</summary>
    public void AddTestToSuite(string suiteName, TestItem test)
    {
        if (test is null) return;
        var trimmed = suiteName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        var categories = _project.TestCategories ??= [];
        var suite = categories.FirstOrDefault(c => c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (suite is null)
        {
            suite = new TestCategory { Name = trimmed };
            categories.Add(suite);
        }

        test.CategoryId = suite.Id;
        suite.Items.Add(test);
        Rebuild();
        Persist();
    }

    /// <summary>테스트 항목 편집 결과를 반영합니다 (in-place 수정이므로 재구성·저장만 수행).</summary>
    public void UpdateTest(TestItem test)
    {
        if (test is null) return;
        Rebuild();
        Persist();
    }

    /// <summary>테스트 항목을 삭제합니다 (삭제 확인은 View에서 처리).</summary>
    public void DeleteTest(TestItem test)
    {
        if (test is null) return;
        var category = _project.TestCategories?.FirstOrDefault(c => c.Id == test.CategoryId);
        category?.Items.Remove(test);
        Rebuild();
        Persist();
    }

    /// <summary>테스트 항목의 상태를 변경합니다 (통과 전환 시 완료 일시 기록, 통과 해제 시 초기화).</summary>
    public void ChangeTestStatus(TestItem test, string newStatus)
    {
        if (test is null || test.Status == newStatus) return;

        var oldStatus = test.Status;
        test.Status = newStatus;
        if (newStatus == TestItem.StatusPass)
            test.CompletedAt = DateTime.Now;
        else if (oldStatus == TestItem.StatusPass)
            test.CompletedAt = null;

        Rebuild();
        Persist();
    }

    /// <summary>테스트 항목의 진행 내용(메모)을 수정합니다.</summary>
    public void EditProgressNote(TestItem test, string newNote)
    {
        if (test is null) return;
        test.ProgressNote = newNote?.Trim() ?? string.Empty;
        Rebuild();
        Persist();
    }

    /// <summary>현재 테스트 카테고리 목록을 백그라운드에서 저장하고 카드 상태를 갱신합니다.</summary>
    private void Persist()
    {
        _refreshCardState();
        var categories = (_project.TestCategories ?? []).ToList();
        QueueSave(() => _repository.SaveTestCategories(_project.Id, categories));
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
                System.Diagnostics.Debug.WriteLine($"[TestPage] 저장 실패: {ex}");
            }
        }, TaskScheduler.Default);
    }
}

/// <summary>테스트 페이지의 스위트 그룹 — 필터된 표시 항목 + 스위트 전체 기준 통과율.</summary>
public sealed record TestSuiteGroup(
    TestCategory Category,
    ObservableCollection<TestItem> Items,
    int PassCount,
    int TotalCount,
    double PassRate);
