using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace LivePowerPoint.Core
{
    public sealed class TableSnapshot
    {
        public string TableId { get; set; }
        public long Version { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public IReadOnlyList<string> Columns { get; set; }
        public IReadOnlyList<IReadOnlyList<object>> Rows { get; set; }
    }

    public sealed class SnapshotParseException : Exception
    {
        public SnapshotParseException(string m) : base(m) { }
    }

    /// <summary>Strict parser for contracts/live-table.schema.json.</summary>
    public static class SnapshotParser
    {
        static readonly HashSet<string> Allowed = new HashSet<string> { "tableId", "version", "updatedAtUtc", "columns", "rows" };

        public static TableSnapshot Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new SnapshotParseException("Empty response");
            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException e) { throw new SnapshotParseException("Malformed JSON: " + e.Message); }
            using (doc)
            {
                var r = doc.RootElement;
                if (r.ValueKind != JsonValueKind.Object) throw new SnapshotParseException("Root must be an object");
                foreach (var p in r.EnumerateObject())
                    if (!Allowed.Contains(p.Name)) throw new SnapshotParseException("Unexpected property " + p.Name);

                var id = Str(r, "tableId");
                if (id.Length == 0 || id.Length > 128) throw new SnapshotParseException("tableId length invalid");
                var vEl = Req(r, "version", JsonValueKind.Number);
                if (!vEl.TryGetInt64(out var version) || version < 0) throw new SnapshotParseException("version must be integer >= 0");
                DateTime updated;
                if (!DateTime.TryParse(Str(r, "updatedAtUtc"), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out updated))
                    throw new SnapshotParseException("updatedAtUtc invalid");

                var cols = Req(r, "columns", JsonValueKind.Array).EnumerateArray().Select(c =>
                {
                    if (c.ValueKind != JsonValueKind.String) throw new SnapshotParseException("columns must be strings");
                    return c.GetString();
                }).ToList();
                if (cols.Count == 0) throw new SnapshotParseException("columns must not be empty");
                if (cols.Distinct().Count() != cols.Count) throw new SnapshotParseException("columns must be unique");

                var rows = new List<IReadOnlyList<object>>();
                foreach (var row in Req(r, "rows", JsonValueKind.Array).EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Array) throw new SnapshotParseException("row must be an array");
                    var cells = new List<object>();
                    foreach (var c in row.EnumerateArray())
                    {
                        switch (c.ValueKind)
                        {
                            case JsonValueKind.String: cells.Add(c.GetString()); break;
                            case JsonValueKind.Number: cells.Add(c.TryGetInt64(out var l) ? (object)l : c.GetDecimal()); break;
                            case JsonValueKind.True: cells.Add(true); break;
                            case JsonValueKind.False: cells.Add(false); break;
                            case JsonValueKind.Null: cells.Add(null); break;
                            default: throw new SnapshotParseException("Unsupported cell type");
                        }
                    }
                    if (cells.Count != cols.Count) throw new SnapshotParseException("Row width does not match columns");
                    rows.Add(cells);
                }
                return new TableSnapshot { TableId = id, Version = version, UpdatedAtUtc = updated, Columns = cols, Rows = rows };
            }
        }

        static JsonElement Req(JsonElement o, string n, JsonValueKind k)
        {
            if (!o.TryGetProperty(n, out var e) || e.ValueKind != k) throw new SnapshotParseException("Missing or invalid " + n);
            return e;
        }
        static string Str(JsonElement o, string n) => Req(o, n, JsonValueKind.String).GetString();
    }
}
