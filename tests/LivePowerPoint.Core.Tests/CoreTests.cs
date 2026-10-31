using System.Globalization;
using LivePowerPoint.Core;
using Xunit;

public class ParserTests
{
    const string Good = "{\"tableId\":\"t\",\"version\":3,\"updatedAtUtc\":\"2026-10-30T22:00:00Z\",\"columns\":[\"A\",\"B\"],\"rows\":[[\"x\",1],[null,2.5]]}";

    [Fact] public void ParsesValid() { var s = SnapshotParser.Parse(Good); Assert.Equal(3, s.Version); Assert.Equal(2, s.Rows.Count); Assert.Null(s.Rows[1][0]); }
    [Theory]
    [InlineData("")] [InlineData("{")] [InlineData("[]")]
    [InlineData("{\"tableId\":\"t\",\"version\":-1,\"updatedAtUtc\":\"2026-10-30T22:00:00Z\",\"columns\":[\"A\"],\"rows\":[]}")]
    [InlineData("{\"tableId\":\"t\",\"version\":1,\"updatedAtUtc\":\"2026-10-30T22:00:00Z\",\"columns\":[\"A\",\"A\"],\"rows\":[]}")]
    [InlineData("{\"tableId\":\"t\",\"version\":1,\"updatedAtUtc\":\"2026-10-30T22:00:00Z\",\"columns\":[\"A\"],\"rows\":[[1,2]]}")]
    [InlineData("{\"tableId\":\"t\",\"version\":1,\"updatedAtUtc\":\"2026-10-30T22:00:00Z\",\"columns\":[\"A\"],\"rows\":[],\"extra\":1}")]
    [InlineData("{\"tableId\":\"t\",\"version\":1,\"updatedAtUtc\":\"2026-10-30T22:00:00Z\",\"columns\":[\"A\"],\"rows\":[[{}]]}")]
    public void RejectsInvalid(string json) => Assert.Throws<SnapshotParseException>(() => SnapshotParser.Parse(json));
}

public class PlannerTests
{
    static readonly CultureInfo C = CultureInfo.GetCultureInfo("en-US");
    static TableSnapshot Snap(params object[][] rows) => new TableSnapshot
    { TableId = "t", Version = 1, Columns = new[] { "Dept", "Sales" }, Rows = rows.Select(r => (IReadOnlyList<object>)r).ToList() };
    static BindingConfig Cfg() => new BindingConfig
    {
        DatasetId = "t", MaxRows = 5,
        Columns = { new ColumnMapping { Field = "Dept", TableColumn = 1 }, new ColumnMapping { Field = "Sales", TableColumn = 3, Format = ValueFormat.Currency } }
    };

