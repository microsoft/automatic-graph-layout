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
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Dot2Graph {
    /// <summary>
    /// This class keeps information about edge geometry;
    /// Bezier curve and arrows settings: it is mostly for DOT support
    /// </summary>
    [Serializable]
    internal class PosData {

        /// <summary>
        /// Control points of consequent Bezier segments.
        /// </summary>
        private List<Point> controlPoints; //array of Points

        ICurve edgeCurve; //one of two is zero
/// <summary>
/// gets or sets the edge curve
/// </summary>
        internal ICurve EdgeCurve {
            get { return edgeCurve; }
            set { edgeCurve = value; }
        }

        /// <summary>
        /// enumerates over the edge control points
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
       
        internal IEnumerable<Point> ControlPoints {
            get {
                if (this.edgeCurve == null) {
                    if (this.controlPoints == null)
                        controlPoints = new List<Point>();
                    return controlPoints;
                }

                return EnumerateCurvePoints();
            }
        }

        IEnumerable<Point> EnumerateCurvePoints() {

              Curve curve = EdgeCurve as Curve;
              if (curve == null) {
                  //maybe it is a line
                  LineSegment lineSeg = this.EdgeCurve as LineSegment;
                  if (lineSeg == null)
                      throw new System.InvalidOperationException("unexpected curve type");
                  Point a = lineSeg.Start;
                  Point b = lineSeg.End;
                   yield return  a;
                  yield return 2.0 / 3.0 * a + b / 3.0;
                  yield return a / 3.0 + 2.0 * b / 3.0;
                  yield return b;
              } else {

                  yield return (curve.Segments[0] as CubicBezierSegment).B(0);
                  foreach (CubicBezierSegment bez in curve.Segments) {
                      yield return bez.B(1);
                      yield return bez.B(2);
                      yield return bez.B(3);
                  }
              }
             
      }


        /// <summary>
        /// Signals if an arrow should be drawn at the end.
        /// </summary>
        private bool arrowAtTarget;
/// <summary>
/// gets ro sets if an arrow head at target is needed
/// </summary>
        internal bool ArrowAtTarget {
            get { return arrowAtTarget; }
            set { arrowAtTarget = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        private Point arrowAtTargetPosition;
/// <summary>
/// gets or sets the position of the arrow head at the target node
/// </summary>
        internal Point ArrowAtTargetPosition {
            get { return arrowAtTargetPosition; }
            set { arrowAtTargetPosition = value; }
        }
        /// <summary>
        /// Signals if an arrow should be drawn at the beginning.
        /// </summary>
        private bool arrowAtSource;

        /// <summary>
        /// if set to true then we need to draw an arrow head at source
        /// </summary>
        internal bool ArrowAtSource {
            get { return arrowAtSource; }
            set { arrowAtSource = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        private Point arrowAtSourcePosition;
        /// <summary>
        /// the position of the arrow head at the source node
        /// </summary>
        internal Point ArrowAtSourcePosition {
            get { return arrowAtSourcePosition; }
            set { arrowAtSourcePosition = value; }
        }
    }
}
								
