using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    ///     Keeps the curve of the edge and arrowhead positions
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    public class EdgeGeometry {
        ICurve curve;
        SmoothedPolyline smoothedPolyline;

        /// <summary>
        /// </summary>
        public EdgeGeometry() {
        }

        internal EdgeGeometry(Port sourcePort, Port targetPort) {
            SourcePort = sourcePort;
            TargetPort = targetPort;
        }

        /// <summary>
        /// </summary>
        public Arrowhead SourceArrowhead { get; set; }

        /// <summary>
        /// </summary>
        public Arrowhead TargetArrowhead { get; set; }

        /// <summary>
        ///     Defines the way the edge connects to the source.
        ///     The member is used at the moment only when adding an edge to the graph.
        /// </summary>
        public Port SourcePort { get; set; }

        /// <summary>
        ///     defines the way the edge connects to the target
        ///     The member is used at the moment only when adding an edge to the graph.
        /// </summary>
        public Port TargetPort { get; set; }

        
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture, "{0}->{1}", SourcePort.Location, TargetPort.Location);
        }

        /// <summary>
        ///     edge thickness
        /// </summary>
        public double LineWidth { get; set; }


        /// <summary>
        ///     A curve representing the edge
        /// </summary>
        public ICurve Curve {
            get { return curve; }
            set {
                RaiseLayoutChangeEvent(value);
                curve = value;                
            }
        }

        /// <summary>
        ///     the polyline of the untrimmed spline
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Polyline")]
        public SmoothedPolyline SmoothedPolyline {
            get { return smoothedPolyline; }
            set { smoothedPolyline = value; }
        }

        /// <summary>
        ///     getting the bounding box of the curve and optional arrow heads
        /// </summary>
        public Rectangle BoundingBox {
            get {
                Rectangle bBox = Curve.BoundingBox;
                if (SourceArrowhead != null)
                    bBox.Add(SourceArrowhead.TipPosition);
                if (TargetArrowhead != null)
                    bBox.Add(TargetArrowhead.TipPosition);
                double del = 0.5*LineWidth;
                var delta = new Point(-del, del);
                bBox.Add(bBox.LeftTop + delta);
                bBox.Add(bBox.RightBottom - delta);
                return bBox;
            }
        }
        
        internal void SetSmoothedPolylineAndCurve(SmoothedPolyline poly) {
            SmoothedPolyline = poly;
            Curve = poly.CreateCurve();
        }

        /// <summary>
        ///     Translate all the geometries with absolute positions by the specified delta
        /// </summary>
        /// <param name="delta">vector by which to translate</param>
        public void Translate(Point delta) {
            if (delta.X == 0 && delta.Y == 0) return;
            RaiseLayoutChangeEvent(delta);
            if (Curve != null)
                Curve.Translate(delta);

            if (SmoothedPolyline != null)
                for (Site s = SmoothedPolyline.HeadSite, s0 = SmoothedPolyline.HeadSite;
                     s != null;
                     s = s.Next, s0 = s0.Next)
                    s.Point = s0.Point + delta;

            if (SourceArrowhead != null)
                SourceArrowhead.TipPosition += delta;
            if (TargetArrowhead != null)
                TargetArrowhead.TipPosition += delta;

        }

        internal double GetMaxArrowheadLength() {
            double l = 0;
            if (SourceArrowhead != null)
                l = SourceArrowhead.Length;
            if (TargetArrowhead != null && TargetArrowhead.Length > l)
                return TargetArrowhead.Length;
            return l;
        }

        
        /// <summary>
        /// </summary>
        public event EventHandler<LayoutChangeEventArgs> LayoutChangeEvent;

        
        /// <summary>
        /// </summary>
        /// <param name="newValue"></param>
        public void RaiseLayoutChangeEvent(object newValue) {
            if (LayoutChangeEvent != null)
                LayoutChangeEvent(this, new LayoutChangeEventArgs{DataAfterChange = newValue});
        }
    }
}