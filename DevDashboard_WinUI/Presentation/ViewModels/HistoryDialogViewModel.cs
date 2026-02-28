using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>날짜별 그룹 항목 (날짜 헤더 + 해당 날짜의 기록 목록)</summary>
public partial class HistoryDateGroup : ObservableObject
{
    /// <summary>그룹 날짜 (yyyy-MM-dd)</summary>
    public DateTime Date { get; }

    /// <summary>표시용 날짜 문자열</summary>
    public string DateText => Date.ToString("yyyy-MM-dd");

    /// <summary>해당 날짜의 기록 항목 목록</summary>
    public ObservableCollection<HistoryEntryViewModel> Entries { get; }

    public HistoryDateGroup(DateTime date, IEnumerable<HistoryEntryViewModel> entries)
    {
        Date = date;
        Entries = new ObservableCollection<HistoryEntryViewModel>(entries);
    }
}

/// <summary>개별 기록 항목 뷰모델 (펼치기/접기 지원)</summary>
public partial class HistoryEntryViewModel : ObservableObject
{
    public HistoryEntry Model { get; }

    public string Title => Model.Title;
    public string Description => Model.Description;
    public string CompletedAtText => Model.CompletedAt.ToString("yyyy-MM-dd");
    public string CreatedAtText => Model.CreatedAt.ToString("yyyy-MM-dd HH:mm");

    /// <summary>상세 정보 표시 여부</summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>상세 정보 존재 여부</summary>
    public bool HasDescription => !string.IsNullOrWhiteSpace(Model.Description);

    public HistoryEntryViewModel(HistoryEntry model)
    {
        Model = model;
    }

    /// <summary>펼치기/접기 토글</summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}

/// <summary>작업 기록 다이얼로그 뷰모델</summary>
public partial class HistoryDialogViewModel : ObservableObject
{
    private readonly ProjectItem _projectItem;
    private readonly List<HistoryEntry> _allEntries;

    /// <summary>검색어</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>날짜별 그룹 목록</summary>
    public ObservableCollection<HistoryDateGroup> DateGroups { get; } = [];

    /// <summary>작업 기록 존재 여부</summary>
    public bool HasEntries => _allEntries.Count > 0;

    /// <summary>프로젝트 이름</summary>
    public string ProjectName => _projectItem.Name;

    public HistoryDialogViewModel(ProjectItem projectItem)
    {
        _projectItem = projectItem;
        _allEntries = [.. (_projectItem.Histories ?? [])];
        RebuildGroups();
    }

    partial void OnSearchTextChanged(string value)
    {
        RebuildGroups();
    }

    /// <summary>작업 기록 추가</summary>
    public void AddEntry(HistoryEntry entry)
    {
        _allEntries.Add(entry);
        RebuildGroups();
    }

    /// <summary>작업 기록 수정 후 목록 갱신</summary>
    public void RefreshAfterEdit()
    {
        RebuildGroups();
    }

    /// <summary>작업 기록 삭제</summary>
    [RelayCommand]
    private void DeleteEntry(HistoryEntryViewModel? entryVm)
    {
        if (entryVm is null) return;
        // ContentDialog 내부에서는 중첩 ContentDialog 사용 불가 — 바로 삭제
        _allEntries.Remove(entryVm.Model);
        RebuildGroups();
    }

    /// <summary>날짜별 그룹을 다시 구성합니다.</summary>
    private void RebuildGroups()
    {
        DateGroups.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allEntries
            : _allEntries.Where(e =>
                e.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || e.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        var groups = filtered
            .GroupBy(e => e.CompletedAt.Date)
            .OrderByDescending(g => g.Key);

        foreach (var group in groups)
        {
            var entryVms = group
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new HistoryEntryViewModel(e));
            DateGroups.Add(new HistoryDateGroup(group.Key, entryVms));
        }

        OnPropertyChanged(nameof(HasEntries));
    }

    /// <summary>전체 작업 기록을 마크다운 문자열로 변환합니다.</summary>
    public string ExportToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {_projectItem.Name} — 작업 기록");
        sb.AppendLine();
        sb.AppendLine($"내보내기 일시: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        var groups = _allEntries
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

    /// <summary>변경 사항을 모델에 반영합니다.</summary>
    public void SaveToModel()
    {
        _projectItem.Histories = [.. _allEntries];
    }
}
