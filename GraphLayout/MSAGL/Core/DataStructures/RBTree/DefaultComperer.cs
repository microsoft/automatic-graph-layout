using System;
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.DataStructures {
    internal class DefaultComperer<T>: IComparer<T> {
        public int Compare(T x, T y) {
            return ((IComparable<T>) x).CompareTo(y);
        }

    }
}
