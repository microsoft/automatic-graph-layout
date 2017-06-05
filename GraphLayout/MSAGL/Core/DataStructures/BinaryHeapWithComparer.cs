using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Msagl.Core.DataStructures {
    /// <summary>
    /// A priority queue based on the binary heap algorithm.
    /// This class needs a comparer object to compare elements of the queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BinaryHeapWithComparer<T> {
        const int initialHeapCapacity = 16;
        T[] A; //array of the heap elems starting at A[1]

        int heapSize = 0;

        internal void Enqueue(T element) {
            if (heapSize == A.Length - 1) {
                var newA = new T[A.Length*2];
                Array.Copy(A, 1, newA, 1, heapSize);
                A = newA;
            }

            int i = heapSize + 1;
            A[i] = element;
            heapSize++;
            int j = i >> 1;
            T parent, son;
            while (i > 1 && Less(son = A[i], parent = A[j])) {
                A[j] = son;
                A[i] = parent;
                i = j;
                j = i >> 1;
            }
        }


        internal T Dequeue() {
            if (heapSize < 1)
                throw new InvalidOperationException();
            T ret = A[1];

            T candidate = A[heapSize];

            heapSize--;

            ChangeMinimum(candidate);

            return ret;
        }

        internal void ChangeMinimum(T candidate) {
            A[1] = candidate;

            int j = 1;
            int i = 2;
            bool done = false;
            while (i < heapSize && !done) {
                done = true;
                //both sons exist
                T leftSon = A[i];
                T rigthSon = A[i + 1];
                int compareResult = comparer.Compare(leftSon, rigthSon);
                if (compareResult < 0) {
                    //left son is the smallest
                    if (comparer.Compare(leftSon, candidate) < 0) {
                        A[j] = leftSon;
                        A[i] = candidate;
                        done = false;
                        j = i;
                        i = j << 1;
                    }
                } else {
                    //right son in not the greatest
                    if (comparer.Compare(rigthSon, candidate) < 0) {
                        A[j] = rigthSon;
                        A[i + 1] = candidate;
                        done = false;
                        j = i + 1;
                        i = j << 1;
                    }
                }
            }

            if (i == heapSize) {
                //can we do one more step:
                T leftSon = A[i];
                if (comparer.Compare(leftSon, candidate) < 0) {
                    A[j] = leftSon;
                    A[i] = candidate;
                }
            }
        }

        internal int Count {
            get { return heapSize; }
        }

        internal bool Less(T a, T b) {
            return comparer.Compare(a, b) < 0;
        }

        IComparer<T> comparer;

        internal BinaryHeapWithComparer(IComparer<T> comparer) {
            A = new T[initialHeapCapacity + 1];
            this.comparer = comparer;
        }

#if TEST_MSAGL

        public override string ToString() {
            int i = 1;
            return "{" + Print(i) + "}";
        }

        string Print(int i) {
            if (2*i + 1 <= heapSize)
                return String.Format(CultureInfo.InvariantCulture, "({0}->{1},{2})", A[i], A[i*2], A[i*2 + 1]) +
                       Print(i*2) + Print(i*2 + 1);
            if (2*i == heapSize)
                return String.Format(CultureInfo.InvariantCulture, "({0}->{1}", A[i], A[i*2]);
            if (i == heapSize && i == 1)
                return " " + A[i].ToString() + " ";
            return "";
        }
#endif


        public T GetMinimum() {
            return A[1];
        }

#if TEST_MSAGL
        //internal void UpdateMinimum() {
        //    throw new NotImplementedException();
        //}
#endif
    }
}