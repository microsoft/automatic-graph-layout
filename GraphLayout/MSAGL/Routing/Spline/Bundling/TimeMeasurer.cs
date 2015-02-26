/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
