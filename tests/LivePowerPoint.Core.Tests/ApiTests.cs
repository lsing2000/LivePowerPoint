using System.Net;
using System.Net.Http.Json;
using LivePowerPoint.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

public class ApiFactory : WebApplicationFactory<Program>
{
    public readonly string Db = Path.Combine(Path.GetTempPath(), "lp-test-" + Guid.NewGuid() + ".db");
    public string Env = "Development";
    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment(Env);
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LivePowerPoint:ApiKey"] = "k1", ["LivePowerPoint:Database"] = Db,
            ["LivePowerPoint:SchemaSql"] = Path.Combine(AppContext.BaseDirectory, "sample-database.sql"),
        }));
    }
}

public class ApiTests : IClassFixture<ApiFactory>
{
    readonly ApiFactory _f; public ApiTests(ApiFactory f) { _f = f; }
    HttpClient Client(string? key = "k1") { var c = _f.CreateClient(); if (key != null) c.DefaultRequestHeaders.Add("X-Api-Key", key); return c; }

    [Fact] public async Task RejectsMissingAndWrongKey()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).GetAsync("/api/live-tables/sales-summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client("bad").GetAsync("/api/live-tables/sales-summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(null).PostAsync("/api/demo/update?datasetId=sales-summary&key=North&value=1", null)).StatusCode);
    }
    [Fact] public async Task ResponseMatchesContract()
    {
        var json = await Client().GetStringAsync("/api/live-tables/sales-summary");
        var s = SnapshotParser.Parse(json);
        Assert.Equal("sales-summary", s.TableId); Assert.Equal(3, s.Rows.Count);
    }
    [Fact] public async Task SqlInjectionAttemptsFindNothing()
    {
        foreach (var id in new[] { "sales_summary", "sales-summary';DROP TABLE datasets;--", "../etc/passwd", "inventory%20OR%201=1" })
            Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync("/api/live-tables/" + Uri.EscapeDataString(id))).StatusCode);
        var r = await Client().PostAsync("/api/demo/update?datasetId=sales-summary&key=" + Uri.EscapeDataString("North' OR '1'='1") + "&value=1", null);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client().GetAsync("/api/live-tables/sales-summary")).StatusCode); // tables intact
    }
    [Fact] public async Task UpdateBumpsVersionAndEtagGives304()
    {
        var c = Client();
        var before = SnapshotParser.Parse(await c.GetStringAsync("/api/live-tables/inventory"));
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/api/demo/update?datasetId=inventory&key=Widgets&value=777", null)).StatusCode);
        var resp = await c.GetAsync("/api/live-tables/inventory");
        var after = SnapshotParser.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(before.Version + 1, after.Version); Assert.Equal(777L, after.Rows[0][1]);
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/live-tables/inventory"); req.Headers.Add("X-Api-Key", "k1"); req.Headers.Add("If-None-Match", resp.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.NotModified, (await _f.CreateClient().SendAsync(req)).StatusCode);
    }
}

public class ProductionTests
{
    [Fact] public async Task ProductionHasNoDemoWriteEndpoint()
    {
        var f = new ApiFactory { Env = "Production" };
        var c = f.CreateClient(); c.DefaultRequestHeaders.Add("X-Api-Key", "k1");
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync("/api/demo/update?datasetId=inventory&key=Widgets&value=1", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/live-tables/inventory")).StatusCode);
    }
}
