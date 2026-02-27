namespace DevDashboard.Models;

/// <summary>프로젝트 항목 모델</summary>
public class ProjectItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>프로젝트 아이콘 이미지 파일 경로</summary>
    public string IconPath { get; set; } = string.Empty;

    /// <summary>프로젝트 루트 경로</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>개발 도구 이름 (AppSettings.Tools에서 관리)</summary>
    public string DevToolName { get; set; } = string.Empty;

    /// <summary>개발 도구 실행 시 추가 옵션 (커맨드 라인 인수)</summary>
    public string Options { get; set; } = string.Empty;

    /// <summary>셸 도구(PowerShell/명령 프롬프트) 선택 시 실행할 명령어</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>셸 도구 실행 폴더 사용 여부</summary>
    public bool UseWorkingDirectory { get; set; }

    /// <summary>셸 도구 실행 폴더 경로</summary>
    public string ShellWorkingDirectory { get; set; } = string.Empty;

    /// <summary>기술 스택 태그 목록 (예: javascript, react, typescript)</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Git 상태 텍스트 (예: Clean, Modified, ...)</summary>
    public string GitStatus { get; set; } = string.Empty;

    public bool IsPinned { get; set; }

    /// <summary>핀 고정 카드의 표시 순서 (핀 안된 카드는 0)</summary>
    public int PinOrder { get; set; }

    /// <summary>관리자 권한으로 실행 여부</summary>
    public bool RunAsAdmin { get; set; }

    /// <summary>속한 그룹 Id (없으면 빈 문자열)</summary>
    public string GroupId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    /// <summary>커맨드 실행 버튼 스크립트 설정 (최대 4슬롯, null이면 미설정)</summary>
    public List<CommandScript?> CommandScripts { get; set; } = [null, null, null, null];

    /// <summary>To-Do 항목 목록</summary>
    public List<TodoItem> Todos { get; set; } = [];

    /// <summary>완료되지 않은 활성 To-Do 항목이 있는지 여부 — GetAll() 시 DB에서 설정</summary>
    public bool HasActiveTodo { get; set; }

    /// <summary>작업 기록 목록</summary>
    public List<HistoryEntry> Histories { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
