using System;

#if PPC
using System.Threading;
using System.Threading.Tasks;
#endif

namespace Microsoft.Msagl.Core {

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
