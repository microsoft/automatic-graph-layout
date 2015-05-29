namespace Microsoft.Msagl.Core.DataStructures {
    class GenericHeapElement<T> {
        internal int indexToA;
        internal double priority;
        internal T v;//value
        internal GenericHeapElement(int index, double priority, T v) {
            indexToA = index;
            this.priority = priority;
            this.v = v;
        }
    }
}