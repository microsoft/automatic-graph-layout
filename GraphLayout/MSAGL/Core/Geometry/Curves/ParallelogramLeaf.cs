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
