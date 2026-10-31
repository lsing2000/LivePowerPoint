using System;
using System.IO;
using System.Windows.Forms;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;

namespace LivePowerPoint.AddIn
{
    /// <summary>
    /// Feasibility gate. Every 5 s, while the slideshow is on the table's slide, sets Cell(2,2) text to a timestamp counter.
    /// Modes let the tester discover whether on-screen pixels change:
    ///   Direct   - text write only (the desired native path).
    ///   Reselect - write, then SlideShowWindow.View.GotoSlide(current) (forces re-render; may replay transition/animations).
    /// The tester records the screen and compares; a log line proves only that the COM write happened.
    /// </summary>
    public sealed class FeasibilityGate : IDisposable
    {
        public enum Mode { Direct, Reselect }
        readonly PowerPoint.Application _app;
        readonly Timer _timer = new Timer { Interval = 5000 }; // WinForms timer fires on the UI STA thread
        readonly string _log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LivePowerPoint", "gate.log");
        int _n;
        public Mode RedrawMode { get; set; } = Mode.Direct;

        public FeasibilityGate(PowerPoint.Application app)
        {
            _app = app; Directory.CreateDirectory(Path.GetDirectoryName(_log));
            _timer.Tick += (s, e) => Tick();
        }

        public void Start()
        {
            Log($"START office={_app.Version} build={_app.Build} bitness={(IntPtr.Size == 8 ? 64 : 32)} mode={RedrawMode} " +
                $"monitors={string.Join(";", Array.ConvertAll(Screen.AllScreens, s => s.DeviceName + " " + s.Bounds))}");
            _timer.Start();
        }
        public void Stop() { _timer.Stop(); Log("STOP"); }

        void Tick()
        {
            if (_app.SlideShowWindows.Count == 0) return;
            var win = _app.SlideShowWindows[1];
            var slide = win.View.Slide;
            foreach (PowerPoint.Shape sh in slide.Shapes)
            {
                if (sh.HasTable != Microsoft.Office.Core.MsoTriState.msoTrue) continue;
                var text = $"GATE {++_n} {DateTime.Now:HH:mm:ss}";
                sh.Table.Cell(2, 2).Shape.TextFrame.TextRange.Text = text;
                if (RedrawMode == Mode.Reselect) win.View.GotoSlide(slide.SlideIndex);
                Log($"WRITE n={_n} text='{text}' slide={slide.SlideIndex} mode={RedrawMode}");
                return;
            }
        }
        void Log(string m) => File.AppendAllText(_log, $"{DateTime.Now:O} {m}{Environment.NewLine}");
        public void Dispose() { _timer.Dispose(); }
    }
}
