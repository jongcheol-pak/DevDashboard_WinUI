using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Services;
using Microsoft.UI.Xaml;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>페이지 이동 버튼 한 칸 — 번호 버튼이거나 건너뛴 구간을 알리는 생략 표식입니다.</summary>
public sealed class HistoryPageButton
{
    /// <summary>이동할 페이지 번호 (생략 표식이면 0)</summary>
    public int Page { get; private init; }

    /// <summary>생략 표식(…) 여부</summary>
    public bool IsEllipsis { get; private init; }

    /// <summary>현재 보고 있는 페이지인지 여부 (강조 표시용)</summary>
    public bool IsCurrent { get; private init; }

    /// <summary>버튼에 표시할 문자열</summary>
    public string Text => IsEllipsis ? "…" : Page.ToString();

    public static HistoryPageButton Number(int page, bool isCurrent)
        => new() { Page = page, IsCurrent = isCurrent };

    public static HistoryPageButton Ellipsis() => new() { IsEllipsis = true };
}

/// <summary>날짜별 그룹 항목 (날짜 헤더 + 해당 날짜의 기록 목록)</summary>
public partial class HistoryDateGroup : ObservableObject
{
    /// <summary>그룹 날짜 (yyyy-MM-dd)</summary>
    public DateTime Date { get; }

    /// <summary>표시용 날짜 문자열</summary>
    public string DateText => Date.ToString("yyyy-MM-dd");

    /// <summary>그룹 헤더에 표시할 건수 ("N건")</summary>
    public string CountText => string.Format(LocalizationService.Get("HistoryDialog_CountFormat"), Entries.Count);

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
    public string CompletedAtText => Model.CompletedAt.ToString("yyyy-MM-dd HH:mm");
    public string CreatedAtText => Model.CreatedAt.ToString("yyyy-MM-dd HH:mm");

    /// <summary>행에 표시할 완료 시각 (HH:mm)</summary>
    public string TimeText => FormatTime(Model);

    /// <summary>기록의 표시용 시각을 만듭니다 (HH:mm).
    /// 시각 필드 도입 전에 만들어진 기록은 CompletedAt이 00:00이라 등록 일시의 시각으로 대신 보여줍니다
    /// (기존 데이터를 변형하지 않고도 의미 있는 시각을 표시하기 위함).
    /// 목록과 내보내기가 같은 시각을 보여주도록 두 경로가 이 메서드를 공유합니다.</summary>
    public static string FormatTime(HistoryEntry entry) => entry.CompletedAt.TimeOfDay == TimeSpan.Zero
        ? entry.CreatedAt.ToString("HH:mm")
        : entry.CompletedAt.ToString("HH:mm");

    /// <summary>행에 표시할 프로젝트명 (생성 시 주입)</summary>
    public string ProjectName { get; }

    /// <summary>작업 기록 유형 (배지 텍스트)</summary>
    public string Kind => Model.Kind;

    /// <summary>유형 배지 표시 여부 (유형이 지정된 경우만)</summary>
    public Visibility KindVisibility => string.IsNullOrWhiteSpace(Model.Kind) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>상세 정보 표시 여부</summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>상세 정보 존재 여부</summary>
    public bool HasDescription => !string.IsNullOrWhiteSpace(Model.Description);

    /// <summary>상세 정보 표시 가시성 (펼쳐진 경우 + 설명이 있는 경우)</summary>
    public Visibility DescriptionVisibility => (HasDescription && IsExpanded) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>상세 영역 전체 가시성 (펼쳐진 경우)</summary>
    public Visibility ExpandedVisibility => IsExpanded ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>펼침/접힘 화살표 글리프 (Segoe MDL2 Assets — ChevronDown / ChevronRight)</summary>
    public string ExpandChevron => IsExpanded ? "\uE70D" : "\uE76C";

    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(DescriptionVisibility));
        OnPropertyChanged(nameof(ExpandedVisibility));
        OnPropertyChanged(nameof(ExpandChevron));
    }

    /// <summary>삭제 버튼 툴팁 ({x:Bind} 전용 — DataTemplate 내 x:Uid ToolTipService 패턴 대체)</summary>
    public string DeleteTooltip => LocalizationService.Get("IconDeleteBtn_Tooltip");

    /// <summary>수정 버튼 툴팁 ({x:Bind} 전용)</summary>
    public string EditTooltip => LocalizationService.Get("IconEditBtn_Tooltip");

    public HistoryEntryViewModel(HistoryEntry model, string projectName)
    {
        Model = model;
        ProjectName = projectName;
    }

    /// <summary>펼치기/접기 토글</summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}

/// <summary>작업 기록 다이얼로그 뷰모델 (카드별 단일 프로젝트) — 공통 로직은 베이스, 저장·세션 신규 항목만 담당.</summary>
public sealed class HistoryDialogViewModel : HistoryDialogViewModelBase
{
    private readonly ProjectItem _projectItem;

    /// <summary>이 세션에서 새로 추가된 항목 목록 (완료 훅에서 프로젝트에 병합)</summary>
    public List<HistoryEntry> NewEntries { get; } = [];

    /// <summary>프로젝트 이름</summary>
    public string ProjectName => _projectItem.Name;

    public HistoryDialogViewModel(ProjectItem projectItem, AppSettings settings)
        : base([.. (projectItem.Histories ?? [])], settings.PageSize, BuildKinds(settings), projectItem.Name)
    {
        _projectItem = projectItem;
    }

    protected override void OnEntryAdded(HistoryEntry entry) => NewEntries.Add(entry);

    // 단일 프로젝트는 다이얼로그 종료 시 SaveToModel로 일괄 반영하므로 변경 시점 추가 처리 불필요
    protected override void OnEntriesModified() { }

    protected override string ExportProjectName => _projectItem.Name;

    /// <summary>변경 사항을 모델에 반영합니다.</summary>
    public void SaveToModel()
    {
        _projectItem.Histories = [.. _entries];
    }
}
