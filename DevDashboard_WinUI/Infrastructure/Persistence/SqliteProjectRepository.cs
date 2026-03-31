using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DevDashboard.Infrastructure.Persistence;

/// <summary>
/// SQLite 기반 프로젝트 저장소 구현체.
/// IProjectRepository 인터페이스를 구현하며, DatabaseContext를 통해 SQLite에 접근합니다.
/// Todos/Histories는 성능을 위해 지연 로딩을 적용합니다.
/// </summary>
public sealed class SqliteProjectRepository : IProjectRepository
{
    /// <summary>DateTime ISO 8601 직렬화 포맷</summary>
    private const string DateTimeFormat = "o";

    private readonly DatabaseContext _db;

    public SqliteProjectRepository(DatabaseContext db)
    {
        _db = db;
    }

    // ===== 전체 조회 =====

    public List<ProjectItem> GetAll()
    {
        using var conn = _db.CreateConnection();

        var projects = ReadProjects(conn);
        var tagsMap = ReadAllTags(conn);
        var scriptsMap = ReadAllCommandScripts(conn);
        var activeSet = ReadActiveProjectIds(conn);
        var activeTestSet = ReadActiveTestProjectIds(conn);

        // Todos/Histories/TestCategories는 지연 로딩 — 다이얼로그 열기 시 GetTodos/GetHistories/GetTestCategories로 로드
        foreach (var p in projects)
        {
            p.Tags = tagsMap.TryGetValue(p.Id, out var tags) ? tags : [];
            p.CommandScripts = scriptsMap.TryGetValue(p.Id, out var scripts) ? scripts : [null, null, null, null];
            p.HasActiveTodo = activeSet.Contains(p.Id);
            p.HasActiveTest = activeTestSet.Contains(p.Id);
        }

        return projects;
    }

    /// <summary>완료되지 않은 To-Do가 있는 프로젝트 ID 집합을 반환합니다.</summary>
    private static HashSet<string> ReadActiveProjectIds(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT ProjectId FROM Todos WHERE IsCompleted = 0";
        using var reader = cmd.ExecuteReader();

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            set.Add(reader.GetString(0));
        return set;
    }

    /// <summary>완료되지 않은 테스트 항목이 있는 프로젝트 ID 집합을 반환합니다.</summary>
    private static HashSet<string> ReadActiveTestProjectIds(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT ProjectId FROM TestItems WHERE Status != 'Done'";
        using var reader = cmd.ExecuteReader();

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            set.Add(reader.GetString(0));
        return set;
    }

    /// <inheritdoc />
    public List<TestCategory> GetTestCategories(string projectId)
    {
        using var conn = _db.CreateConnection();
        return ReadTestCategoriesForProject(conn, projectId);
    }

