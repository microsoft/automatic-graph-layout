//
// DevTrace.cs
// Development-time tracing, using a #define (DEVTRACE) separate from Release-enabled TRACE.
//
// Copyright Microsoft Corporation.
//
#if !DEBUG

#if DEVTRACE
#error DEVTRACE is only allowed in DEBUG builds
#endif

#else // DEBUG

using System.Diagnostics;

namespace Microsoft.Msagl.DebugHelpers {
#if DEVTRACE
    #if !TRACE
    #error TRACE must be defined if DEVTRACE is
    #endif

    #if !TEST_MSAGL
    // Rectilinear uses some TEST_MSAGL utilities in DEVTRACE
    #error TEST_MSAGL must be defined if DEVTRACE is
    #endif
#else  // DEVTRACE
    static
#endif // DEVTRACE
    internal class DevTrace {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])"), Conditional("DEVTRACE")]
        internal static void Assert(bool condition, string format, params object[] args) {
            Trace.Assert(condition, string.Format(format, args));
        }

#if DEVTRACE
        // Called by the test app.  By default this is added to the Trace.Listeners collection,
        // retaining the DefaultTraceListener, so we can see the output in the debugger's Output
        // window as well.  The .config file can modify this to remove the default listener.
        internal static void AddListenerToFile(string fileName) {
            // Use File.Create to overwrite any existing file.
            Trace.Listeners.Add(new TextWriterTraceListener(System.IO.File.Create(fileName)));
        }

        // The message level; unrelated to the TraceLevel from the config file, this just
        // identifies a consistent prefix for warnings and errors.
        internal enum Level {
            Info
            , Warning
            , Error
        }

        // The switch from the config file.  Its TraceLevel is used as the verbosity level.
        TraceSwitch Switch { get; set; }

        // 'name' should be qualified so it is apparent which module uses it, e.g.
        // "Rectilinear_ScanLineTrace".  Caller usually maintains this DevTrace instance
        // inside #if DEVTRACE, and a conditional function wrapper to call it, e.g.:
        //   #if DEVTRACE
        //      DevTrace ScanLineDevTrace;
        //   #endif // DEVTRACE
        //
        //   [Conditional("DEVTRACE")]
        //   void ScanLineTraceInfo(...) {
        //   #if DEVTRACE
        //       DevTrace.WriteLine(...);
        //   #endif // DEVTRACE
        //   }
        // This eliminates the need for #if DEVTRACE...#endif at each callsite.
        //
        // The test app must also have an <appname>.exe.config file in the same directory as <appname>.exe.
        // See TestRectilinear for an example of contents.  This allows tracing to be configurable per-test-app,
        // both for which switches to use and what level of output or verification is desired.
        // So to summarize, it's a 2-step process for the client:
        //   1.  Set up <appname>.exe.config (with its Properties set for it to be copied to the bin directory)
        //   2.  Either modify that .config file to create a listener to your file, or (best for multiple runs)
        //       add a test-app parameter for the filename to create (see TestRectilinear).  Note that some
        //       of the possible switches are for validation rather than output; those don't require a file.
        // To enable tracing inside MSAGL, it's two or three more steps:
        //   a.  If a switch doesn't exist yet, create it as noted above (and put it in the .config files of
        //       apps that are interested in it).
        //   b.  Insert appropriate calls in the code.
        //   c.  Modify the tracelevel in the .config file of the app you're using, from 0 (off) to 1 (high level)
        //       to 4 (low-level, very verbose or detailed verification).

        internal DevTrace(string switchName) : this(switchName, "") {}
        internal DevTrace(string switchName, string prefix) {
            this.Switch = new TraceSwitch(switchName, prefix);
            Trace.WriteLine(string.Format("Trace switch {0} level: {1}", switchName
                    , (TraceLevel.Off == Switch.Level) ? "Off" : ((int)Switch.Level).ToString()));
        }

        internal bool IsLevel(int level) {
            return (int)Switch.Level >= level;
        }

        // VerboseLevel is filtered by the config-file value, as an int rather than Error, Warning, etc.
        // which would be confusing with TraceLevel.  Higher numbers mean more output or verification.
        // MessageLevel is *not* related to the switch value from the config file, it just identifies
        // a consistent prefix.
        internal void WriteLineIf(DevTrace.Level messageLevel, int verboseLevel, string format, params object[] args) {
            if (IsLevel(verboseLevel)) {
                Trace.IndentLevel = verboseLevel;
                string message = Switch.Description;
                if (!string.IsNullOrEmpty(message)) {
                    message += ": ";
                } 
                message += string.Format(format, args);
                if (DevTrace.Level.Warning == messageLevel) {
                    message = "*** Trace Warning *** " + message;
                }
                if (DevTrace.Level.Error == messageLevel) {
                    message = "*** Trace Error *** " + message;
                }
                Trace.WriteLine(message);
            }
        }

        // Usually verboseLevel is 0 for warnings and errors.
        internal void WriteWarning(int verboseLevel, string format, params object[] args) {
            WriteLineIf(DevTrace.Level.Warning, verboseLevel, format, args);
            FlushListeners();
        }

        internal void WriteError(int verboseLevel, string format, params object[] args) {
            WriteLineIf(DevTrace.Level.Error, verboseLevel, format, args);
            FlushListeners();
        }

        // Followup info for Warning or Error that doesn't fit cleanly on a single line.
        internal void WriteFollowup(int verboseLevel, string format, params object[] args) {
            if (IsLevel(verboseLevel)) {
                Trace.IndentLevel = 8;
                Trace.WriteLine(string.Format(format, args));
            }
        }

        internal void FlushListeners() {
            // Warnings or Errors may be followed by an assert so let's make sure the files are flushed.
            foreach (TraceListener listener in Trace.Listeners) {
                listener.Flush();
            }
        }

#endif // DEVTRACE
    }
}
#endif // DEBUG