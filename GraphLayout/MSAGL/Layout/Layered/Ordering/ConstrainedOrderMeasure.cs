namespace Microsoft.Msagl.Layout.Layered {
    internal class ConstrainedOrderMeasure {
        readonly int numberOfCrossings;
        //readonly double deviationFromConstraints;

        internal ConstrainedOrderMeasure(int numberOfCrossings) {
            this.numberOfCrossings = numberOfCrossings;
          //  this.deviationFromConstraints = deviationFromConstraints;
        }

        static public bool operator <(ConstrainedOrderMeasure a, ConstrainedOrderMeasure b) {
            return a.numberOfCrossings < b.numberOfCrossings;
        }


        static public bool operator >(ConstrainedOrderMeasure a, ConstrainedOrderMeasure b) {
            return b < a;
        }
    }
}
