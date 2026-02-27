namespace DevDashboard.Models;

/// <summary>커맨드 스크립트 셸 타입</summary>
public enum ShellType
{
    Cmd,
    PowerShell
}

/// <summary>커맨드 실행 버튼에 설정되는 스크립트 정보</summary>
public class CommandScript
{
    /// <summary>스크립트 설명 (버튼 툴팁에 표시)</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>실행할 셸 타입</summary>
    public ShellType ShellType { get; set; } = ShellType.Cmd;

    /// <summary>관리자 권한으로 실행 여부</summary>
    public bool RunAsAdmin { get; set; }

    /// <summary>실행할 스크립트 내용</summary>
    public string Script { get; set; } = string.Empty;

    /// <summary>실행 폴더 사용 여부</summary>
    public bool UseWorkingDirectory { get; set; }

    /// <summary>실행 폴더 경로</summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>커맨드 버튼에 표시할 아이콘 심볼 이름 (빈 문자열이면 기본 ">_" 텍스트 표시)</summary>
    public string IconSymbol { get; set; } = string.Empty;
}
