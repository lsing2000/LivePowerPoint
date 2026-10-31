using Microsoft.Data.Sqlite;
using System.Globalization;

namespace LivePowerPoint.DataService;

public sealed record Dataset(string Id, string Table, string[] Columns, string[] DbColumns, string OrderBy, string KeyColumn, string ValueColumn);

/// <summary>Allow-listed datasets only. No SQL, table or column name ever comes from a request.</summary>
public sealed class DataStore
{
    public static readonly Dictionary<string, Dataset> Datasets = new(StringComparer.Ordinal)
    {
        ["sales-summary"] = new("sales-summary", "sales_summary", new[] { "Department", "Sales", "Status" }, new[] { "department", "sales", "status" }, "sort_order", "department", "sales"),
        ["inventory"] = new("inventory", "inventory", new[] { "Item", "Stock", "Health" }, new[] { "item", "stock", "health" }, "sort_order", "item", "stock"),
    };

    readonly string _cs;
    public DataStore(string connectionString, string schemaSqlPath)
    {
        _cs = connectionString;
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = File.ReadAllText(schemaSqlPath);
        cmd.ExecuteNonQuery();
    }

    SqliteConnection Open() { var c = new SqliteConnection(_cs); c.Open(); return c; }

    public object? GetSnapshot(string datasetId)
    {
        if (!Datasets.TryGetValue(datasetId, out var ds)) return null;
        using var c = Open();
        long version; string updated;
        using (var m = c.CreateCommand())
        {
            m.CommandText = "SELECT version, updated_at FROM datasets WHERE dataset_id = $id";
            m.Parameters.AddWithValue("$id", datasetId);
            using var r = m.ExecuteReader();
            if (!r.Read()) return null;
            version = r.GetInt64(0); updated = r.GetString(1);
        }
        var rows = new List<object?[]>();
        using (var q = c.CreateCommand())
        {
            // Identifiers come from the compiled allow-list above, never from input.
            q.CommandText = $"SELECT {string.Join(",", ds.DbColumns)} FROM {ds.Table} ORDER BY {ds.OrderBy}";
            using var r = q.ExecuteReader();
            while (r.Read())
            {
                var row = new object?[ds.Columns.Length];
                for (int i = 0; i < row.Length; i++) row[i] = r.IsDBNull(i) ? null : r.GetValue(i);
                rows.Add(row);
            }
        }
        return new { tableId = datasetId, version, updatedAtUtc = updated, columns = ds.Columns, rows };
    }

    /// <summary>Demo-only: sets the value column of one row (parameterised) and bumps the dataset version.</summary>
    public bool UpdateValue(string datasetId, string key, long value)
    {
        if (!Datasets.TryGetValue(datasetId, out var ds)) return false;
        using var c = Open();
        using var tx = c.BeginTransaction();
        using var u = c.CreateCommand();
        u.Transaction = tx;
        u.CommandText = $"UPDATE {ds.Table} SET {ds.ValueColumn} = $v WHERE {ds.KeyColumn} = $k";
        u.Parameters.AddWithValue("$v", value);
        u.Parameters.AddWithValue("$k", key);
        if (u.ExecuteNonQuery() == 0) return false;
        using var b = c.CreateCommand();
        b.Transaction = tx;
        b.CommandText = "UPDATE datasets SET version = version + 1, updated_at = $t WHERE dataset_id = $id";
        b.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        b.Parameters.AddWithValue("$id", datasetId);
        b.ExecuteNonQuery();
        tx.Commit();
        return true;
    }
}
