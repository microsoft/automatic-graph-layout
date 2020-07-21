using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Core.DataStructures {

    // TODO: Move and expand the commented tests below into PriorityQueueTests unit tests for all 3 PQ classes.
    // TODO: Reduce duplication with GenericBinaryHeapPriorityQueue by breaking most functionality out into a 
    //      GenericBinaryHeapPriorityQueue<T, THeapElement> class and using THeapElement.ComparePriority, keeping only Enqueue
    //      and DecreasePriority in the derived classes as these must know about timestamp.

    /// <summary>
    /// A generic version priority queue based on the binary heap algorithm where
    /// the priority of each element is passed as a parameter and priority ties are broken by timestamp.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class GenericBinaryHeapPriorityQueueWithTimestamp<T> : IEnumerable<T> {

        const int InitialHeapCapacity = 16;


        // ReSharper disable InconsistentNaming
        GenericHeapElementWithTimestamp<T>[] A;//array of heap elements
        // ReSharper restore InconsistentNaming

         UInt64 timestamp;
         UInt64 NextTimestamp { get { return ++this.timestamp; } }

        /// <summary>
        /// it is a mapping from queue elements and their correspondent HeapElements
        /// </summary>
        readonly Dictionary<T, GenericHeapElementWithTimestamp<T>> cache;
        internal int Count { get { return heapSize; } }
        int heapSize;

        internal GenericBinaryHeapPriorityQueueWithTimestamp() {
            cache = new Dictionary<T, GenericHeapElementWithTimestamp<T>>();
            A = new GenericHeapElementWithTimestamp<T>[InitialHeapCapacity + 1];
        }


        void SwapWithParent(int i) {
            var parent = A[i >> 1];

            PutAtI(i >> 1, A[i]);
            PutAtI(i, parent);
        }

        internal void Enqueue(T element, double priority) {
            if (heapSize == A.Length - 1) {
                var newA = new GenericHeapElementWithTimestamp<T>[A.Length * 2];
                Array.Copy(A, 1, newA, 1, heapSize);
                A = newA;
            }

            heapSize++;
            int i = heapSize;
            GenericHeapElementWithTimestamp<T> h;

            A[i] = cache[element] = h = new GenericHeapElementWithTimestamp<T>(i, priority, element, this.NextTimestamp);
            while (i > 1 && A[i >> 1].ComparePriority(A[i]) > 0) {
                SwapWithParent(i);
                i >>= 1;
            }
            System.Diagnostics.Debug.Assert(A[i] == h);
            A[i] = h;

        }

        void PutAtI(int i, GenericHeapElementWithTimestamp<T> h) {
            A[i] = h;
            h.indexToA = i;
        }

#if TEST_MSAGL
        /// <summary>
        /// Gets the next element to be dequeued, without dequeueing it.
        /// </summary>
        internal T Peek() {
            if (heapSize == 0) {
                throw new InvalidOperationException();
            }
            return A[1].v;
        }

        /// <summary>
        /// Gets the timestamp of the next element to be dequeued, without dequeueing it.
        /// </summary>
        internal UInt64 PeekTimestamp() {
            if (heapSize == 0) {
                throw new InvalidOperationException();
            }
            return A[1].Timestamp;
        }
#endif // TEST_MSAGL

        internal T Dequeue() {
            if (heapSize == 0)
                throw new InvalidOperationException();

            var ret = A[1].v;

            MoveQueueOneStepForward(ret);

            return ret;

        }

        void MoveQueueOneStepForward(T ret) {
            cache.Remove(ret);
            PutAtI(1, A[heapSize]);
            int i = 1;
            while (true) {
                int smallest = i;
                int l = i << 1;

                if (l <= heapSize && A[l].ComparePriority(A[i]) < 0)
                    smallest = l;

                int r = l + 1;

                if (r <= heapSize && A[r].ComparePriority(A[smallest]) < 0)
                    smallest = r;

                if (smallest != i)
                    SwapWithParent(smallest);
                else
                    break;

                i = smallest;

            }

            heapSize--;
        }

        /// <summary>
        /// sets the object priority to c
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal void DecreasePriority(T element, double newPriority)
        {
            GenericHeapElementWithTimestamp<T> h;
            //ignore the element if it is not in the queue
            if (!cache.TryGetValue(element, out h)) return;
                
            //var h = cache[element];
            h.priority = newPriority;
            int i = h.indexToA;
            while (i > 1) {
                if (A[i].ComparePriority(A[i >> 1]) < 0)
                    SwapWithParent(i);
                else
                    break;
                i >>= 1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Test() {
            var q = new GenericBinaryHeapPriorityQueue<int>();
            q.Enqueue(2, 2);
            q.Enqueue(1, 1);
            q.Enqueue(9, 9);
            q.Enqueue(8, 8);
            q.Enqueue(5, 5);
            q.Enqueue(3, 3);
            q.Enqueue(4, 4);
            q.Enqueue(7, 7);
            q.Enqueue(6, 6);
            q.Enqueue(0, 0);

            q.DecreasePriority(4, 2.5);

            while (q.IsEmpty() == false)
                Console.WriteLine(q.Dequeue());


        }
        /// <summary>
        /// enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() {
            for (int i = 1; i <= heapSize; i++)
                yield return A[i].v;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            for (int i = 1; i <= heapSize; i++)
                yield return A[i].v;
        }
#if TEST_MSAGL
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var sb=new StringBuilder();
            foreach (var i in this)
                sb.Append(i + ",");
            return sb.ToString();
        }
  
#endif
    }
}



