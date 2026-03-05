namespace DevDashboard.Domain.ValueObjects;

/// <summary>
/// 프로젝트 그룹 값 객체.
/// 식별자(Id)로 동일성을 판단하는 단순 그룹 정보를 표현합니다.
/// record 타입으로 불변성을 보장하며, with 식으로 변형 복사가 가능합니다.
/// </summary>
public record ProjectGroup
{
    /// <summary>그룹 고유 식별자 (UUID)</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>그룹 표시 이름</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>시스템 기본 그룹 여부 — true이면 삭제 불가</summary>
    public bool IsDefault { get; init; }
}
