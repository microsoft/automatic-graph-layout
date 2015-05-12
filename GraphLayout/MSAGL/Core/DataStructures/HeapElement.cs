namespace Microsoft.Msagl.Core.DataStructures {
    class HeapElem {
        internal int indexToA;
        internal double priority;
        internal int v;//value
        internal HeapElem(int index, double priority, int v) {
            this.indexToA = index;
            this.priority = priority;
            this.v = v;
        }
    }
}