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
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// A FloatingPort that has an associated Node, which is where we take the Curve,
    /// and we calculate the Location based on an offset from the Center of the RelativeTo node.
    /// </summary>
    public class RelativeFloatingPort : FloatingPort {

     
        /// <summary>
        /// the delegate returning center
        /// </summary>
        public Func<Point> CenterDelegate {
            get;
            private set;
        }
        /// <summary>
        /// the delegate returning center
        /// </summary>
        public Func<ICurve> CurveDelegate {
            get;
            private set;
        }


//        /// <summary>
//        /// The node where we calculate our location and Curve from
//        /// </summary>
//        public Node RelativeTo { get; private set; }

        /// <summary>
        /// An offset relative to the Center of the Node that we use to calculate Location
        /// </summary>
        public virtual Point LocationOffset { get; private set; }
        
        /// <summary>
        /// Create a port relative to a specific node with an offset for the port Location from the nodes center
        /// <param name="curveDelegate"></param>
        /// <param name="centerDelegate"></param>
        /// <param name="locationOffset"></param>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public RelativeFloatingPort(Func<ICurve> curveDelegate, Func<Point> centerDelegate, Point locationOffset)
            : base(null, centerDelegate() + locationOffset) {
            LocationOffset = locationOffset;
            CurveDelegate = curveDelegate;
            CenterDelegate = centerDelegate;
        }
        
        /// <summary>
        /// Create a port relative to the center of a specific node
        /// </summary>
        /// <param name="curveDelegate"></param>
        /// <param name="centerDelegate"></param>
        public RelativeFloatingPort(Func<ICurve> curveDelegate, Func<Point> centerDelegate)
            : this(curveDelegate, centerDelegate, new Point()) {
        }
        
        /// <summary>
        /// Get the location = CenterDelegate() + LocationOffset
        /// </summary>
        public override Point Location {
            get {
                return CenterDelegate() + LocationOffset;
            }
        }
        /// <summary>
        /// Get the curve from the node's BoundaryCurve
        /// </summary>
        public override ICurve Curve {
            get {
                return CurveDelegate();
            }
        }
    }
}