    [Fact] public void WritesOnlyMappedCellsBelowHeader()
    {
        var p = MappingPlanner.Plan(Cfg(), Snap(new object[] { "N", 1500L }), 4, 3, (r, c) => "", C);
        Assert.Equal(2, p.Writes.Count);
        Assert.All(p.Writes, w => Assert.Equal(2, w.Row));
        Assert.DoesNotContain(p.Writes, w => w.Column == 2);          // unmapped column untouched
        Assert.Equal("$1,500", p.Writes.Single(w => w.Column == 3).Text);
    }
    [Fact] public void SkipsUnchangedCells()
    {
        var p = MappingPlanner.Plan(Cfg(), Snap(new object[] { "N", 1500L }), 2, 3, (r, c) => c == 1 ? "N" : "$1,500", C);
        Assert.Empty(p.Writes);
    }
    [Fact] public void TruncatesWithDiagnostic()
    {
        var p = MappingPlanner.Plan(Cfg(), Snap(new object[] { "a", 1L }, new object[] { "b", 2L }, new object[] { "c", 3L }), 3, 3, (r, c) => "", C);
        Assert.Contains(p.Writes, w => w.Row == 3);
        Assert.DoesNotContain(p.Writes, w => w.Row > 3);
        Assert.Contains(p.Diagnostics, d => d.StartsWith("Truncated"));
    }
    [Fact] public void ClearsSurplusRows()
    {
        var p = MappingPlanner.Plan(Cfg(), Snap(new object[] { "a", 1L }), 3, 3, (r, c) => "old", C);
        Assert.Contains(p.Writes, w => w.Row == 3 && w.Text == "");
    }
    [Fact] public void MissingFieldAndOutOfRangeColumnDiagnosed()
    {
        var cfg = Cfg(); cfg.Columns.Add(new ColumnMapping { Field = "Nope", TableColumn = 2 }); cfg.Columns.Add(new ColumnMapping { Field = "Dept", TableColumn = 9 });
        var p = MappingPlanner.Plan(cfg, Snap(new object[] { "a", 1L }), 2, 3, (r, c) => "", C);
        Assert.Equal(2, p.Diagnostics.Count);
    }
    [Fact] public void MaxRowsHonoured()
    {
        var cfg = Cfg(); cfg.MaxRows = 1;
        var p = MappingPlanner.Plan(cfg, Snap(new object[] { "a", 1L }, new object[] { "b", 2L }), 10, 3, (r, c) => "", C);
        Assert.DoesNotContain(p.Writes, w => w.Row > 2);
    }
}

public class FormatterTests
{
    static readonly CultureInfo C = CultureInfo.GetCultureInfo("en-US");
    [Theory]
    [InlineData(ValueFormat.Number, 1234567L, "1,234,567")]
    [InlineData(ValueFormat.Currency, 98500L, "$98,500")]
    [InlineData(ValueFormat.Text, true, "Yes")]
    public void Formats(ValueFormat f, object v, string expected) => Assert.Equal(expected, CellFormatter.Format(v, new ColumnMapping { Format = f }, C));
    [Fact] public void Percent() => Assert.Equal("82.0%", CellFormatter.Format(0.82m, new ColumnMapping { Format = ValueFormat.Percent }, C));
    [Fact] public void Date() => Assert.Equal("10/30/2026", CellFormatter.Format("2026-10-30T22:00:00Z", new ColumnMapping { Format = ValueFormat.Date }, C));
    [Fact] public void NullUsesNullText() => Assert.Equal("n/a", CellFormatter.Format(null, new ColumnMapping { NullText = "n/a" }, C));
    [Fact] public void SanitizesControlChars() => Assert.Equal("a b c", CellFormatter.Sanitize("a\nb\u0007 c"));
    [Fact] public void NonNumericWithNumberFormatFallsBackToText() => Assert.Equal("abc", CellFormatter.Format("abc", new ColumnMapping { Format = ValueFormat.Number }, C));
}

public class PollingTests
{
    [Fact] public void VersionTrackerSkipsSameAndOlder()
    {
        var v = new VersionTracker(); var s = new TableSnapshot { Version = 5 };
        Assert.True(v.ShouldApply(s)); v.MarkApplied(s);
        Assert.False(v.ShouldApply(s)); Assert.False(v.ShouldApply(new TableSnapshot { Version = 4 })); Assert.True(v.ShouldApply(new TableSnapshot { Version = 6 }));
    }
    [Fact] public void BackoffGrowsCapsAndResets()
    {
        var b = new BackoffPolicy(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60), new Random(1));
        var d1 = b.Failure(); var d2 = b.Failure();
        Assert.True(d2 > d1 * 1.0 || d2.TotalSeconds >= 15);
        for (int i = 0; i < 20; i++) Assert.True(b.Failure().TotalSeconds <= 75.1);
        b.Success(); Assert.Equal(TimeSpan.FromSeconds(5), b.Next);
    }
    [Fact] public void RefreshClamped() { Assert.Equal(2, new BindingConfig { RefreshSeconds = 0 }.ClampedRefreshSeconds); Assert.Equal(60, new BindingConfig { RefreshSeconds = 999 }.ClampedRefreshSeconds); }
}
