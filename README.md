# LivePowerPoint — delivery status: PARTIAL, NOT COMPLETE

Built on **Linux (no Windows, no PowerPoint)**. The primary objective (native table updating visibly in a full-screen
slideshow) is **UNVERIFIED** and the project is **not done** per `ACCEPTANCE_TESTS.md`.

| Deliverable | State |
|---|---|
| `src/LivePowerPoint.Core` (parser, mapping planner, formatter, backoff, version tracking) | Built, 31 tests pass (`dotnet test`) |
| `src/LivePowerPoint.DataService` (ASP.NET Core + SQLite, API-key auth, allow-listed datasets, dev-only demo write) | Built, tested, smoke-run |
| `samples/` (`LiveTableDemo.pptx`, `sample-database.sql`, `change-demo-data.ps1/.sh`, `make_sample_deck.py`) | Created. Deck has 2 styled native tables on 2 slides, **not yet linked** (link with the add-in) |
| `src/LivePowerPoint.AddIn` (VSTO source) | **Uncompiled, unrun.** Ribbon handlers for link/map/refresh/diagnostics are **stubs**; link/map dialogs not written. No `.csproj` |
| Feasibility gate (`FeasibilityGate.cs`) | Source written, **never executed** |
| Overlay fallback | **Not implemented** |
| Installer / `.vsto` / SHA-256 / signing | **Not produced** (needs Windows + VS) |
| Evidence videos, Zoom proof, soak tests | **None exist** |

## Run the backend (any OS with .NET 10 SDK)
```
dotnet test
$env:ASPNETCORE_ENVIRONMENT="Development"; $env:ASPNETCORE_URLS="http://127.0.0.1:5080"
dotnet run --project src/LivePowerPoint.DataService --no-launch-profile
samples/change-demo-data.ps1            # or samples/change-demo-data.sh 150000
```
Dev API key is `demo-key-change-me` (Development only). In Production set `LivePowerPoint__ApiKey` (service refuses to start
without it), use HTTPS behind a reverse proxy, and the `/api/demo/update` endpoint does not exist.

## Smallest Windows test needed next
On a Windows 11 + PowerPoint 365 machine with VS 2022 (Office/SharePoint workload): create the VSTO project (see
`src/LivePowerPoint.AddIn/README.md`), run "Run Feasibility Gate" on `LiveTableDemo.pptx` slide 1 in full-screen show,
screen-record 3 writes in `Direct` mode, then `Reselect` mode. Send me the recording and `gate.log`; the result decides
native path vs. overlay fallback.

## Known unknowns / risks
- Whether PowerPoint repaints an in-progress slideshow after `TextRange.Text` changes is **unknown**; community reports vary by build. If `Direct` fails, `GotoSlide` re-render replays transitions/animations (documented drawback) and the WPF overlay would be a non-native fallback needing owner approval.
- Windows-only: Core targets netstandard2.0 so the add-in (net48) can consume it; `System.Text.Json` on net48 must be verified.
- Licenses: Microsoft.Data.Sqlite (MIT), xunit (Apache-2.0), python-pptx (MIT), System.Text.Json (MIT). Own code: owner to declare license.
