using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 작업 기록 다이얼로그 공통 베이스 뷰모델 — 검색·날짜 그룹·CRUD·마크다운 내보내기·페이지네이션·유형을 담당합니다.
/// 카드별(HistoryDialogViewModel)·전체(ProjectHistoryDialogViewModel)가 상속하며, 저장/수정 표시·프로젝트 선택만 각자 담당합니다.
/// </summary>
public abstract partial class HistoryDialogViewModelBase : ObservableObject
{
    /// <summary>현재 편집 대상 기록 목록 (파생이 로드·저장)</summary>
    protected readonly List<HistoryEntry> _entries;

    private readonly int _pageSize;

    /// <summary>검색어</summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>현재 페이지 (1부터)</summary>
    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    /// <summary>날짜별 그룹 목록 (현재 페이지 항목)</summary>
    public ObservableCollection<HistoryDateGroup> DateGroups { get; } = [];

    /// <summary>작업 기록 존재 여부</summary>
    public bool HasEntries => _entries.Count > 0;

    /// <summary>전체 페이지 수 (검색 필터 반영, 최소 1)</summary>
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredEntries().Count / (double)_pageSize));

    /// <summary>페이지 표시 텍스트 ("현재 / 전체")</summary>
    public string PageInfoText => $"{CurrentPage} / {TotalPages}";

    /// <summary>이전 페이지 이동 가능 여부</summary>
    public bool CanPrevPage => CurrentPage > 1;

    /// <summary>다음 페이지 이동 가능 여부</summary>
    public bool CanNextPage => CurrentPage < TotalPages;

    /// <summary>선택 가능한 작업 기록 유형 목록 (기본 + 사용자 정의)</summary>
    public IReadOnlyList<string> AvailableKinds { get; }

    protected HistoryDialogViewModelBase(List<HistoryEntry> entries, int pageSize, IReadOnlyList<string> availableKinds)
    {
        _entries = entries;
        _pageSize = pageSize > 0 ? pageSize : 100;
        AvailableKinds = availableKinds;
        RebuildGroups();
    }

    /// <summary>기본 유형 + 사용자 정의 유형을 합쳐 선택 목록을 구성합니다.</summary>
    protected static IReadOnlyList<string> BuildKinds(AppSettings settings)
        => AppSettingsDialogViewModel.DefaultHistoryKinds
            .Concat(settings.HistoryKinds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        RebuildGroups();
    }

    partial void OnCurrentPageChanged(int value) => RebuildGroups();

    [RelayCommand]
    private void PrevPage()
    {
        if (CurrentPage > 1) CurrentPage--;
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages) CurrentPage++;
    }

    /// <summary>검색 필터를 적용한 기록 목록을 반환합니다 (정렬·페이지 미적용).</summary>
    private List<HistoryEntry> FilteredEntries()
        => string.IsNullOrWhiteSpace(SearchText)
            ? _entries
            : _entries.Where(e =>
                e.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || e.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>검색 필터 → 현재 페이지 슬라이스 → 날짜 그룹핑 순으로 목록을 재구성합니다.</summary>
    protected void RebuildGroups()
    {
        var filtered = FilteredEntries()
            .OrderByDescending(e => e.CompletedAt.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)_pageSize));

        // 검색·삭제로 페이지 범위를 벗어나면 보정 (재대입 시 OnCurrentPageChanged가 다시 호출)
        if (CurrentPage > totalPages) { CurrentPage = totalPages; return; }
        if (CurrentPage < 1) { CurrentPage = 1; return; }

        var pageItems = filtered.Skip((CurrentPage - 1) * _pageSize).Take(_pageSize);

        DateGroups.Clear();
        var groups = pageItems.GroupBy(e => e.CompletedAt.Date).OrderByDescending(g => g.Key);
        foreach (var group in groups)
        {
            var entryVms = group.OrderByDescending(e => e.CreatedAt).Select(e => new HistoryEntryViewModel(e));
            DateGroups.Add(new HistoryDateGroup(group.Key, entryVms));
        }

        OnPropertyChanged(nameof(HasEntries));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageInfoText));
        OnPropertyChanged(nameof(CanPrevPage));
        OnPropertyChanged(nameof(CanNextPage));
    }

    /// <summary>작업 기록 추가</summary>
    public void AddEntry(HistoryEntry entry)
    {
        _entries.Add(entry);
        OnEntryAdded(entry);
        OnEntriesModified();
        RebuildGroups();
    }

    /// <summary>작업 기록 수정 (제목·설명·유형·완료일)</summary>
    public void UpdateEntry(HistoryEntryViewModel entryVm, string title, string description, string kind, DateTime completedAt)
    {
        entryVm.Model.Title = title;
        entryVm.Model.Description = description;
        entryVm.Model.Kind = kind;
        entryVm.Model.CompletedAt = completedAt;
        OnEntriesModified();
        RebuildGroups();
    }

    /// <summary>수정 후 목록 갱신</summary>
    public void RefreshAfterEdit()
    {
        OnEntriesModified();
        RebuildGroups();
    }

    /// <summary>작업 기록 삭제</summary>
    [RelayCommand]
    private void DeleteEntry(HistoryEntryViewModel? entryVm)
    {
        if (entryVm is null) return;
        // ContentDialog 내부에서는 중첩 ContentDialog 사용 불가 — 바로 삭제
        _entries.Remove(entryVm.Model);
        OnEntriesModified();
        RebuildGroups();
    }

    /// <summary>항목 추가 시 파생 훅 (단일 프로젝트 VM이 세션 신규 항목을 추적).</summary>
    protected virtual void OnEntryAdded(HistoryEntry entry) { }

    /// <summary>기록 변경 시 파생 훅 (전체 VM이 수정된 프로젝트를 표시).</summary>
    protected abstract void OnEntriesModified();

    /// <summary>내보내기 제목에 쓰일 프로젝트 이름 (파생 제공).</summary>
    protected abstract string ExportProjectName { get; }

    /// <summary>현재 기록 전체를 마크다운 문자열로 변환합니다 (검색·페이지 무관 전체).</summary>
    public string ExportToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {ExportProjectName} — 작업 기록");
        sb.AppendLine();
        sb.AppendLine($"내보내기 일시: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        var groups = _entries
            .GroupBy(e => e.CompletedAt.Date)
            .OrderByDescending(g => g.Key);

        foreach (var group in groups)
        {
            sb.AppendLine($"## {group.Key:yyyy-MM-dd}");
            sb.AppendLine();

            foreach (var entry in group.OrderByDescending(e => e.CreatedAt))
            {
                sb.AppendLine($"### {entry.Title}");
                sb.AppendLine();

                if (!string.IsNullOrWhiteSpace(entry.Description))
                {
                    sb.AppendLine(entry.Description);
                    sb.AppendLine();
                }

                sb.AppendLine($"- 등록일: {entry.CreatedAt:yyyy-MM-dd HH:mm}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
