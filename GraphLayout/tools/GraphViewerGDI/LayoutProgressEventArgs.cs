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
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// A class representing arguments of the layout progress.
    /// </summary>
    public class LayoutProgressEventArgs : EventArgs {
        internal LayoutProgress progress;
        internal string diagnostics;
        /// <summary>
        /// returning LayoutProgress
        /// </summary>
        public LayoutProgress Progress {
            get { return progress; }
        }
        /// <summary>
        /// Returning diagnostics (in case aborted because of a crash)
        /// </summary>
        public string Diagnostics {
            get { return diagnostics; }
        }

        internal LayoutProgressEventArgs(LayoutProgress progress, string diagnostics) {
            this.progress = progress;
            this.diagnostics = diagnostics;
        }
    }
}
