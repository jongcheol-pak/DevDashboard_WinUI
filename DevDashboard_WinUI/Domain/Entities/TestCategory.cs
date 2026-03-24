using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Domain.Entities;

/// <summary>
/// 테스트 카테고리 엔티티.
/// 기능 단위의 큰 묶음을 나타내며, 하위에 여러 TestItem을 소유합니다.
/// </summary>
public partial class TestCategory : ObservableObject
{
    /// <summary>카테고리 고유 식별자 (UUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>카테고리 이름 (예: "로그인 기능 테스트")</summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>카테고리에 속한 테스트 항목 목록</summary>
    public List<TestItem> Items { get; set; } = [];

    /// <summary>카테고리 등록 일시</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
