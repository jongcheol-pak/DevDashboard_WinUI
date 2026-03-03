using Microsoft.Data.Sqlite;

namespace DevDashboard.Infrastructure.Persistence;

/// <summary>
/// SQLite 데이터베이스 초기화 및 연결 관리.
/// 저장 위치: %LOCALAPPDATA%\Packages\[PackageFamilyName]\LocalState\projects.db
/// 테이블 생성, WAL 모드 활성화, 스키마 마이그레이션을 담당합니다.
/// </summary>
public sealed class DatabaseContext
{
    private static readonly string DbPath = Path.Combine(
        Windows.Storage.ApplicationData.Current.LocalFolder.Path,
        "projects.db");

    /// <summary>현재 사용 중인 DB 파일의 전체 경로를 반환합니다.</summary>
    public static string GetDbPath() => DbPath;

    private readonly string _connectionString;

    public DatabaseContext()
    {
        // Foreign Keys=True: 연결마다 PRAGMA foreign_keys = ON 자동 적용
        _connectionString = $"Data Source={DbPath};Foreign Keys=True";
        InitializeDatabase();
    }

    /// <summary>새 연결을 생성하여 반환합니다. 호출자가 using으로 해제해야 합니다.</summary>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>지정된 경로의 DB에 대한 연결을 생성합니다 (가져오기용).
    /// Pooling=False로 Dispose 시 파일 핸들이 즉시 해제됩니다.</summary>
    public static SqliteConnection CreateConnectionForPath(string dbPath)
    {
        var connection = new SqliteConnection($"Data Source={dbPath};Foreign Keys=True;Pooling=False");
        connection.Open();
        return connection;
    }

    /// <summary>현재 데이터베이스를 지정된 경로로 내보냅니다.
    /// VACUUM INTO를 사용해 WAL 데이터 포함 완전한 스냅샷을 단일 파일로 생성합니다.</summary>
    public static void ExportTo(string destPath)
    {
        // VACUUM INTO는 대상 파일이 이미 존재하면 오류를 발생시키므로 먼저 삭제
        if (File.Exists(destPath))
            File.Delete(destPath);

        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        // 경로 내 작은따옴표 이스케이프
        cmd.CommandText = $"VACUUM INTO '{destPath.Replace("'", "''")}'";
        cmd.ExecuteNonQuery();
    }

    /// <summary>테이블이 없으면 생성하고 스키마 마이그레이션을 실행합니다.</summary>
    private void InitializeDatabase()
    {
        using var connection = CreateConnection();

        // WAL 모드 활성화: 동시 읽기 성능 향상
        using (var walCmd = connection.CreateCommand())
        {
            walCmd.CommandText = "PRAGMA journal_mode=WAL;";
            walCmd.ExecuteNonQuery();
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Projects (
                Id          TEXT PRIMARY KEY,
                Name        TEXT NOT NULL DEFAULT '',
                Description TEXT NOT NULL DEFAULT '',
                IconPath    TEXT NOT NULL DEFAULT '',
                Path        TEXT NOT NULL DEFAULT '',
                DevToolName TEXT NOT NULL DEFAULT '',
                Options     TEXT NOT NULL DEFAULT '',
                Command     TEXT NOT NULL DEFAULT '',
                GitStatus   TEXT NOT NULL DEFAULT '',
                IsPinned    INTEGER NOT NULL DEFAULT 0,
                PinOrder    INTEGER NOT NULL DEFAULT 0,
                RunAsAdmin  INTEGER NOT NULL DEFAULT 0,
                GroupId     TEXT NOT NULL DEFAULT '',
                Category    TEXT NOT NULL DEFAULT '',
                CreatedAt   TEXT NOT NULL DEFAULT '',
                UseWorkingDirectory INTEGER NOT NULL DEFAULT 0,
                ShellWorkingDirectory TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS ProjectTags (
                ProjectId TEXT NOT NULL,
                Tag       TEXT NOT NULL,
                PRIMARY KEY (ProjectId, Tag),
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS CommandScripts (
                ProjectId         TEXT    NOT NULL,
                SlotIndex         INTEGER NOT NULL,
                Description       TEXT    NOT NULL DEFAULT '',
                ShellType         TEXT    NOT NULL DEFAULT 'Cmd',
                RunAsAdmin        INTEGER NOT NULL DEFAULT 0,
                Script            TEXT    NOT NULL DEFAULT '',
                UseWorkingDirectory INTEGER NOT NULL DEFAULT 0,
                WorkingDirectory  TEXT    NOT NULL DEFAULT '',
                IconSymbol        TEXT    NOT NULL DEFAULT '',
                PRIMARY KEY (ProjectId, SlotIndex),
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Todos (
                Id          TEXT PRIMARY KEY,
                ProjectId   TEXT NOT NULL,
                Text        TEXT NOT NULL DEFAULT '',
                Description TEXT NOT NULL DEFAULT '',
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                CompletedAt TEXT,
                CreatedAt   TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Histories (
                Id          TEXT PRIMARY KEY,
                ProjectId   TEXT NOT NULL,
                Title       TEXT NOT NULL DEFAULT '',
                Description TEXT NOT NULL DEFAULT '',
                CompletedAt TEXT NOT NULL DEFAULT '',
                CreatedAt   TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Groups (
                Id   TEXT PRIMARY KEY,
                Name TEXT NOT NULL DEFAULT ''
            );

            CREATE INDEX IF NOT EXISTS IX_Todos_ProjectId ON Todos(ProjectId);
            CREATE INDEX IF NOT EXISTS IX_Histories_ProjectId ON Histories(ProjectId);
            """;
        cmd.ExecuteNonQuery();
    }
}
