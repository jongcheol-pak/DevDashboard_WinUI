namespace DevDashboard.Domain.Entities;

/// <summary>
/// 사용자 정의 외부 도구 설정 엔티티.
/// AppSettings의 자식 엔티티로, 개발 도구 목록을 구성합니다.
/// </summary>
public class ExternalTool
{
    /// <summary>도구 표시 이름</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>실행 파일 경로</summary>
    public string ExecutablePath { get; set; } = string.Empty;
}
