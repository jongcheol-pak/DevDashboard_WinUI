using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Models;
using DevDashboard.Services;

namespace DevDashboard.ViewModels;

/// <summary>프로젝트 작업 기록 다이얼로그 뷰모델 — 모든 프로젝트의 작업 기록을 조회/관리</summary>
public partial class ProjectHistoryDialogViewModel : ObservableObject
{
    private readonly IProjectRepository _projectRepository;
    private List<HistoryEntry> _currentEntries = [];
    private readonly HashSet<string> _modifiedProjectIds = [];

    /// <summary>검색어</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>선택된 프로젝트</summary>
    [ObservableProperty]
    private ProjectItem? _selectedProject;

    /// <summary>ComboBox에 표시할 프로젝트 목록</summary>
    public ObservableCollection<ProjectItem> Projects { get; }

    /// <summary>날짜별 그룹 목록</summary>
    public ObservableCollection<HistoryDateGroup> DateGroups { get; } = [];

    /// <summary>선택된 프로젝트의 작업 기록 존재 여부</summary>
    public bool HasEntries => _currentEntries.Count > 0;

    public ProjectHistoryDialogViewModel(List<ProjectItem> projects, IProjectRepository projectRepository)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(projectRepository);

        _projectRepository = projectRepository;
        Projects = new ObservableCollection<ProjectItem>(projects.OrderBy(p => p.Name));

        if (Projects.Count > 0)
            SelectedProject = Projects[0];
    }

    partial void OnSelectedProjectChanged(ProjectItem? oldValue, ProjectItem? newValue)
    {
        // 이전 프로젝트의 변경 사항을 모델에 반영
        SaveCurrentEntriesToModel(oldValue);

        // 새 프로젝트의 기록 로드
        SearchText = string.Empty;
        LoadEntries();
    }

    partial void OnSearchTextChanged(string value) => RebuildGroups();

    /// <summary>선택된 프로젝트의 기록을 로드합니다.</summary>
    private void LoadEntries()
    {
        _currentEntries = SelectedProject is null
            ? []
            : [.. (SelectedProject.Histories ?? [])];
        RebuildGroups();
    }

    /// <summary>현재 편집 중인 기록을 프로젝트 모델에 반영합니다.</summary>
    private void SaveCurrentEntriesToModel(ProjectItem? project)
    {
        if (project is null) return;
        project.Histories = [.. _currentEntries];
    }

    /// <summary>작업 기록 추가</summary>
    public void AddEntry(HistoryEntry entry)
    {
        _currentEntries.Add(entry);
        MarkCurrentProjectModified();
        RebuildGroups();
    }

    /// <summary>작업 기록 수정 후 목록 갱신</summary>
    public void RefreshAfterEdit()
    {
        MarkCurrentProjectModified();
        RebuildGroups();
    }

    /// <summary>작업 기록 삭제</summary>
    [RelayCommand]
    private void DeleteEntry(HistoryEntryViewModel? entryVm)
    {
        if (entryVm is null) return;
        // ContentDialog 내부에서는 중첩 ContentDialog 사용 불가 — 바로 삭제
        _currentEntries.Remove(entryVm.Model);
        MarkCurrentProjectModified();
        RebuildGroups();
    }

    /// <summary>현재 선택된 프로젝트를 수정됨으로 표시합니다.</summary>
    private void MarkCurrentProjectModified()
    {
        if (SelectedProject is not null)
            _modifiedProjectIds.Add(SelectedProject.Id);
    }

    /// <summary>날짜별 그룹을 다시 구성합니다.</summary>
    private void RebuildGroups()
    {
        DateGroups.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _currentEntries
            : _currentEntries.Where(e =>
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

    /// <summary>선택된 프로젝트의 작업 기록을 마크다운 문자열로 변환합니다.</summary>
    public string ExportToMarkdown()
    {
        var projectName = SelectedProject?.Name ?? string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine($"# {projectName} — 작업 기록");
        sb.AppendLine();
        sb.AppendLine($"내보내기 일시: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        var groups = _currentEntries
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

    /// <summary>변경 사항을 모델 및 DB에 저장합니다.</summary>
    public void SaveAll()
    {
        // 현재 프로젝트의 편집 내용을 모델에 반영
        SaveCurrentEntriesToModel(SelectedProject);

        // 수정된 프로젝트의 기록을 DB에 저장
        foreach (var id in _modifiedProjectIds)
        {
            var project = Projects.FirstOrDefault(p => p.Id == id);
            if (project is not null)
                _projectRepository.SaveHistories(project.Id, project.Histories);
        }
    }
}
