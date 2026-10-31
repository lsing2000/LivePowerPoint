using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LivePowerPoint.Core;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;

namespace LivePowerPoint.AddIn
{
    public sealed class SourceInfo { public string BaseUrl; public string ApiKey; }

    /// <summary>
    /// One loop per binding. Network I/O runs on the thread pool; the diff is computed and applied on the captured
    /// UI SynchronizationContext (PowerPoint's STA). A running flag prevents overlapping refreshes.
    /// </summary>
    public sealed class LiveRefresher : IDisposable
    {
        readonly PowerPoint.Application _app; readonly SynchronizationContext _ui; readonly Func<string, SourceInfo> _sources;
        readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        readonly CancellationTokenSource _cts = new CancellationTokenSource();
        readonly Dictionary<string, string> _status = new Dictionary<string, string>();
        public bool RedrawViaGoto { get; set; }   // set after the feasibility gate shows Direct does not repaint

        public LiveRefresher(PowerPoint.Application app, SynchronizationContext ui, Func<string, SourceInfo> sources)
        { _app = app; _ui = ui; _sources = sources; }

        public string Status(string bindingId) => _status.TryGetValue(bindingId, out var s) ? s : "idle";

        public void Start(string presentationFullName, string slideId, string shapeName, BindingConfig cfg)
        {
            Task.Run(() => Loop(presentationFullName, shapeName, cfg, _cts.Token));
        }

        async Task Loop(string pres, string shapeName, BindingConfig cfg, CancellationToken ct)
        {
            var backoff = new BackoffPolicy(TimeSpan.FromSeconds(cfg.ClampedRefreshSeconds), TimeSpan.FromSeconds(60));
            var versions = new VersionTracker();
            while (!ct.IsCancellationRequested)
            {
                TimeSpan delay;
                try
                {
                    var src = _sources(cfg.DatasetId);
                    var req = new HttpRequestMessage(HttpMethod.Get, src.BaseUrl.TrimEnd('/') + "/api/live-tables/" + Uri.EscapeDataString(cfg.DatasetId));
                    req.Headers.Add("X-Api-Key", src.ApiKey);
                    using (var resp = await _http.SendAsync(req, ct).ConfigureAwait(false))
                    {
                        resp.EnsureSuccessStatusCode();
                        var snap = SnapshotParser.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
                        if (versions.ShouldApply(snap))
                        {
                            var tcs = new TaskCompletionSource<bool>();
                            _ui.Post(_ => { try { tcs.SetResult(Apply(pres, shapeName, cfg, snap)); } catch (Exception e) { tcs.SetException(e); } }, null);
                            if (await tcs.Task.ConfigureAwait(false)) versions.MarkApplied(snap);
                        }
                        _status[cfg.BindingId] = "ok " + DateTime.Now.ToString("T");
                    }
                    backoff.Success(); delay = backoff.Next;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
                catch (Exception e)
                {
                    _status[cfg.BindingId] = "error (showing last good data): " + e.GetType().Name; // never log the key
                    delay = backoff.Failure();
                }
                try { await Task.Delay(delay, ct).ConfigureAwait(false); } catch (OperationCanceledException) { return; }
            }
        }

        /// <summary>UI thread only. Releases every COM wrapper it creates.</summary>
        bool Apply(string pres, string shapeName, BindingConfig cfg, TableSnapshot snap)
        {
            PowerPoint.Presentation p = null; PowerPoint.Shape shape = null; PowerPoint.Table table = null;
            try
            {
                foreach (PowerPoint.Presentation cand in _app.Presentations) if (cand.FullName == pres) { p = cand; break; }
                if (p == null) return false;
                shape = FindByBindingId(p, cfg.BindingId);
                if (shape == null) { _status[cfg.BindingId] = "table not found (deleted?)"; return false; }
                table = shape.Table;
                var plan = MappingPlanner.Plan(cfg, snap, table.Rows.Count, table.Columns.Count,
                    (r, c) => { var cell = table.Cell(r, c); try { return cell.Shape.TextFrame.TextRange.Text; } finally { Marshal.ReleaseComObject(cell); } },
                    CultureInfo.CurrentCulture);
                foreach (var w in plan.Writes)
                {
                    var cell = table.Cell(w.Row, w.Column);
                    try { cell.Shape.TextFrame.TextRange.Text = w.Text; } finally { Marshal.ReleaseComObject(cell); }
                }
                if (plan.Writes.Count > 0 && RedrawViaGoto && _app.SlideShowWindows.Count > 0)
                {
                    var win = _app.SlideShowWindows[1];
                    try { if (win.View.Slide.SlideIndex == shape.Parent.SlideIndex) win.View.GotoSlide(win.View.Slide.SlideIndex); }
                    finally { Marshal.ReleaseComObject(win); }
                }
                if (plan.Diagnostics.Count > 0) _status[cfg.BindingId] = string.Join("; ", plan.Diagnostics);
                return true;
            }
            finally
            {
                if (table != null) Marshal.ReleaseComObject(table);
                if (shape != null) Marshal.ReleaseComObject(shape);
                if (p != null) Marshal.ReleaseComObject(p);
            }
        }

        static PowerPoint.Shape FindByBindingId(PowerPoint.Presentation p, string id)
        {
            foreach (PowerPoint.Slide s in p.Slides)
                foreach (PowerPoint.Shape sh in s.Shapes)
                    if (sh.HasTable == Microsoft.Office.Core.MsoTriState.msoTrue && sh.Tags["LP_BINDING_ID"] == id) return sh;
            return null;
        }

        public void Dispose() { _cts.Cancel(); _http.Dispose(); }
    }
}
