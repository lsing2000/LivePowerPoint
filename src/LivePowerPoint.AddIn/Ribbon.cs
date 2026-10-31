using System.Runtime.InteropServices;
using Microsoft.Office.Core;

namespace LivePowerPoint.AddIn
{
    // Handlers are intentionally thin stubs: the link/map dialogs (WinForms) are TODO, see docs/TEST_RESULTS.md.
    [ComVisible(true)]
    public class Ribbon : IRibbonExtensibility
    {
        bool _auto = true;
        public string GetCustomUI(string id) { using (var s = typeof(Ribbon).Assembly.GetManifestResourceStream("LivePowerPoint.AddIn.Ribbon.xml")) using (var r = new System.IO.StreamReader(s)) return r.ReadToEnd(); }
        public void Ribbon_Load(IRibbonUI ui) { }
        public void OnSources(IRibbonControl c) { }
        public void OnLink(IRibbonControl c) { }
        public void OnMap(IRibbonControl c) { }
        public void OnRefresh(IRibbonControl c) { }
        public void OnAuto(IRibbonControl c, bool pressed) { _auto = pressed; }
        public bool GetAutoPressed(IRibbonControl c) => _auto;
        public void OnUnlink(IRibbonControl c) { }
        public void OnDiagnostics(IRibbonControl c) { }
        public void OnGate(IRibbonControl c) { Globals.ThisAddIn.Gate = Globals.ThisAddIn.Gate ?? new FeasibilityGate(Globals.ThisAddIn.Application); Globals.ThisAddIn.Gate.Start(); }
    }
}
