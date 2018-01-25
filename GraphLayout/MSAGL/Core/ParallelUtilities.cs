using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if PARALLEL_SUPPORTED
using System.Threading.Tasks;
#endif
using System.Threading;
using System.Diagnostics;

namespace Microsoft.Msagl.Core
{
#if PARALLEL_SUPPORTED
    /// <summary>
    /// Helper methods for running tasks in parallel.
    /// </summary>
    internal static class ParallelUtilities
    {
        /// <summary>
        /// Runs the given action on the source items in parallel.  Calls the progress action on the caller thread as items get processed.
        /// </summary>
        /// <typeparam name="T">The type of items in the source enumerable.</typeparam>
        /// <param name="source">The items that action will be applied to.</param>
        /// <param name="action">The action being applied to the items.</param>
        /// <param name="progressAction">The action being called to report progress on the items.  The integer parameter is the number of items processed since the last progress report.</param>
        [Conditional("NET4")]
        public static void ForEach<T>(IEnumerable<T> source, Action<T> action, Action<int> progressAction)
        {
            // Parallel.ForEach operates faster on an array
            T[] sourceArray = source.ToArray();

            AutoResetEvent progressMade = new AutoResetEvent(false);
            
            int itemsProcessed = 0;
            Exception taskException = null;
            CancellationTokenSource cancelSource = new CancellationTokenSource();

            Task task = null;
            Task exceptionHandleTask = null;

            try
            {
                // Execute the Parallel.ForEach call in a separate task since it is a blocking call
                task = new Task(() =>
                    {
                        // Execute the actions in parallel
                        ParallelOptions options = new ParallelOptions();
                        options.CancellationToken = cancelSource.Token;
                        Parallel.ForEach(sourceArray, options, item =>
                            {
                                action(item);
                                Interlocked.Increment(ref itemsProcessed);
                                progressMade.Set();
                            });

                    }, cancelSource.Token);
                exceptionHandleTask = task.ContinueWith(t => { taskException = t.Exception; });
                task.Start();

                // Report progress as it happens until all items have been processed or the task otherwise completes (possibly by exception).
                int progressReported = 0;
                while (progressReported < sourceArray.Length && (progressMade.WaitOne(100) || !exceptionHandleTask.IsCompleted))
                {
                    int newTotal = itemsProcessed;
                    if (newTotal != progressReported)
                    {
                        try
                        {
                            progressAction(newTotal - progressReported);
                        }
                        catch (OperationCanceledException)
                        {
                            // If the progress action caused a cancel exception, end the parallel tasks.
                            cancelSource.Cancel();
                            exceptionHandleTask.Wait();
                            throw;
                        }

                        progressReported = newTotal;
                    }
                }

                // Be sure the task is completely finished. 
                // This will also throw any unhandled exceptions that were not caught by the ContinueWith.
                exceptionHandleTask.Wait();

                HandleParallelException(taskException);
            }
            finally
            {
                if (exceptionHandleTask != null)
                {
                    exceptionHandleTask.Dispose();
                }

                if (task != null)
                {
                    task.Dispose();
                }

                if (progressMade != null)
                {
                    progressMade.Dispose();
                }
            }
        }

        /// <summary>
        /// Handles exceptions from a parallel loop.
        /// </summary>
        private static void HandleParallelException(Exception exception)
        {
            if (exception != null)
            {
                // Nested cancels are propagated as OperationCanceledExceptions.
                AggregateException innerException = exception.InnerException as AggregateException;
                if (innerException != null && innerException.InnerException is OperationCanceledException)
                {
                    throw new OperationCanceledException(Strings.ParallelOperationCanceledExceptionMessage, innerException.InnerException);
                }

                throw exception;
            }
        }
    }
#endif
}
