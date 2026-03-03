namespace DevDashboard.Domain.ValueObjects;

/// <summary>
/// git 커밋 정보를 담는 불변 값 객체.
/// </summary>
public record GitCommit(
    string Hash,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset Date,
    string Message)
{
    /// <summary>7자리 단축 해시</summary>
    public string ShortHash => Hash.Length >= 7 ? Hash[..7] : Hash;
}
