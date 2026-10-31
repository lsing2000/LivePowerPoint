using System.Net;
using System.Security.Cryptography;
using System.Text;
using LivePowerPoint.DataService;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var apiKey = app.Configuration["LivePowerPoint:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("LivePowerPoint:ApiKey must be configured (env LivePowerPoint__ApiKey).");
    apiKey = "demo-key-change-me"; // Development only; never used in Production.
}

var dbPath = app.Configuration["LivePowerPoint:Database"] ?? Path.Combine(app.Environment.ContentRootPath, "livepowerpoint.db");
var schema = app.Configuration["LivePowerPoint:SchemaSql"] ?? Path.Combine(AppContext.BaseDirectory, "sample-database.sql");
var store = new DataStore($"Data Source={dbPath}", schema);

bool KeyOk(HttpRequest req)
{
    var supplied = req.Headers["X-Api-Key"].ToString();
    if (supplied.Length == 0 && req.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        supplied = req.Headers.Authorization.ToString()[7..];
    return CryptographicOperations.FixedTimeEquals(
        SHA256.HashData(Encoding.UTF8.GetBytes(supplied)), SHA256.HashData(Encoding.UTF8.GetBytes(apiKey!)));
}

app.MapGet("/api/live-tables/{datasetId}", (string datasetId, HttpRequest req, HttpResponse res) =>
{
    if (!KeyOk(req)) return Results.Unauthorized();
    var snap = store.GetSnapshot(datasetId);
    if (snap is null) return Results.NotFound();
    var etag = $"\"{((dynamic)snap).version}\"";
    if (req.Headers.IfNoneMatch == etag) return Results.StatusCode(304);
    res.Headers.ETag = etag;
    res.Headers.CacheControl = "no-store";
    return Results.Json(snap);
});

// Demo writes: Development environment AND loopback caller AND API key. Not mapped in Production.
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/demo/update", (HttpRequest req, string datasetId, string key, long value) =>
    {
        if (!KeyOk(req)) return Results.Unauthorized();
        var ip = req.HttpContext.Connection.RemoteIpAddress; // null only for the in-process TestServer
        if (ip != null && !IPAddress.IsLoopback(ip)) return Results.StatusCode(403);
        return store.UpdateValue(datasetId, key, value) ? Results.Ok() : Results.NotFound();
    });
}

app.Run();

public partial class Program { }
