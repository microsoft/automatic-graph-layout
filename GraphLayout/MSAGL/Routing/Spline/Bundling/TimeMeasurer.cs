using System;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Outputs run time in debug mode 
    /// </summary>
    internal class TimeMeasurer {
#if DEBUG && TEST_MSAGL
        static Timer timer;
        static TimeMeasurer() {
            timer = new Timer();
            timer.Start();
        }
#endif
        /// <summary>
        /// Run the task and outputs its execution time
        /// </summary>
        internal static void Run(Task task) {
            Run(MethodName(task), task);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String,System.Object)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "description")]
        internal static void Run(String description, Task task) {
#if DEBUG && TEST_MSAGL
            Timer t = new Timer();
            t.Start();
            task();
            t.Stop();

            Console.Write(description);
            Console.WriteLine(" executed in {0} sec.", String.Format("{0:0.00}", t.Duration));
#else
			//task();
#endif
        }

        internal delegate void Task();

        static string MethodName(Task task) {
            Type tp = task.Target.GetType().DeclaringType;
            String cls = (tp != null ? tp.Name : "");

            String method = task.Method.Name;
            if (method.Length >= 4)
                method = method.Substring(0, method.Length - 4);

            if (cls.Length > 0)
                return cls + "." + method;
            return method;
        }

        internal static void DebugOutput(string str) {
#if DEBUG && TEST_MSAGL
            timer.Stop();
            Console.Write("{0}: ", String.Format("{0:0.000}", timer.Duration));
            Console.WriteLine(str);
#endif
        }
    }
}
