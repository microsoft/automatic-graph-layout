using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Core.DataStructures {


    /// <summary>
    /// A generic version priority queue based on the binary heap algorithm where
    /// the priority of each element is passed as a parameter.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class GenericBinaryHeapPriorityQueue<T> : IEnumerable<T> {

        const int InitialHeapCapacity = 16;


        // ReSharper disable InconsistentNaming
        GenericHeapElement<T>[] A;//array of heap elements
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// it is a mapping from queue elements and their correspondent HeapElements
        /// </summary>
        readonly Dictionary<T, GenericHeapElement<T>> cache;
        internal int Count { get { return heapSize; } }
        int heapSize;

        internal bool ContainsElement(T key) {
            return cache.ContainsKey(key);
        }


        internal GenericBinaryHeapPriorityQueue() {
            cache = new Dictionary<T, GenericHeapElement<T>>();
            A = new GenericHeapElement<T>[InitialHeapCapacity + 1];
        }


        void SwapWithParent(int i) {
            var parent = A[i >> 1];

            PutAtI(i >> 1, A[i]);
            PutAtI(i, parent);
        }



        internal void Enqueue(T element, double priority) {
            if (heapSize == A.Length - 1) {
                var newA = new GenericHeapElement<T>[A.Length * 2];
                Array.Copy(A, 1, newA, 1, heapSize);
                A = newA;
            }

            heapSize++;
            int i = heapSize;
            A[i] = cache[element] = new GenericHeapElement<T>(i, priority, element);
            while (i > 1 && A[i >> 1].priority.CompareTo(priority) > 0) {
                SwapWithParent(i);
                i >>= 1;
            }          
        }

        internal bool IsEmpty() {
            return heapSize == 0;
        }

        void PutAtI(int i, GenericHeapElement<T> h) {
            A[i] = h;
            h.indexToA = i;
        }

        internal T Dequeue() {
            if (heapSize == 0)
                throw new InvalidOperationException();

            var ret = A[1].v;

            MoveQueueOneStepForward(ret);

            return ret;

        }

        internal T Dequeue(out double priority) {
            if (heapSize == 0) throw new InvalidOperationException();

            var ret = A[1].v;
            priority = A[1].priority;
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

                if (l <= heapSize && A[l].priority.CompareTo(A[i].priority) < 0)
                    smallest = l;

                int r = l + 1;

                if (r <= heapSize && A[r].priority.CompareTo(A[smallest].priority) < 0)
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
            GenericHeapElement<T> h;
            //ignore the element if it is not in the queue
            if (!cache.TryGetValue(element, out h)) return;
                
            //var h = cache[element];
            h.priority = newPriority;
            int i = h.indexToA;
            while (i > 1) {
                if (A[i].priority.CompareTo(A[i >> 1].priority) < 0)
                    SwapWithParent(i);
                else
                    break;
                i >>= 1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        
        /// <summary>
        /// enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() {
            for (int i = 1; i <= heapSize; i++)
                yield return A[i].v;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public T Peek(out double priority) {
            if (Count == 0) {
                priority = 0.0;
                return default(T);
            }
            priority = A[1].priority;
            return A[1].v;         
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
            StringBuilder sb=new StringBuilder();
            foreach (var i in this)
                sb.Append(i + ",");
            return sb.ToString();
        }
  
#endif
    }
}



