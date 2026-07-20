using CommunityToolkit.Mvvm.ComponentModel;
using DevDashboard.Infrastructure.Services;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 새 작업/작업 편집 다이얼로그 뷰모델 — 제목·설명·카테고리·우선순위·시작/종료일과 "테스트 추가" 토글을 편집합니다.
/// </summary>
public partial class TaskEditDialogViewModel : ObservableObject
{
    private readonly TodoItem? _existing;

    /// <summary>"미분류" 카테고리 표시 라벨 (내부 저장은 빈 문자열)</summary>
    private readonly string _noneCategoryLabel;

    [ObservableProperty] public partial string Title { get; set; } = string.Empty;
    [ObservableProperty] public partial string Description { get; set; } = string.Empty;
    [ObservableProperty] public partial string SelectedCategoryOption { get; set; } = string.Empty;
    [ObservableProperty] public partial TaskPriorityItem SelectedPriority { get; set; }
    [ObservableProperty] public partial DateTimeOffset? StartDate { get; set; }
    [ObservableProperty] public partial DateTimeOffset? EndDate { get; set; }

    /// <summary>"테스트 추가" 토글 (FR-T6 — 새 작업 시 테스트 목록에도 등록). 실제 생성은 T9에서 처리.</summary>
    [ObservableProperty] public partial bool AddTest { get; set; }

    /// <summary>카테고리 선택 목록 (미분류 + 기본 + 사용자 정의)</summary>
    public IReadOnlyList<string> CategoryOptions { get; }

    /// <summary>우선순위 선택 목록 (높음/보통/낮음)</summary>
    public IReadOnlyList<TaskPriorityItem> Priorities { get; }

    /// <summary>편집 모드 여부 (false이면 새 작업)</summary>
    public bool IsEditMode => _existing is not null;

    /// <summary>테스트 추가 토글 표시 여부 — 새 작업일 때만 (편집 시 숨김)</summary>
    public bool ShowTestToggle => !IsEditMode;

    /// <summary>다이얼로그 헤더 제목 ("새 작업" / "작업 편집")</summary>
    public string HeaderTitle => LocalizationService.Get(IsEditMode ? "TaskEdit_TitleEdit" : "TaskEdit_TitleAdd");

    /// <summary>헤더 상태 배지 문구 — 다이얼로그를 연 시점의 상태 스냅샷 (수명이 짧아 갱신하지 않는다)</summary>
    public string StatusLabel { get; }

    /// <summary>기본 버튼 문구 — 새 작업은 "등록", 편집은 기존 공용 "저장"</summary>
    public string PrimaryButtonLabel =>
        LocalizationService.Get(IsEditMode ? "Dialog_Save" : "TaskEdit_Submit");

    /// <param name="status">
    /// 헤더 배지에 표시할 상태. 새 작업은 호출한 칸반 열의 상태를 넘긴다.
    /// 편집 모드는 이 값을 무시하고 기존 항목의 상태를 쓴다.
    /// </param>
    public TaskEditDialogViewModel(TodoItem? existing, AppSettings settings, TodoStatus status = TodoStatus.Waiting)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _existing = existing;
        _noneCategoryLabel = LocalizationService.Get("TaskCategory_None");
        StatusLabel = StatusLabelFor(existing?.Status ?? status);

        CategoryOptions = new[] { _noneCategoryLabel }
            .Concat(AppSettingsDialogViewModel.DefaultTaskCategories)
            .Concat(settings.TaskCategories)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Priorities =
        [
            new(TaskPriority.High, LocalizationService.Get("TaskPriority_High")),
            new(TaskPriority.Normal, LocalizationService.Get("TaskPriority_Normal")),
            new(TaskPriority.Low, LocalizationService.Get("TaskPriority_Low")),
        ];

        if (existing is not null)
        {
            Title = existing.Text;
            Description = existing.Description;
            SelectedCategoryOption = string.IsNullOrEmpty(existing.Category) ? _noneCategoryLabel : existing.Category;
            SelectedPriority = Priorities.First(p => p.Value == existing.Priority);
            StartDate = existing.StartDate.HasValue ? new DateTimeOffset(existing.StartDate.Value) : null;
            EndDate = existing.EndDate.HasValue ? new DateTimeOffset(existing.EndDate.Value) : null;
        }
        else
        {
            SelectedCategoryOption = _noneCategoryLabel;
            SelectedPriority = Priorities.First(p => p.Value == TaskPriority.Normal);
            // 새 작업은 시작일을 오늘로 미리 채운다(시안 기준). 시각을 버려 저장값에 잔여 시각이 남지 않게 한다.
            StartDate = new DateTimeOffset(DateTime.Today);
        }
    }

    /// <summary>상태 → 표시 문구. 칸반 열 헤더와 같은 리소스 키를 쓴다.</summary>
    private static string StatusLabelFor(TodoStatus status) => status switch
    {
        TodoStatus.Active => LocalizationService.Get("TaskStatus_Active"),
        TodoStatus.Completed => LocalizationService.Get("TaskStatus_Completed"),
        TodoStatus.Hold => LocalizationService.Get("TaskStatus_Hold"),
        _ => LocalizationService.Get("TaskStatus_Waiting"),
    };

    /// <summary>입력값을 TodoItem에 반영합니다. 편집이면 기존 항목을, 새 작업이면 새 항목을 반환합니다.</summary>
    public TodoItem BuildResult()
    {
        var todo = _existing ?? new TodoItem { Status = TodoStatus.Waiting };
        todo.Text = Title.Trim();
        todo.Description = Description?.Trim() ?? string.Empty;
        todo.Category = SelectedCategoryOption == _noneCategoryLabel ? string.Empty : SelectedCategoryOption;
        todo.Priority = SelectedPriority.Value;
        todo.StartDate = StartDate?.DateTime;
        todo.EndDate = EndDate?.DateTime;
        return todo;
    }
}

/// <summary>우선순위 콤보박스 항목</summary>
public sealed record TaskPriorityItem(TaskPriority Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
