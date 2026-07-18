namespace DevDashboard.Domain.Enums;

/// <summary>카드 목록 정렬 기준</summary>
public enum SortOrder
{
    /// <summary>이름 순</summary>
    Name,

    /// <summary>카테고리 순</summary>
    Category,

    /// <summary>등록일 순</summary>
    CreatedAt
}

/// <summary>앱 테마 모드</summary>
public enum ThemeMode
{
    /// <summary>밝은 테마</summary>
    Light,

    /// <summary>어두운 테마</summary>
    Dark,

    /// <summary>Windows 시스템 테마 따름</summary>
    System
}

/// <summary>커맨드 스크립트 셸 타입</summary>
public enum ShellType
{
    /// <summary>명령 프롬프트 (cmd.exe)</summary>
    Cmd,

    /// <summary>PowerShell</summary>
    PowerShell
}

/// <summary>작업(칸반) 우선순위</summary>
public enum TaskPriority
{
    /// <summary>높음</summary>
    High,

    /// <summary>보통 (기본)</summary>
    Normal,

    /// <summary>낮음</summary>
    Low
}
