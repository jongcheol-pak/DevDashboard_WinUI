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

    /// <summary>새 연결을 생성하여 반환합니다. 호출자가 using으로 해제해야 합니다.
    /// DB 파일이 삭제된 경우(설정에서 초기화 등) 테이블을 자동으로 재생성합니다.</summary>
    public SqliteConnection CreateConnection()
    {
        // DB 파일이 삭제된 후 Open()이 빈 파일을 새로 생성하므로, 사전에 감지하여 테이블 재초기화
        var needsInit = !File.Exists(DbPath);
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        if (needsInit)
            EnsureTablesCreated(connection);
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

    /// <summary>프로젝트 관련 테이블의 데이터만 삭제합니다 (LauncherItems 유지).</summary>
    public static void ClearProjectData()
    {
        using var conn = new SqliteConnection($"Data Source={DbPath};Foreign Keys=True");
        conn.Open();
        using var tx = conn.BeginTransaction();

        // FK CASCADE로 하위 테이블(ProjectTags, CommandScripts, Todos, Histories) 자동 삭제
        foreach (var table in new[] { "Projects", "Groups" })
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM {table}";
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    /// <summary>런처 항목 데이터만 삭제합니다 (프로젝트 데이터 유지).</summary>
    public static void ClearLauncherData()
    {
        using var conn = new SqliteConnection($"Data Source={DbPath};Foreign Keys=True");
        conn.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM LauncherItems";
        cmd.ExecuteNonQuery();
        tx.Commit();
    }

    /// <summary>앱 시작 시 테이블 초기화 + WAL 모드 활성화를 수행합니다.</summary>
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // WAL 모드 활성화: 동시 읽기 성능 향상
        using (var walCmd = connection.CreateCommand())
        {
            walCmd.CommandText = "PRAGMA journal_mode=WAL;";
            walCmd.ExecuteNonQuery();
        }

        EnsureTablesCreated(connection);
        MigrateSchema(connection);
    }

    /// <summary>기존 테이블에 누락된 컬럼을 추가합니다. 이미 존재하면 무시됩니다.</summary>
    private static void MigrateSchema(SqliteConnection connection)
    {
        AddColumnIfNotExists(connection, "Groups", "IsDefault", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfNotExists(connection, "TestItems", "CategoryId", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfNotExists(connection, "TestItems", "Status", "TEXT NOT NULL DEFAULT 'Testing'");
        MigrateIsCompletedToStatus(connection);
    }

    /// <summary>기존 IsCompleted 값을 Status 컬럼으로 마이그레이션합니다.</summary>
    private static void MigrateIsCompletedToStatus(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE TestItems SET Status = 'Done' WHERE IsCompleted = 1 AND Status = 'Testing'";
        cmd.ExecuteNonQuery();
    }

    /// <summary>허용된 테이블/컬럼 이름 — SQL Injection 방지용 화이트리스트</summary>
    private static readonly HashSet<string> AllowedIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Projects", "ProjectTags", "CommandScripts", "Todos", "Histories", "Groups", "LauncherItems", "TestItems", "TestCategories",
        "Id", "Name", "Description", "IconPath", "Path", "DevToolName", "Options",
        "Command", "GitStatus", "IsPinned", "PinOrder", "RunAsAdmin", "GroupId",
        "Category", "CreatedAt", "UseWorkingDirectory", "ShellWorkingDirectory",
        "ProjectId", "Tag", "SlotIndex", "ShellType", "Script", "WorkingDirectory",
        "IconSymbol", "Text", "IsCompleted", "CompletedAt", "Title", "IsDefault",
        "DisplayName", "ExecutablePath", "IconCachePath", "SortOrder", "ProgressNote", "CategoryId", "Status"
    };

    private static void AddColumnIfNotExists(SqliteConnection connection, string table, string column, string definition)
    {
        // 화이트리스트 검증으로 SQL Injection 방지
        if (!AllowedIdentifiers.Contains(table))
            throw new ArgumentException($"허용되지 않은 테이블 이름: {table}", nameof(table));
        if (!AllowedIdentifiers.Contains(column))
            throw new ArgumentException($"허용되지 않은 컬럼 이름: {column}", nameof(column));

        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}'";
        var exists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;
        if (exists) return;

        using var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        alterCmd.ExecuteNonQuery();
    }

    /// <summary>테이블이 없으면 생성합니다. CREATE TABLE IF NOT EXISTS를 사용하므로 멱등성이 보장됩니다.</summary>
    private static void EnsureTablesCreated(SqliteConnection connection)
    {
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
                Id        TEXT PRIMARY KEY,
                Name      TEXT NOT NULL DEFAULT '',
                IsDefault INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS LauncherItems (
                Id             TEXT PRIMARY KEY,
                DisplayName    TEXT NOT NULL DEFAULT '',
                ExecutablePath TEXT NOT NULL DEFAULT '',
                IconCachePath  TEXT NOT NULL DEFAULT '',
                SortOrder      INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS TestCategories (
                Id        TEXT PRIMARY KEY,
                ProjectId TEXT NOT NULL,
                Name      TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS TestItems (
                Id           TEXT PRIMARY KEY,
                CategoryId   TEXT NOT NULL DEFAULT '',
                ProjectId    TEXT NOT NULL,
                Text         TEXT NOT NULL DEFAULT '',
                ProgressNote TEXT NOT NULL DEFAULT '',
                IsCompleted  INTEGER NOT NULL DEFAULT 0,
                Status       TEXT NOT NULL DEFAULT 'Testing',
                CompletedAt  TEXT,
                CreatedAt    TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Todos_ProjectId ON Todos(ProjectId);
            CREATE INDEX IF NOT EXISTS IX_Histories_ProjectId ON Histories(ProjectId);
            CREATE INDEX IF NOT EXISTS IX_TestItems_ProjectId ON TestItems(ProjectId);
            CREATE INDEX IF NOT EXISTS IX_TestCategories_ProjectId ON TestCategories(ProjectId);
            """;
        cmd.ExecuteNonQuery();
    }
}
