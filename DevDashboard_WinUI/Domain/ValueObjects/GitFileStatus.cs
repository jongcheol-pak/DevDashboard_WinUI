namespace DevDashboard.Domain.ValueObjects;

/// <summary>
/// git 커밋/파일 변경 상태 값 객체.
/// record 타입으로 불변성을 보장하며, UI 바인딩에 사용됩니다.
/// </summary>
public record GitFileStatus(string StatusCode, string FilePath)
{
    /// <summary>Index 영역(첫 글자) 상태 코드</summary>
    public char IndexCode => StatusCode.Length > 0 ? StatusCode[0] : ' ';

    /// <summary>WorkTree 영역(둘째 글자) 상태 코드</summary>
    public char WorkTreeCode => StatusCode.Length > 1 ? StatusCode[1] : ' ';

    /// <summary>Index 상태 레이블 (한글 표시용)</summary>
    public string IndexLabel => ToLabel(IndexCode);

    /// <summary>WorkTree 상태 레이블 (한글 표시용)</summary>
    public string WorkTreeLabel => ToLabel(WorkTreeCode);

    /// <summary>기존 UI 호환용 상태 레이블 — WorkTree가 공백이면 Index 레이블 사용</summary>
    public string StatusLabel => WorkTreeCode == ' ' ? IndexLabel : WorkTreeLabel;

    /// <summary>Index 색상 분류 — UI 브러시 선택에 사용</summary>
    public GitFileStatusKind IndexKind => ToKind(IndexCode);

    /// <summary>WorkTree 색상 분류 — UI 브러시 선택에 사용</summary>
    public GitFileStatusKind WorkTreeKind => ToKind(WorkTreeCode);

    /// <summary>기존 UI 호환용 색상 분류 — WorkTree가 공백이면 Index 분류 사용</summary>
    public GitFileStatusKind Kind => WorkTreeCode == ' ' ? IndexKind : WorkTreeKind;

    /// <summary>상태 코드 문자를 사람이 읽을 수 있는 레이블로 변환합니다.</summary>
    private static string ToLabel(char code) => code switch
    {
        'M' => "Modified",
        'A' => "Added",
        'D' => "Deleted",
        'R' => "Renamed",
        'C' => "Copied",
        'U' => "Updated",
        '?' => "Untracked",
        '!' => "Ignored",
        ' ' => "-",
        _ => code.ToString(),
    };

    /// <summary>상태 코드 문자를 UI 색상 분류로 변환합니다.</summary>
    private static GitFileStatusKind ToKind(char code) => code switch
    {
        '?' or '!' => GitFileStatusKind.Untracked,
        'D' => GitFileStatusKind.Deleted,
        'A' => GitFileStatusKind.Added,
        ' ' => GitFileStatusKind.None,
        _ => GitFileStatusKind.Modified,
    };
}

/// <summary>변경 파일 상태 분류 — UI 색상 구분 기준</summary>
public enum GitFileStatusKind
{
    /// <summary>변경 없음</summary>
    None,

    /// <summary>수정됨</summary>
    Modified,

    /// <summary>새로 추가됨</summary>
    Added,

    /// <summary>삭제됨</summary>
    Deleted,

    /// <summary>추적되지 않음 또는 무시됨</summary>
    Untracked,
}