    /// <inheritdoc />
    public List<TodoItem> GetTodos(string projectId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Todos WHERE ProjectId = @pid ORDER BY CreatedAt";
        cmd.Parameters.AddWithValue("@pid", projectId);
        using var reader = cmd.ExecuteReader();

        var list = new List<TodoItem>();
        var hasStatus = HasColumn(reader, "Status");
        while (reader.Read())
        {
            var completedAtStr = reader.IsDBNull(reader.GetOrdinal("CompletedAt"))
                ? null
                : reader.GetString(reader.GetOrdinal("CompletedAt"));
            var isCompleted = reader.GetInt64(reader.GetOrdinal("IsCompleted")) != 0;

            list.Add(new TodoItem
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                Text = reader.GetString(reader.GetOrdinal("Text")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                IsCompleted = isCompleted,
                Status = hasStatus && Enum.TryParse<TodoStatus>(reader.GetString(reader.GetOrdinal("Status")), out var s)
                    ? s
                    : isCompleted ? TodoStatus.Completed : TodoStatus.Waiting,
                CompletedAt = string.IsNullOrEmpty(completedAtStr) ? null : ParseDateTime(completedAtStr),
                CreatedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("CreatedAt")))
            });
        }

        return list;
    }

    /// <inheritdoc />
    public List<HistoryEntry> GetHistories(string projectId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Histories WHERE ProjectId = @pid ORDER BY CompletedAt DESC, CreatedAt DESC";
        cmd.Parameters.AddWithValue("@pid", projectId);
        using var reader = cmd.ExecuteReader();

        var list = new List<HistoryEntry>();
        while (reader.Read())
        {
            list.Add(new HistoryEntry
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                CompletedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("CompletedAt"))),
                CreatedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("CreatedAt")))
            });
        }

        return list;
    }

    // ===== 추가 =====

    public void Add(ProjectItem project)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        InsertProject(conn, project);
        InsertTags(conn, project.Id, project.Tags);
        InsertCommandScripts(conn, project.Id, project.CommandScripts);
        InsertTodos(conn, project.Id, project.Todos);
        InsertHistories(conn, project.Id, project.Histories);
        InsertTestCategories(conn, project.Id, project.TestCategories);

        tx.Commit();
    }

    // ===== 갱신 =====

    public void Update(ProjectItem project)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        UpdateProject(conn, project);

        // Tags: 삭제 후 재삽입
        DeleteByProjectId(conn, "ProjectTags", project.Id);
        InsertTags(conn, project.Id, project.Tags);

        // CommandScripts: 삭제 후 재삽입
        DeleteByProjectId(conn, "CommandScripts", project.Id);
        InsertCommandScripts(conn, project.Id, project.CommandScripts);

        tx.Commit();
    }

    // ===== 삭제 =====

    public void Delete(string projectId)
    {
        using var conn = _db.CreateConnection();

        // CASCADE 제약으로 하위 테이블(Tags, Scripts, Todos, Histories) 자동 삭제
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Projects WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", projectId);
        cmd.ExecuteNonQuery();
    }

    // ===== 부분 저장 =====

    public void SaveTodos(string projectId, List<TodoItem> todos)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        DeleteByProjectId(conn, "Todos", projectId);
        InsertTodos(conn, projectId, todos);

        tx.Commit();
    }

    public void SaveHistories(string projectId, List<HistoryEntry> histories)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        DeleteByProjectId(conn, "Histories", projectId);
        InsertHistories(conn, projectId, histories);

        tx.Commit();
    }

    public void SaveTestCategories(string projectId, List<TestCategory> categories)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        DeleteByProjectId(conn, "TestItems", projectId);
        DeleteByProjectId(conn, "TestCategories", projectId);
        InsertTestCategories(conn, projectId, categories);

        tx.Commit();
    }

    public void SaveCommandScripts(string projectId, List<CommandScript?> scripts)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        DeleteByProjectId(conn, "CommandScripts", projectId);
        InsertCommandScripts(conn, projectId, scripts);

        tx.Commit();
    }

    public void UpdatePinned(string projectId, bool isPinned)
    {
        using var conn = _db.CreateConnection();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Projects SET IsPinned = @pinned WHERE Id = @id";
        cmd.Parameters.AddWithValue("@pinned", isPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@id", projectId);
        cmd.ExecuteNonQuery();
    }

    public void UpdatePinOrder(IReadOnlyList<string> orderedPinnedIds)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Projects SET PinOrder = @order WHERE Id = @id";
        cmd.Parameters.AddWithValue("@order", 0);
        cmd.Parameters.AddWithValue("@id", string.Empty);

        for (var i = 0; i < orderedPinnedIds.Count; i++)
        {
            cmd.Parameters["@order"].Value = i + 1;
            cmd.Parameters["@id"].Value = orderedPinnedIds[i];
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    // ===== 마이그레이션 지원 =====

    /// <summary>여러 프로젝트를 일괄 추가합니다 (JSON 마이그레이션용).</summary>
    public void BulkInsert(List<ProjectItem> projects)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        foreach (var p in projects)
        {
            InsertProject(conn, p);
            InsertTags(conn, p.Id, p.Tags);
            InsertCommandScripts(conn, p.Id, p.CommandScripts);
            InsertTodos(conn, p.Id, p.Todos);
            InsertHistories(conn, p.Id, p.Histories);
            InsertTestCategories(conn, p.Id, p.TestCategories);
        }

        tx.Commit();
    }

    /// <summary>프로젝트가 하나라도 있는지 확인합니다.</summary>
    public bool HasAnyProjects()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Projects";
        var count = Convert.ToInt64(cmd.ExecuteScalar());
        return count > 0;
    }

    // ===== 내부 헬퍼: 읽기 =====

    private static List<ProjectItem> ReadProjects(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Projects ORDER BY IsPinned DESC, PinOrder, CreatedAt";
        using var reader = cmd.ExecuteReader();

        var list = new List<ProjectItem>();
        while (reader.Read())
        {
            list.Add(new ProjectItem
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                IconPath = reader.GetString(reader.GetOrdinal("IconPath")),
                Path = reader.GetString(reader.GetOrdinal("Path")),
                DevToolName = reader.GetString(reader.GetOrdinal("DevToolName")),
                Options = reader.GetString(reader.GetOrdinal("Options")),
                Command = reader.GetString(reader.GetOrdinal("Command")),
                GitStatus = reader.GetString(reader.GetOrdinal("GitStatus")),
                IsPinned = reader.GetInt64(reader.GetOrdinal("IsPinned")) != 0,
                PinOrder = (int)reader.GetInt64(reader.GetOrdinal("PinOrder")),
                RunAsAdmin = reader.GetInt64(reader.GetOrdinal("RunAsAdmin")) != 0,
                GroupId = reader.GetString(reader.GetOrdinal("GroupId")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                CreatedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UseWorkingDirectory = reader.GetInt64(reader.GetOrdinal("UseWorkingDirectory")) != 0,
                ShellWorkingDirectory = reader.GetString(reader.GetOrdinal("ShellWorkingDirectory"))
            });
        }

        return list;
    }

    /// <summary>모든 프로젝트의 태그를 한 번에 읽어 프로젝트 ID 기준 맵을 반환합니다.</summary>
    private static Dictionary<string, List<string>> ReadAllTags(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ProjectId, Tag FROM ProjectTags";
        using var reader = cmd.ExecuteReader();

        var map = new Dictionary<string, List<string>>();
        while (reader.Read())
        {
            var pid = reader.GetString(0);
            var tag = reader.GetString(1);
            if (!map.TryGetValue(pid, out var list))
            {
                list = [];
                map[pid] = list;
            }
            list.Add(tag);
        }

        return map;
    }

    /// <summary>모든 프로젝트의 커맨드 스크립트를 한 번에 읽어 프로젝트 ID 기준 맵을 반환합니다.</summary>
    private static Dictionary<string, List<CommandScript?>> ReadAllCommandScripts(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CommandScripts ORDER BY SlotIndex";
        using var reader = cmd.ExecuteReader();

        // 구버전 DB 호환: CloseAfterCompletion 컬럼이 없으면 기본값(false) 사용
        var hasCloseColumn = HasColumn(reader, "CloseAfterCompletion");

        var map = new Dictionary<string, List<CommandScript?>>();
        while (reader.Read())
        {
            var pid = reader.GetString(reader.GetOrdinal("ProjectId"));
            if (!map.TryGetValue(pid, out var list))
            {
                list = [null, null, null, null];
                map[pid] = list;
            }

            var index = (int)reader.GetInt64(reader.GetOrdinal("SlotIndex"));
            if (index >= 0 && index < 4)
            {
                list[index] = new CommandScript
                {
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    ShellType = Enum.TryParse<ShellType>(reader.GetString(reader.GetOrdinal("ShellType")), out var st) ? st : ShellType.Cmd,
                    RunAsAdmin = reader.GetInt64(reader.GetOrdinal("RunAsAdmin")) != 0,
                    Script = reader.GetString(reader.GetOrdinal("Script")),
                    UseWorkingDirectory = reader.GetInt64(reader.GetOrdinal("UseWorkingDirectory")) != 0,
                    WorkingDirectory = reader.GetString(reader.GetOrdinal("WorkingDirectory")),
                    IconSymbol = reader.GetString(reader.GetOrdinal("IconSymbol")),
                    CloseAfterCompletion = hasCloseColumn && reader.GetInt64(reader.GetOrdinal("CloseAfterCompletion")) != 0
                };
            }
        }

        return map;
    }

    // ===== 내부 헬퍼: 쓰기 =====

    private static void InsertProject(SqliteConnection conn, ProjectItem p)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Projects
                (Id, Name, Description, IconPath, Path, DevToolName, Options, Command,
                 GitStatus, IsPinned, PinOrder, RunAsAdmin, GroupId, Category, CreatedAt,
                 UseWorkingDirectory, ShellWorkingDirectory)
            VALUES
                (@id, @name, @desc, @icon, @path, @devTool, @opts, @cmd,
                 @git, @pinned, @pinOrder, @admin, @groupId, @cat, @created,
                 @useWd, @shellWd)
            """;
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.Parameters.AddWithValue("@name", p.Name);
        cmd.Parameters.AddWithValue("@desc", p.Description);
        cmd.Parameters.AddWithValue("@icon", p.IconPath);
        cmd.Parameters.AddWithValue("@path", p.Path);
        cmd.Parameters.AddWithValue("@devTool", p.DevToolName);
        cmd.Parameters.AddWithValue("@opts", p.Options);
        cmd.Parameters.AddWithValue("@cmd", p.Command);
        cmd.Parameters.AddWithValue("@git", p.GitStatus);
        cmd.Parameters.AddWithValue("@pinned", p.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@pinOrder", p.PinOrder);
        cmd.Parameters.AddWithValue("@admin", p.RunAsAdmin ? 1 : 0);
        cmd.Parameters.AddWithValue("@groupId", p.GroupId);
        cmd.Parameters.AddWithValue("@cat", p.Category);
        cmd.Parameters.AddWithValue("@created", p.CreatedAt.ToString(DateTimeFormat));
        cmd.Parameters.AddWithValue("@useWd", p.UseWorkingDirectory ? 1 : 0);
        cmd.Parameters.AddWithValue("@shellWd", p.ShellWorkingDirectory);
        cmd.ExecuteNonQuery();
    }

    private static void UpdateProject(SqliteConnection conn, ProjectItem p)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Projects SET
                Name = @name, Description = @desc, IconPath = @icon, Path = @path,
                DevToolName = @devTool, Options = @opts, Command = @cmd,
                GitStatus = @git, IsPinned = @pinned, PinOrder = @pinOrder,
                RunAsAdmin = @admin, GroupId = @groupId, Category = @cat,
                UseWorkingDirectory = @useWd, ShellWorkingDirectory = @shellWd
            WHERE Id = @id
            """;
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.Parameters.AddWithValue("@name", p.Name);
        cmd.Parameters.AddWithValue("@desc", p.Description);
        cmd.Parameters.AddWithValue("@icon", p.IconPath);
        cmd.Parameters.AddWithValue("@path", p.Path);
        cmd.Parameters.AddWithValue("@devTool", p.DevToolName);
        cmd.Parameters.AddWithValue("@opts", p.Options);
        cmd.Parameters.AddWithValue("@cmd", p.Command);
        cmd.Parameters.AddWithValue("@git", p.GitStatus);
        cmd.Parameters.AddWithValue("@pinned", p.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@pinOrder", p.PinOrder);
        cmd.Parameters.AddWithValue("@admin", p.RunAsAdmin ? 1 : 0);
        cmd.Parameters.AddWithValue("@groupId", p.GroupId);
        cmd.Parameters.AddWithValue("@cat", p.Category);
        cmd.Parameters.AddWithValue("@useWd", p.UseWorkingDirectory ? 1 : 0);
        cmd.Parameters.AddWithValue("@shellWd", p.ShellWorkingDirectory);
        cmd.ExecuteNonQuery();
    }

    private static void InsertTags(SqliteConnection conn, string projectId, List<string> tags)
    {
        if (tags.Count == 0) return;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO ProjectTags (ProjectId, Tag) VALUES (@pid, @tag)";
        cmd.Parameters.AddWithValue("@pid", projectId);
        cmd.Parameters.AddWithValue("@tag", string.Empty);

        foreach (var tag in tags)
        {
            cmd.Parameters["@tag"].Value = tag;
            cmd.ExecuteNonQuery();
        }
    }

    private static void InsertCommandScripts(SqliteConnection conn, string projectId, List<CommandScript?> scripts)
    {
        if (scripts.All(s => s is null)) return;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO CommandScripts
                (ProjectId, SlotIndex, Description, ShellType, RunAsAdmin, Script, UseWorkingDirectory, WorkingDirectory, IconSymbol, CloseAfterCompletion)
            VALUES
                (@pid, @idx, @desc, @shell, @admin, @script, @useWd, @wd, @icon, @close)
            """;
        cmd.Parameters.AddWithValue("@pid", projectId);
        cmd.Parameters.AddWithValue("@idx", 0);
        cmd.Parameters.AddWithValue("@desc", string.Empty);
        cmd.Parameters.AddWithValue("@shell", string.Empty);
        cmd.Parameters.AddWithValue("@admin", 0);
        cmd.Parameters.AddWithValue("@script", string.Empty);
        cmd.Parameters.AddWithValue("@useWd", 0);
        cmd.Parameters.AddWithValue("@wd", string.Empty);
        cmd.Parameters.AddWithValue("@icon", string.Empty);
        cmd.Parameters.AddWithValue("@close", 0);

        for (var i = 0; i < scripts.Count; i++)
        {
            var s = scripts[i];
            if (s is null) continue;

            cmd.Parameters["@idx"].Value = i;
            cmd.Parameters["@desc"].Value = s.Description;
            cmd.Parameters["@shell"].Value = s.ShellType.ToString();
            cmd.Parameters["@admin"].Value = s.RunAsAdmin ? 1 : 0;
            cmd.Parameters["@script"].Value = s.Script;
            cmd.Parameters["@useWd"].Value = s.UseWorkingDirectory ? 1 : 0;
            cmd.Parameters["@wd"].Value = s.WorkingDirectory;
            cmd.Parameters["@icon"].Value = s.IconSymbol;
            cmd.Parameters["@close"].Value = s.CloseAfterCompletion ? 1 : 0;
            cmd.ExecuteNonQuery();
        }
    }

    private static void InsertTodos(SqliteConnection conn, string projectId, List<TodoItem> todos)
    {
        if (todos.Count == 0) return;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Todos
                (Id, ProjectId, Text, Description, IsCompleted, Status, CompletedAt, CreatedAt)
            VALUES
                (@id, @pid, @text, @desc, @completed, @status, @completedAt, @created)
            """;
        cmd.Parameters.AddWithValue("@id", string.Empty);
        cmd.Parameters.AddWithValue("@pid", projectId);
        cmd.Parameters.AddWithValue("@text", string.Empty);
        cmd.Parameters.AddWithValue("@desc", string.Empty);
        cmd.Parameters.AddWithValue("@completed", 0);
        cmd.Parameters.AddWithValue("@status", string.Empty);
        cmd.Parameters.AddWithValue("@completedAt", DBNull.Value);
        cmd.Parameters.AddWithValue("@created", string.Empty);

        foreach (var t in todos)
        {
            cmd.Parameters["@id"].Value = t.Id;
            cmd.Parameters["@text"].Value = t.Text;
            cmd.Parameters["@desc"].Value = t.Description;
            cmd.Parameters["@completed"].Value = t.IsCompleted ? 1 : 0;
            cmd.Parameters["@status"].Value = t.Status.ToString();
            cmd.Parameters["@completedAt"].Value = t.CompletedAt.HasValue
                ? t.CompletedAt.Value.ToString(DateTimeFormat)
                : (object)DBNull.Value;
            cmd.Parameters["@created"].Value = t.CreatedAt.ToString(DateTimeFormat);
            cmd.ExecuteNonQuery();
        }
    }

    private static void InsertHistories(SqliteConnection conn, string projectId, List<HistoryEntry> histories)
    {
        if (histories.Count == 0) return;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Histories
                (Id, ProjectId, Title, Description, CompletedAt, CreatedAt)
            VALUES
                (@id, @pid, @title, @desc, @completedAt, @created)
            """;
        cmd.Parameters.AddWithValue("@id", string.Empty);
        cmd.Parameters.AddWithValue("@pid", projectId);
        cmd.Parameters.AddWithValue("@title", string.Empty);
        cmd.Parameters.AddWithValue("@desc", string.Empty);
        cmd.Parameters.AddWithValue("@completedAt", string.Empty);
        cmd.Parameters.AddWithValue("@created", string.Empty);

        foreach (var h in histories)
        {
            cmd.Parameters["@id"].Value = h.Id;
            cmd.Parameters["@title"].Value = h.Title;
            cmd.Parameters["@desc"].Value = h.Description;
            cmd.Parameters["@completedAt"].Value = h.CompletedAt.ToString(DateTimeFormat);
            cmd.Parameters["@created"].Value = h.CreatedAt.ToString(DateTimeFormat);
            cmd.ExecuteNonQuery();
        }
    }

    private static void InsertTestCategories(SqliteConnection conn, string projectId, List<TestCategory> categories)
    {
        if (categories.Count == 0) return;

        // 카테고리 삽입
        using var catCmd = conn.CreateCommand();
        catCmd.CommandText = """
            INSERT INTO TestCategories (Id, ProjectId, Name, CreatedAt)
            VALUES (@id, @pid, @name, @created)
            """;
        catCmd.Parameters.AddWithValue("@id", string.Empty);
        catCmd.Parameters.AddWithValue("@pid", projectId);
        catCmd.Parameters.AddWithValue("@name", string.Empty);
        catCmd.Parameters.AddWithValue("@created", string.Empty);

        // 항목 삽입
        using var itemCmd = conn.CreateCommand();
        itemCmd.CommandText = """
            INSERT INTO TestItems
                (Id, CategoryId, ProjectId, Text, ProgressNote, IsCompleted, Status, CompletedAt, CreatedAt)
            VALUES
                (@id, @catId, @pid, @text, @note, @completed, @status, @completedAt, @created)
            """;
        itemCmd.Parameters.AddWithValue("@id", string.Empty);
        itemCmd.Parameters.AddWithValue("@catId", string.Empty);
        itemCmd.Parameters.AddWithValue("@pid", projectId);
        itemCmd.Parameters.AddWithValue("@text", string.Empty);
        itemCmd.Parameters.AddWithValue("@note", string.Empty);
        itemCmd.Parameters.AddWithValue("@completed", 0);
        itemCmd.Parameters.AddWithValue("@status", TestItem.StatusTesting);
        itemCmd.Parameters.AddWithValue("@completedAt", DBNull.Value);
        itemCmd.Parameters.AddWithValue("@created", string.Empty);

        foreach (var cat in categories)
        {
            catCmd.Parameters["@id"].Value = cat.Id;
            catCmd.Parameters["@name"].Value = cat.Name;
            catCmd.Parameters["@created"].Value = cat.CreatedAt.ToString(DateTimeFormat);
            catCmd.ExecuteNonQuery();

            foreach (var t in cat.Items)
            {
                itemCmd.Parameters["@id"].Value = t.Id;
                itemCmd.Parameters["@catId"].Value = cat.Id;
                itemCmd.Parameters["@text"].Value = t.Text;
                itemCmd.Parameters["@note"].Value = t.ProgressNote;
                itemCmd.Parameters["@completed"].Value = t.IsCompleted ? 1 : 0;
                itemCmd.Parameters["@status"].Value = t.Status;
                itemCmd.Parameters["@completedAt"].Value = t.CompletedAt.HasValue
                    ? t.CompletedAt.Value.ToString(DateTimeFormat)
                    : (object)DBNull.Value;
                itemCmd.Parameters["@created"].Value = t.CreatedAt.ToString(DateTimeFormat);
                itemCmd.ExecuteNonQuery();
            }
        }
    }

    // ===== 공통 헬퍼 =====

    /// <summary>특정 테이블에서 projectId에 해당하는 모든 행을 삭제합니다.</summary>
    private static void DeleteByProjectId(SqliteConnection conn, string tableName, string projectId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM [{tableName}] WHERE ProjectId = @pid";
        cmd.Parameters.AddWithValue("@pid", projectId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>ISO 8601 또는 일반 형식의 날짜 문자열을 DateTime으로 파싱합니다.</summary>
    private static DateTime ParseDateTime(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;

        return DateTime.MinValue;
    }

    /// <summary>SqliteDataReader에 지정된 컬럼이 존재하는지 확인합니다 (구버전 DB 호환용).</summary>
    private static bool HasColumn(SqliteDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ===== 가져오기/내보내기 지원 =====

    /// <summary>지정된 DB 파일에서 모든 프로젝트(Tags, Scripts, Todos, Histories 포함)를 읽어옵니다.</summary>
    public static List<ProjectItem> ReadAllFromDb(string dbPath)
    {
        using var conn = DatabaseContext.CreateConnectionForPath(dbPath);

        // Projects 테이블 존재 여부 확인
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Projects'";
        if (checkCmd.ExecuteScalar() is null)
            throw new InvalidOperationException("InvalidDbFile");

        var projects = ReadProjects(conn);
        var tagsMap = ReadAllTags(conn);
        var scriptsMap = ReadAllCommandScripts(conn);

        foreach (var p in projects)
        {
            p.Tags = tagsMap.TryGetValue(p.Id, out var tags) ? tags : [];
            p.CommandScripts = scriptsMap.TryGetValue(p.Id, out var scripts) ? scripts : [null, null, null, null];
            p.Todos = ReadTodosForProject(conn, p.Id);
            p.Histories = ReadHistoriesForProject(conn, p.Id);
            p.TestCategories = ReadTestCategoriesForProject(conn, p.Id);
        }

        return projects;
    }

    /// <summary>지정된 DB 파일의 Groups 테이블에서 그룹 목록을 읽어옵니다.
    /// Groups 테이블이 없는 구버전 DB는 빈 목록을 반환합니다.</summary>
    public static List<ProjectGroup> ReadGroupsFromDb(string dbPath)
    {
        using var conn = DatabaseContext.CreateConnectionForPath(dbPath);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, IsDefault FROM Groups";
        using var reader = cmd.ExecuteReader();

        var list = new List<ProjectGroup>();
        while (reader.Read())
        {
            list.Add(new ProjectGroup
            {
                Id        = reader.GetString(reader.GetOrdinal("Id")),
                Name      = reader.GetString(reader.GetOrdinal("Name")),
                IsDefault = reader.GetInt32(reader.GetOrdinal("IsDefault")) != 0
            });
        }

        return list;
    }

    /// <summary>프로젝트를 삭제(관련 하위 데이터 포함)하고 새로 추가합니다 (덮어쓰기용).</summary>
    public void DeleteAndInsert(ProjectItem project)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        // CASCADE로 하위 테이블 자동 삭제
        using (var delCmd = conn.CreateCommand())
        {
            delCmd.CommandText = "DELETE FROM Projects WHERE Id = @id";
            delCmd.Parameters.AddWithValue("@id", project.Id);
            delCmd.ExecuteNonQuery();
        }

        InsertProject(conn, project);
        InsertTags(conn, project.Id, project.Tags);
        InsertCommandScripts(conn, project.Id, project.CommandScripts);
        InsertTodos(conn, project.Id, project.Todos);
        InsertHistories(conn, project.Id, project.Histories);
        InsertTestCategories(conn, project.Id, project.TestCategories);

        tx.Commit();
    }

    /// <summary>이름이 동일한 기존 프로젝트의 ID를 반환합니다. 없으면 null.</summary>
    public string? FindProjectIdByName(string name)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Projects WHERE Name = @name";
        cmd.Parameters.AddWithValue("@name", name);
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>이름이 동일한 기존 프로젝트를 삭제하고 새 프로젝트를 추가합니다 (이름 기준 덮어쓰기).</summary>
    public void DeleteByNameAndInsert(string existingId, ProjectItem project)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        // 기존 프로젝트 삭제 (CASCADE로 하위 테이블 자동 삭제)
        using (var delCmd = conn.CreateCommand())
        {
            delCmd.CommandText = "DELETE FROM Projects WHERE Id = @id";
            delCmd.Parameters.AddWithValue("@id", existingId);
            delCmd.ExecuteNonQuery();
        }

        InsertProject(conn, project);
        InsertTags(conn, project.Id, project.Tags);
        InsertCommandScripts(conn, project.Id, project.CommandScripts);
        InsertTodos(conn, project.Id, project.Todos);
        InsertHistories(conn, project.Id, project.Histories);
        InsertTestCategories(conn, project.Id, project.TestCategories);

        tx.Commit();
    }

    private static List<TodoItem> ReadTodosForProject(SqliteConnection conn, string projectId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Todos WHERE ProjectId = @pid ORDER BY CreatedAt";
        cmd.Parameters.AddWithValue("@pid", projectId);
        using var reader = cmd.ExecuteReader();

        var list = new List<TodoItem>();
        var hasStatus = HasColumn(reader, "Status");
        while (reader.Read())
        {
            var completedAtStr = reader.IsDBNull(reader.GetOrdinal("CompletedAt"))
                ? null
                : reader.GetString(reader.GetOrdinal("CompletedAt"));
            var isCompleted = reader.GetInt64(reader.GetOrdinal("IsCompleted")) != 0;

            list.Add(new TodoItem
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                Text = reader.GetString(reader.GetOrdinal("Text")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                IsCompleted = isCompleted,
                Status = hasStatus && Enum.TryParse<TodoStatus>(reader.GetString(reader.GetOrdinal("Status")), out var s)
                    ? s
                    : isCompleted ? TodoStatus.Completed : TodoStatus.Waiting,
                CompletedAt = string.IsNullOrEmpty(completedAtStr) ? null : ParseDateTime(completedAtStr),
                CreatedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("CreatedAt")))
            });
        }

        return list;
    }

    private static List<HistoryEntry> ReadHistoriesForProject(SqliteConnection conn, string projectId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Histories WHERE ProjectId = @pid ORDER BY CompletedAt DESC, CreatedAt DESC";
        cmd.Parameters.AddWithValue("@pid", projectId);
        using var reader = cmd.ExecuteReader();

        var list = new List<HistoryEntry>();
        while (reader.Read())
        {
            list.Add(new HistoryEntry
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                CompletedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("CompletedAt"))),
                CreatedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("CreatedAt")))
            });
        }

        return list;
    }

    private static List<TestCategory> ReadTestCategoriesForProject(SqliteConnection conn, string projectId)
    {
        // 카테고리 읽기
        using var catCmd = conn.CreateCommand();
        catCmd.CommandText = "SELECT * FROM TestCategories WHERE ProjectId = @pid ORDER BY CreatedAt";
        catCmd.Parameters.AddWithValue("@pid", projectId);
        using var catReader = catCmd.ExecuteReader();

        var categories = new List<TestCategory>();
        while (catReader.Read())
        {
            categories.Add(new TestCategory
            {
                Id = catReader.GetString(catReader.GetOrdinal("Id")),
                Name = catReader.GetString(catReader.GetOrdinal("Name")),
                CreatedAt = ParseDateTime(catReader.GetString(catReader.GetOrdinal("CreatedAt")))
            });
        }

        if (categories.Count == 0) return categories;

        // 테스트 항목 읽기 — CategoryId 기준으로 매핑
        using var itemCmd = conn.CreateCommand();
        itemCmd.CommandText = "SELECT * FROM TestItems WHERE ProjectId = @pid ORDER BY CreatedAt";
        itemCmd.Parameters.AddWithValue("@pid", projectId);
        using var itemReader = itemCmd.ExecuteReader();

        var catMap = categories.ToDictionary(c => c.Id);
        var hasStatusColumn = HasColumn(itemReader, "Status");
        while (itemReader.Read())
        {
            var completedAtStr = itemReader.IsDBNull(itemReader.GetOrdinal("CompletedAt"))
                ? null
                : itemReader.GetString(itemReader.GetOrdinal("CompletedAt"));

            var categoryId = itemReader.GetString(itemReader.GetOrdinal("CategoryId"));

            // 구버전 DB 호환: Status 컬럼이 없으면 IsCompleted 기반으로 결정
            string status;
            if (hasStatusColumn)
            {
                status = itemReader.GetString(itemReader.GetOrdinal("Status"));
            }
            else
            {
                var isCompleted = itemReader.GetInt64(itemReader.GetOrdinal("IsCompleted")) != 0;
                status = isCompleted ? TestItem.StatusDone : TestItem.StatusTesting;
            }

            var item = new TestItem
            {
                Id = itemReader.GetString(itemReader.GetOrdinal("Id")),
                CategoryId = categoryId,
                Text = itemReader.GetString(itemReader.GetOrdinal("Text")),
                ProgressNote = itemReader.GetString(itemReader.GetOrdinal("ProgressNote")),
                Status = status,
                CompletedAt = string.IsNullOrEmpty(completedAtStr) ? null : ParseDateTime(completedAtStr),
                CreatedAt = ParseDateTime(itemReader.GetString(itemReader.GetOrdinal("CreatedAt")))
            };

            if (catMap.TryGetValue(categoryId, out var cat))
                cat.Items.Add(item);
        }

        return categories;
    }

    /// <summary>DB Groups 테이블을 지정된 그룹 목록으로 전체 교체합니다 (AppSettings → DB 동기화).</summary>
    public void SyncGroups(IReadOnlyList<ProjectGroup> groups)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        using (var delCmd = conn.CreateCommand())
        {
            delCmd.CommandText = "DELETE FROM Groups";
            delCmd.ExecuteNonQuery();
        }

        using var insCmd = conn.CreateCommand();
        insCmd.CommandText = "INSERT INTO Groups (Id, Name, IsDefault) VALUES (@id, @name, @isDefault)";
        var idParam   = insCmd.Parameters.Add("@id",   SqliteType.Text);
        var nameParam = insCmd.Parameters.Add("@name", SqliteType.Text);
        var defParam  = insCmd.Parameters.Add("@isDefault", SqliteType.Integer);

        foreach (var g in groups)
        {
            idParam.Value   = g.Id;
            nameParam.Value = g.Name;
            defParam.Value  = g.IsDefault ? 1 : 0;
            insCmd.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
