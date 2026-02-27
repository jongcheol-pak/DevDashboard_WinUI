namespace DevDashboard.Models;

/// <summary>앱 언어 설정</summary>
public enum LanguageSetting
{
    /// <summary>Windows 시스템 언어 사용 (한국어면 한국어, 그 외 영어)</summary>
    SystemDefault,

    /// <summary>한국어 고정</summary>
    Korean,

    /// <summary>영어 고정</summary>
    English
}
