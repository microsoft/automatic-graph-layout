using System;

namespace Microsoft.Msagl.Core.Geometry.Curves {

    /// <summary>
    /// A leaf of the ParallelogramNodeOverICurve hierarchy.
    /// Is used in curve intersectons routine.
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    internal class ParallelogramLeaf : ParallelogramNodeOverICurve {
        double low;

        internal double Low {
            get {
                return low;
            }
            set {
                low = value;
            }
        }
        double high;

        internal double High {
            get {
                return high;
            }
            set {
                high = value;
            }
        }

        internal ParallelogramLeaf(double low, double high, Parallelogram box, ICurve seg, double leafBoxesOffset)
            : base(seg, leafBoxesOffset) {
            this.low = low;
            this.high = high;
            this.Parallelogram = box;
        }



        LineSegment chord;

        internal LineSegment Chord {
            get { return chord; }
            set {
                chord = value;
                if (!ApproximateComparer.Close(Seg[low], chord.Start))
                    throw new InvalidOperationException();
            }
        }
    }
}
