namespace DevDashboard.Domain.ValueObjects;

/// <summary>
/// 오픈소스 라이브러리 정보 값 객체.
/// 앱 정보 화면에서 사용하는 라이선스 목록 항목을 표현합니다.
/// </summary>
public record OpenSourceItem(string Name, string License, string Url);
