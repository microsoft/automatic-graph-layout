using System;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Outputs run time in debug mode 
    /// </summary>
    internal class TimeMeasurer {
#if TEST_MSAGL
        static Timer timer;
        static TimeMeasurer() {
            timer = new Timer();
            timer.Start();
        }
#endif

        internal delegate void Task();

        internal static void DebugOutput(string str) {
#if TEST_MSAGL
            timer.Stop();
            System.Diagnostics.Debug.Write("{0}: ", String.Format("{0:0.000}", timer.Duration));
            System.Diagnostics.Debug.WriteLine(str);
#endif
        }
    }
}
