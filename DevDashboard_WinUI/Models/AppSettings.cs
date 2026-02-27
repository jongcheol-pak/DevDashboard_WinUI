namespace DevDashboard.Models;

/// <summary>앱 전체 설정 루트 모델 — settings.json 최상위 객체</summary>
public class AppSettings
{
    /// <summary>Windows 부팅 시 자동 실행 여부</summary>
    public bool RunOnStartup { get; set; }

    /// <summary>앱 테마 설정 (Light / Dark / System)</summary>
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    public SortOrder SortOrder { get; set; } = SortOrder.Name;

    public ViewMode ViewMode { get; set; } = ViewMode.Grid;

    public List<ProjectGroup> Groups { get; set; } = [];

    /// <summary>사용자 정의 외부 도구 목록</summary>
    public List<ExternalTool> Tools { get; set; } = [];

    /// <summary>기술 스택 태그 목록</summary>
    public List<string> TechStackTags { get; set; } = [];

    /// <summary>카테고리 목록</summary>
    public List<string> Categories { get; set; } = [];

    /// <summary>앱 언어 설정 (SystemDefault / Korean / English)</summary>
    public LanguageSetting Language { get; set; } = LanguageSetting.SystemDefault;

    /// <summary>마지막으로 선택된 그룹 탭 ID (null이면 전체 탭)</summary>
    public string? SelectedGroupId { get; set; }

    /// <summary>진행 중인 To-Do 완료 시 작업 기록 팝업 표시 여부</summary>
    public bool ShowWorkLogPopupOnTodoComplete { get; set; }
}
