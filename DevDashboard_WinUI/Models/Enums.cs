namespace DevDashboard.Models;

/// <summary>카드 목록 정렬 기준</summary>
public enum SortOrder
{
    Name,
    Category,
    CreatedAt
}

/// <summary>대시보드 뷰 모드</summary>
public enum ViewMode
{
    Grid,
    List
}

/// <summary>앱 테마 모드</summary>
public enum ThemeMode
{
    Light,
    Dark,
    System
}
