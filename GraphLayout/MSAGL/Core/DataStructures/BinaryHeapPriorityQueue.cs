using System;

namespace Microsoft.Msagl.Core.DataStructures {


    /// <summary>
    /// A priority queue based on the binary heap algorithm
    /// </summary>
    internal class BinaryHeapPriorityQueue {
        //indexing for A starts from 1

        readonly int[] _heap;//array of heap elements
        readonly int[] _reverse_heap; // the map from [0,..., n-1] to their places in heap
        /// <summary>
        /// the array of priorities
        /// </summary>
        readonly double[] _priors;
        internal int Count { get { return heapSize; } }
        int heapSize;
        /// <summary>
        /// the constructor
        /// we suppose that all integers inserted into the queue will be less then n
        /// </summary>
        /// <param name="n">it is the number of different integers that will be inserted into the queue </param>
        internal BinaryHeapPriorityQueue(int n) {
            _priors = new double[n];
            _heap = new int[n + 1];//because indexing for A starts from 1
            _reverse_heap = new int[n];
        }


        void SwapWithParent(int i) {
            int parent = _heap[i >> 1];
            PutAtI(i >> 1, _heap[i]);
            PutAtI(i, parent);
        }

        internal void Enqueue(int o, double priority) {
            heapSize++;
            int i = heapSize;
            _priors[o] = priority;
            PutAtI(i, o);
            while (i > 1 && _priors[_heap[i >> 1]] > priority) {
                SwapWithParent(i);
                i >>= 1;
            }
        }

        void PutAtI(int i, int h) {
            _heap[i] = h;
            _reverse_heap[h] = i;
        }

        /// <summary>
        /// return the first element of the queue and removes it from the queue
        /// </summary>
        /// <returns></returns>
        internal int Dequeue() {
            if (heapSize == 0)
                throw new InvalidOperationException();
            int ret = _heap[1];
            if (heapSize > 1)
            {
                PutAtI(1, _heap[heapSize]);
                int i = 1;
                while (true)
                {
                    int smallest = i;
                    int l = i << 1;

                    if (l <= heapSize && _priors[_heap[l]] <_priors[ _heap[i]])
                        smallest = l;

                    int r = l + 1;

                    if (r <= heapSize && _priors[_heap[r]] < _priors[_heap[smallest]])
                        smallest = r;

                    if (smallest != i)
                        SwapWithParent(smallest);
                    else
                        break;

                    i = smallest;

                }
            }
            heapSize--;
            return ret;
        }

        /// <summary>
        /// sets the object priority to c
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal void DecreasePriority(int o, double newPriority) {

            //System.Diagnostics.Debug.WriteLine("delcrease "+ o.ToString()+" to "+ newPriority.ToString());

            _priors[o] = newPriority;
            int i = _reverse_heap[o];
            while (i > 1) {
                if (_priors[_heap[i]] < _priors[_heap[i >> 1]])
                    SwapWithParent(i);
                else
                    break;
                i >>= 1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Test() {
            var q = new BinaryHeapPriorityQueue(10);
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
            for (int i = 0; i < 10; i++)
                System.Diagnostics.Debug.WriteLine(q.Dequeue());
        }
    }
}


