using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    // Partial Silverlight implementation of the Parallel class.
    public static class Parallel
    {
        private static TaskFactory Factory { get; set; }

        static Parallel()
        {
            Factory = new TaskFactory();
        }

        public static void ForEach<T>(IEnumerable<T> source, ParallelOptions parallelOptions, Action<T> body)
        {
            Task.WaitAll(source.Select(obj => Factory.StartNew(o => body((T)o), obj)).ToArray());
        }
    }

    public class ParallelOptions
    {
        public ParallelOptions()
        {
        }
    }
}
