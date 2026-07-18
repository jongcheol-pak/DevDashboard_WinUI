using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>프로젝트 작업 기록 다이얼로그 뷰모델 — 모든 프로젝트의 작업 기록을 조회/관리합니다.
/// 공통 로직(검색·날짜그룹·CRUD·마크다운·페이지네이션·유형)은 베이스, 프로젝트 선택·다중 저장만 담당.</summary>
public sealed partial class ProjectHistoryDialogViewModel : HistoryDialogViewModelBase
{
    private readonly IProjectRepository _projectRepository;
    private readonly HashSet<string> _modifiedProjectIds = [];

    /// <summary>선택된 프로젝트</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddHistory))]
    public partial ProjectItem? SelectedProject { get; set; }

    /// <summary>ComboBox에 표시할 프로젝트 목록</summary>
    public ObservableCollection<ProjectItem> Projects { get; }

    /// <summary>프로젝트가 선택되어 기록 추가가 가능한지 여부</summary>
    public bool CanAddHistory => SelectedProject is not null;

    public ProjectHistoryDialogViewModel(List<ProjectItem> projects, IProjectRepository projectRepository, AppSettings settings)
        : base([], settings.PageSize, BuildKinds(settings))
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
        // 이전 프로젝트의 변경 사항을 모델에 반영한 뒤 새 프로젝트의 기록 로드
        SaveCurrentEntriesToModel(oldValue);
        SearchText = string.Empty;
        LoadEntries();
    }

    protected override string ExportProjectName => SelectedProject?.Name ?? string.Empty;

    /// <summary>기록 변경 시 현재 선택 프로젝트를 수정됨으로 표시합니다.</summary>
    protected override void OnEntriesModified()
    {
        if (SelectedProject is not null)
            _modifiedProjectIds.Add(SelectedProject.Id);
    }

    /// <summary>선택된 프로젝트의 기록을 베이스 목록으로 로드합니다.</summary>
    private void LoadEntries()
    {
        _entries.Clear();
        if (SelectedProject?.Histories is { } histories)
            _entries.AddRange(histories);
        CurrentPage = 1;
        RebuildGroups();
    }

    /// <summary>현재 편집 중인 기록을 프로젝트 모델에 반영합니다.</summary>
    private void SaveCurrentEntriesToModel(ProjectItem? project)
    {
        if (project is null) return;
        project.Histories = [.. _entries];
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
