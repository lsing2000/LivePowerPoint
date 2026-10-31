using System;
using System.Threading;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;

namespace LivePowerPoint.AddIn
{
    public partial class ThisAddIn
    {
        internal LiveRefresher Refresher; internal FeasibilityGate Gate;

        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            // Startup runs on the Office UI STA thread; capture its context for marshalling.
            var ui = SynchronizationContext.Current ?? new System.Windows.Forms.WindowsFormsSynchronizationContext();
            Refresher = new LiveRefresher(Application, ui, SourceRegistry.Lookup);
            Application.SlideShowBegin += wn => { /* start a loop per bound table in wn.Presentation */ };
            Application.SlideShowEnd += pres => { /* loops stop with Dispose; status stays for Diagnostics */ };
            Application.PresentationCloseFinal += pres => { };
        }
        private void ThisAddIn_Shutdown(object sender, EventArgs e) { Refresher?.Dispose(); Gate?.Dispose(); }
        protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject() => new Ribbon();
        private void InternalStartup() { Startup += ThisAddIn_Startup; Shutdown += ThisAddIn_Shutdown; }
    }
}
