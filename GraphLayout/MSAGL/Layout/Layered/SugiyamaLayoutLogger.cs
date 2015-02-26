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
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Msagl.Layout.Layered {
#if REPORTING

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

            Console.WriteLine(message);
        }
    }
#endif
}