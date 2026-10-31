# Install (planned, UNTESTED) and limitations
Build on Windows: open VSTO project in VS 2022, Release, Project > Publish (ClickOnce) to `installer/publish`, sign with a
certificate (test cert = trust prompt; label as TEST BUILD), then compute `Get-FileHash -Algorithm SHA256`.
End users need: Office 365 desktop (match bitness), .NET Framework 4.8, VSTO Runtime. Uninstall: Settings > Apps, or
`VSTOInstaller.exe /u <path>.vsto`; delete `%LOCALAPPDATA%\LivePowerPoint`.

Limitations: no Windows verification of anything add-in related; no installer; fixed-size tables; polling only;
native slideshow repaint unproven; overlay fallback not built; overlay (if built) is not native, absent from PPT/PDF export.
