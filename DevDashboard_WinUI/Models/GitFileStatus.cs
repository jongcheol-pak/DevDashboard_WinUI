namespace DevDashboard.Models;

/// <summary>git status --porcelain 파싱 결과 — 파일 변경 상태</summary>
public record GitFileStatus(string StatusCode, string FilePath)
{
    /// <summary>Index 영역(첫 글자) 상태 코드</summary>
    public char IndexCode => StatusCode.Length > 0 ? StatusCode[0] : ' ';

    /// <summary>WorkTree 영역(둘째 글자) 상태 코드</summary>
    public char WorkTreeCode => StatusCode.Length > 1 ? StatusCode[1] : ' ';

    /// <summary>Index 상태 레이블</summary>
    public string IndexLabel => ToLabel(IndexCode);

    /// <summary>WorkTree 상태 레이블</summary>
    public string WorkTreeLabel => ToLabel(WorkTreeCode);

    /// <summary>기존 UI 호환용 상태 레이블</summary>
    public string StatusLabel => WorkTreeCode == ' ' ? IndexLabel : WorkTreeLabel;

    /// <summary>Index 색상 분류</summary>
    public GitFileStatusKind IndexKind => ToKind(IndexCode);

    /// <summary>WorkTree 색상 분류</summary>
    public GitFileStatusKind WorkTreeKind => ToKind(WorkTreeCode);

    /// <summary>기존 UI 호환용 색상 분류</summary>
    public GitFileStatusKind Kind => WorkTreeCode == ' ' ? IndexKind : WorkTreeKind;

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

    /// <summary>색상 분류 — UI 브러시 선택에 사용</summary>
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
    None,
    Modified,
    Added,
    Deleted,
    Untracked,
}
