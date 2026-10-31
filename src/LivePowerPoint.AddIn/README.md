# LivePowerPoint.AddIn (VSTO, .NET Framework 4.8)

**Status: source only. NOT compiled or run** (authored on Linux; VSTO needs Windows + Visual Studio Office tools).
Create the project on Windows: VS 2022 > "PowerPoint VSTO Add-in" (.NET Framework 4.8) named `LivePowerPoint.AddIn`,
replace its generated `ThisAddIn.cs` with the files here, add a reference to `..\LivePowerPoint.Core`, add `Ribbon.xml`
as an embedded resource, then `msbuild /p:Configuration=Release`. Expect to fix compile errors; none were verified.

Files:
- `FeasibilityGate.cs` – the first-gate experiment: writes a table cell every 5 s during a slideshow, with selectable redraw modes, logs to `%LOCALAPPDATA%\LivePowerPoint\gate.log`.
- `ThisAddIn.cs` – lifecycle, STA marshalling, slideshow events.
- `BindingStore.cs` – shape-tag persistence of non-secret binding JSON.
- `LiveRefresher.cs` – async polling (HttpClient, off UI thread), backoff, version skip, COM writes on UI thread.
- `Ribbon.cs` / `Ribbon.xml` – "Live Data" tab.
- Secrets: API key stored per-user with DPAPI (`ProtectedData`, CurrentUser) in `%LOCALAPPDATA%\LivePowerPoint\sources.json`; never in the deck.
