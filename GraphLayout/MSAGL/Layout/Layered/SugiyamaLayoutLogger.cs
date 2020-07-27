using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Msagl.Layout.Layered {
#if TEST_MSAGL

    /// <summary>
    /// Log class
    /// </summary>
    public sealed class SugiyamaLayoutLogger : IDisposable {
        static StreamWriter sw;
        internal SugiyamaLayoutLogger() {}

        #region IDisposable Members

        /// <summary>
        /// disposes the object
        /// </summary>
        public void Dispose() {
            sw.Close();
            sw.Dispose();
            sw = null;
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// writes a message to the log file
        /// </summary>
        /// <param name="message"></param>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void Write(string message) {
            if (sw == null) sw = new StreamWriter("msaglLogFile");
            sw.Write(message);
            sw.Flush();

            System.Diagnostics.Debug.WriteLine(message);
        }
    }
#endif
}