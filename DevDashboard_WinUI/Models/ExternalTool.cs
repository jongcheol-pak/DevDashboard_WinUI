namespace DevDashboard.Models;

/// <summary>사용자 정의 외부 도구 설정</summary>
public class ExternalTool
{
    /// <summary>도구 표시 이름</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>실행 파일 경로</summary>
    public string ExecutablePath { get; set; } = string.Empty;
}
