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

    /// <summary>커밋 메시지 첫 줄 — 목록에 제목으로 표시합니다.</summary>
    /// <remarks>
    /// 본문(첫 줄 이후)만 따로 떼는 프로퍼티는 두지 않는다 — 상세 화면이 제목·본문을
    /// 나누지 않고 <see cref="Message"/> 전문을 한 덩어리로 그리므로 소비처가 없다.
    /// </remarks>
    public string Subject
    {
        get
        {
            var end = Message.IndexOf('\n');
            return (end < 0 ? Message : Message[..end]).Trim();
        }
    }
}
