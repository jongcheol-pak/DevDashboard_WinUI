using Microsoft.Data.Sqlite;

namespace DevDashboard.Infrastructure.Persistence;

/// <summary>
/// SQLite 기반 런처 항목 저장소.
/// LauncherItems 테이블에 CRUD 작업을 수행합니다.
/// </summary>
public sealed class LauncherRepository
{
    private readonly DatabaseContext _db;

    public LauncherRepository(DatabaseContext db)
    {
        _db = db;
    }

    /// <summary>모든 런처 항목을 SortOrder 순으로 반환합니다.</summary>
    public List<LauncherItem> GetAll()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, DisplayName, ExecutablePath, IconCachePath, SortOrder FROM LauncherItems ORDER BY SortOrder";
        using var reader = cmd.ExecuteReader();

        var list = new List<LauncherItem>();
        while (reader.Read())
        {
            list.Add(new LauncherItem
            {
                Id = reader.GetString(0),
                DisplayName = reader.GetString(1),
                ExecutablePath = reader.GetString(2),
                IconCachePath = reader.GetString(3),
                SortOrder = reader.GetInt32(4)
            });
        }
        return list;
    }

    /// <summary>런처 항목을 추가합니다.</summary>
    public void Add(LauncherItem item)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO LauncherItems (Id, DisplayName, ExecutablePath, IconCachePath, SortOrder)
            VALUES (@id, @name, @path, @icon, @order)
            """;
        cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.Parameters.AddWithValue("@name", item.DisplayName);
        cmd.Parameters.AddWithValue("@path", item.ExecutablePath);
        cmd.Parameters.AddWithValue("@icon", item.IconCachePath);
        cmd.Parameters.AddWithValue("@order", item.SortOrder);
        cmd.ExecuteNonQuery();
    }

    /// <summary>런처 항목을 삭제합니다.</summary>
    public void Delete(string id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM LauncherItems WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>모든 항목의 SortOrder를 일괄 업데이트합니다.</summary>
    public void UpdateSortOrders(List<LauncherItem> items)
    {
        using var conn = _db.CreateConnection();
        using var transaction = conn.BeginTransaction();

        try
        {
            foreach (var item in items)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE LauncherItems SET SortOrder = @order WHERE Id = @id";
                cmd.Parameters.AddWithValue("@order", item.SortOrder);
                cmd.Parameters.AddWithValue("@id", item.Id);
                cmd.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
