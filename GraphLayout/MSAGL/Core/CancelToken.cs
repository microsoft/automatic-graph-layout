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

#if PPC
using System.Threading;
using System.Threading.Tasks;
#endif

namespace Microsoft.Msagl.Core {
#if SILVERLIGHT
    public class OperationCanceledException : Exception
    {
    }
#endif


    /// <summary>
    /// a place holder for the cancelled flag
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class CancelToken {

        volatile bool canceled;

        /// <summary>
        /// Set this flag to true when you want to cancel the layout.
        /// </summary>
        public bool Canceled {
            get { return canceled; }
            set { 
                canceled = value;
#if PPC
                if (canceled)
                {
                    cancellationTokenSource.Cancel();
                }
#endif
            }
        }
        /// <summary>
        /// throws is the layout has been cancelled
        /// </summary>
        public void ThrowIfCanceled()
        {
#if PPC
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
#endif
            if (this.Canceled)
                throw new OperationCanceledException();
        }

#if PPC
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Pass this cancelation token for use in cancellable parallel for-each operations
        /// </summary>
        public CancellationToken CancellationToken
        {
            get { return cancellationTokenSource.Token; }
        }
#endif
    }
}
