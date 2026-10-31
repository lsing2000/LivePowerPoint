using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LivePowerPoint.Core
{
    public enum ValueFormat { Text, Number, Currency, Percent, Date }

    public sealed class ColumnMapping
    {
        public string Field { get; set; }
        public int TableColumn { get; set; }       // 1-based PowerPoint column
        public ValueFormat Format { get; set; }
        public string NullText { get; set; } = "";
    }

    /// <summary>Non-secret binding metadata persisted in shape tags (JSON). Never holds credentials.</summary>
    public sealed class BindingConfig
    {
        public string BindingId { get; set; }
        public string DatasetId { get; set; }
        public int HeaderRow { get; set; } = 1;
        public int StartRow { get; set; } = 2;
        public int MaxRows { get; set; } = 10;
        public int RefreshSeconds { get; set; } = 5;
        public List<ColumnMapping> Columns { get; set; } = new List<ColumnMapping>();

        public int ClampedRefreshSeconds => Math.Max(2, Math.Min(60, RefreshSeconds));
    }

    public sealed class CellWrite
    {
        public int Row { get; set; }     // 1-based
        public int Column { get; set; }  // 1-based
        public string Text { get; set; }
    }

    public sealed class PlanResult
    {
        public List<CellWrite> Writes { get; } = new List<CellWrite>();
        public List<string> Diagnostics { get; } = new List<string>();
    }

    public static class CellFormatter
    {
        public static string Sanitize(string s, int maxLen = 500)
        {
            if (s == null) return "";
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                if (ch == '\r' || ch == '\n' || ch == '\t') sb.Append(' ');
                else if (!char.IsControl(ch)) sb.Append(ch);
            }
            var t = sb.ToString();
            return t.Length > maxLen ? t.Substring(0, maxLen) : t;
        }

        public static string Format(object v, ColumnMapping m, CultureInfo culture)
        {
            if (v == null) return Sanitize(m.NullText);
            switch (m.Format)
            {
                case ValueFormat.Number when IsNum(v): return Convert.ToDecimal(v, culture).ToString("#,##0.##", culture);
                case ValueFormat.Currency when IsNum(v): return Convert.ToDecimal(v, culture).ToString("C0", culture);
                case ValueFormat.Percent when IsNum(v): return Convert.ToDecimal(v, culture).ToString("P1", culture);
                case ValueFormat.Date:
                    if (DateTime.TryParse(Convert.ToString(v, culture), culture, DateTimeStyles.RoundtripKind, out var d))
                        return d.ToString("d", culture);
                    break;
            }
            if (v is bool b) return b ? "Yes" : "No";
            return Sanitize(Convert.ToString(v, culture));
        }

        static bool IsNum(object v) => v is long || v is decimal || v is int || v is double;
    }

    public static class MappingPlanner
    {
        /// <summary>
        /// Produces only the writes needed to turn <paramref name="current"/> (table text, 1-based [row,col] dictionary)
        /// into the snapshot. Header and unmapped cells are never touched.
        /// </summary>
        public static PlanResult Plan(BindingConfig cfg, TableSnapshot snap, int tableRows, int tableCols,
            Func<int, int, string> current, CultureInfo culture)
        {
            var res = new PlanResult();
            var index = new Dictionary<string, int>();
            for (int i = 0; i < snap.Columns.Count; i++) index[snap.Columns[i]] = i;

            int capacity = Math.Max(0, Math.Min(cfg.MaxRows, tableRows - cfg.StartRow + 1));
            if (snap.Rows.Count > capacity)
                res.Diagnostics.Add($"Truncated: {snap.Rows.Count} rows received, table holds {capacity}.");

            foreach (var m in cfg.Columns)
            {
                if (!index.ContainsKey(m.Field)) { res.Diagnostics.Add($"Field '{m.Field}' missing from data."); continue; }
                if (m.TableColumn < 1 || m.TableColumn > tableCols) { res.Diagnostics.Add($"Mapped column {m.TableColumn} is outside the table."); continue; }
            }

            for (int r = 0; r < capacity; r++)
            {
                bool hasData = r < snap.Rows.Count;
                foreach (var m in cfg.Columns)
                {
                    if (!index.TryGetValue(m.Field, out var fi) || m.TableColumn < 1 || m.TableColumn > tableCols) continue;
                    var text = hasData ? CellFormatter.Format(snap.Rows[r][fi], m, culture) : "";
                    int tr = cfg.StartRow + r;
                    if (current(tr, m.TableColumn) != text)
                        res.Writes.Add(new CellWrite { Row = tr, Column = m.TableColumn, Text = text });
                }
            }
            return res;
        }
    }
}
