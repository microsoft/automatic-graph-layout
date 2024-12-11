using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace DrawingFromGeometryGraphSample {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [SupportedOSPlatform("windows")]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DrawingFromGeometryGraphForm());
        }
    }
}