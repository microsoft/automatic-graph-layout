using System;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// an utility class to keep different polylines created around a shape
    /// </summary>
    internal class TightLooseCouple {
        internal Polyline TightPolyline { get; set; }
        internal Shape LooseShape { get; set; }

        internal TightLooseCouple() { }

        public TightLooseCouple(Polyline tightPolyline, Shape looseShape, double distance) {
            TightPolyline = tightPolyline;
            LooseShape = looseShape;
            Distance = distance;
        }
        /// <summary>
        /// the loose polyline has been created with this distance
        /// </summary>
        internal double Distance { get; set; }
        public override string ToString() {
            return (TightPolyline == null ? "null" : TightPolyline.ToString().Substring(0, 5)) + "," + (LooseShape == null ? "null" : LooseShape.ToString().Substring(0, 5));
        }
    }
}