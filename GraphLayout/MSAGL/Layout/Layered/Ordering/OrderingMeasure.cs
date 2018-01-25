using System;

namespace Microsoft.Msagl.Layout.Layered {
    
    internal class OrderingMeasure {
        int numberOfCrossings;
        double layerGroupDisbalance;
        int[][] la;
        int virtVertexStart;
        /// <summary>
        /// for the i-th layer the optimal size of an original group is optimalOriginalGroupSize[i]
        /// </summary>
        double[] optimalOriginalGroupSize;
        /// <summary>
        /// for the i-th layer the optimal size of a virtual group is optimalOriginalGroupSize[i]
        /// </summary>
        double[] optimalVirtualGroupSize;

        internal OrderingMeasure(int[][] layerArraysPar,
            int numOfCrossings, int virtualVertexStart,
            double[] optimalOriginalGroupSizePar,
        double[] optimalVirtualGroupSizePar
) {
            this.numberOfCrossings = numOfCrossings;
            this.la = layerArraysPar;
            this.virtVertexStart = virtualVertexStart;
            this.optimalVirtualGroupSize = optimalVirtualGroupSizePar;
            this.optimalOriginalGroupSize = optimalOriginalGroupSizePar;

            if (optimalOriginalGroupSize != null)
                CalculateLayerGroupDisbalance();
        }

        void CalculateLayerGroupDisbalance() {
            for(int i = 0; i<la.Length;i++)     
                layerGroupDisbalance+=LayerGroupDisbalance(la[i],this.optimalOriginalGroupSize[i],
                    this.optimalVirtualGroupSize[i]);

        }

        double LayerGroupDisbalance(int[] l, double origGroupOptSize, double virtGroupOptSize){
            if (origGroupOptSize == 1)
                return LayerGroupDisbalanceWithOrigSeparators(l,virtGroupOptSize);
            else
                return LayerGroupDisbalanceWithVirtSeparators(l,origGroupOptSize);
        }

        private double LayerGroupDisbalanceWithVirtSeparators(int[] l, double origGroupOptSize) {
            double ret = 0;
            for (int i = 0; i < l.Length; )
                ret += CurrentOrigGroupDelta(ref i, l, origGroupOptSize);
            return ret;
        }

        private double CurrentOrigGroupDelta(ref int i, int[] l, double origGroupOptSize) {
            double groupSize = 0;
            int j = i;
            for (; j < l.Length && l[j] < this.virtVertexStart; j++)
                groupSize++;
            i = j + 1;
            return Math.Abs(origGroupOptSize - groupSize);
        }

        private double LayerGroupDisbalanceWithOrigSeparators(int[] l, double virtGroupOptSize) {
            double ret = 0;
            for (int i = 0; i < l.Length; )
                ret += CurrentVirtGroupDelta(ref i, l, virtGroupOptSize);
            return ret;
        }

        private double CurrentVirtGroupDelta(ref int i, int[] l, double virtGroupOptSize) {
            double groupSize = 0;
            int j = i;
            for (; j < l.Length && l[j] >= this.virtVertexStart; j++)
                groupSize++;
            i = j + 1;
            return Math.Abs(virtGroupOptSize - groupSize);
        }

        static public bool operator<(OrderingMeasure a, OrderingMeasure b){
            if (a.numberOfCrossings < b.numberOfCrossings)
                return true;
            if (a.numberOfCrossings > b.numberOfCrossings)
                return false;
           
            return (int)a.layerGroupDisbalance < (int)b.layerGroupDisbalance;
        }

         static public bool operator>(OrderingMeasure a, OrderingMeasure b){
            if (a.numberOfCrossings > b.numberOfCrossings)
                return true;
            if (a.numberOfCrossings < b.numberOfCrossings)
                return false;

           
            return (int)a.layerGroupDisbalance > (int)b.layerGroupDisbalance;
        }


         internal bool IsPerfect() {
             return this.numberOfCrossings == 0 && this.layerGroupDisbalance == 0;
         }
    }
}
