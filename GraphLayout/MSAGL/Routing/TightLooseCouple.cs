using System;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing
{
    /// <summary>
    /// an utility class to keep different polylines created around a shape
    /// </summary>
    internal class TightLooseCouple
    {
        internal Polyline TightPolyline { get; set; }
        internal Shape LooseShape { get; set; }

        internal TightLooseCouple(){}

        public TightLooseCouple(Polyline tightPolyline, Shape looseShape, double distance)
        {
            TightPolyline = tightPolyline;
            LooseShape = looseShape;
            Distance=distance;
        }
        /// <summary>
        /// the loose polyline has been created with this distance
        /// </summary>
        internal double Distance { get; set; }

        /// <summary>
        /// compare just by TightPolyline
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {  
            if(TightPolyline==null)
                throw new InvalidOperationException();
            return TightPolyline.GetHashCode();
        }
        /// <summary>
        /// compare just by TightPolyline
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var couple = obj as TightLooseCouple;
            if(couple==null)
                return false;
            return TightPolyline == couple.TightPolyline;
        }
        public override string ToString()
        {
            return (TightPolyline == null ? "null" : TightPolyline.ToString().Substring(0,5)) + "," + (LooseShape == null ? "null" : LooseShape.ToString().Substring(0,5));
        }
    }
}