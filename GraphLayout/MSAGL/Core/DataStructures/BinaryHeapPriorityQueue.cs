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

namespace Microsoft.Msagl.Core.DataStructures {


    /// <summary>
    /// A priority queue based on the binary heap algorithm
    /// </summary>
    internal class BinaryHeapPriorityQueue {
        //indexing for A starts from 1

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        //internal void Clear()
        //{
        //    if(heapSize>0)
        //    {
        //        for(int i=0;i<cache.Length;i++)
        //            this.cache[i]=null;

        //        heapSize=0; 
        //    }
        //}


// ReSharper disable InconsistentNaming
        readonly HeapElem[] A;//array of heap elements
// ReSharper restore InconsistentNaming
        /// <summary>
        /// cache[k]=A[cache[k].indexToA] this is the invariant
        /// </summary>
        readonly HeapElem[] cache;
        internal int Count { get { return heapSize; } }
        int heapSize;
        /// <summary>
        /// the constructor
        /// we suppose that all integers inserted into the queue will be less then n
        /// </summary>
        /// <param name="n">it is the number of different integers that will be inserted into the queue </param>
        internal BinaryHeapPriorityQueue(int n) {
            cache = new HeapElem[n];
            A = new HeapElem[n + 1];//because indexing for A starts from 1
        }


        void SwapWithParent(int i) {
            HeapElem parent = A[i >> 1];

            PutAtI(i >> 1, A[i]);
            PutAtI(i, parent);
        }

        internal void Enqueue(int o, double priority) {
            //System.Diagnostics.Debug.WriteLine("insert "+ o.ToString() + " with pr "+ priority.ToString());

            heapSize++;
            int i = heapSize;
       
            System.Diagnostics.Debug.Assert(cache[o] == null);
            A[i] = cache[o] =new HeapElem(i, priority, o);
            while (i > 1 && A[i >> 1].priority.CompareTo(priority) > 0) {
                SwapWithParent(i);
                i >>= 1;
            }
       
        }

        internal bool IsEmpty() {
            return heapSize == 0;
        }

        void PutAtI(int i, HeapElem h) {
            A[i] = h;
            h.indexToA = i;
        }

        ///// <summary>
        ///// return the first element of the queue without removing it
        ///// </summary>
        ///// <returns></returns>
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        //internal int Peek() {
        //    return A[1].v;
        //}

        /// <summary>
        /// return the first element of the queue and removes it from the queue
        /// </summary>
        /// <returns></returns>
        internal int Dequeue() {
            if (heapSize == 0)
                throw new InvalidOperationException();

            int ret = A[1].v;

            cache[ret] = null;

            //			System.Diagnostics.Debug.WriteLine("del_min "+ ret.ToString()+" with prio "+A[1].priority.ToString() );
            if (heapSize > 1)
            {
                PutAtI(1, A[heapSize]);
                int i = 1;
                while (true)
                {
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

            HeapElem h = cache[o];
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
                Console.WriteLine(q.Dequeue());
        }
    }
}


