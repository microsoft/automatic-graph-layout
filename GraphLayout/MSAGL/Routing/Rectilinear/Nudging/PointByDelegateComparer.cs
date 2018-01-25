using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging{
    internal class PointByDelegateComparer : IComparer<Point>{

        PointProjection projection;

        public PointByDelegateComparer(PointProjection projection){
            this.projection = projection;
        }

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <returns>
        /// Value 
        ///                     Condition 
        ///                     Less than zero
        ///                 <paramref name="x"/> is less than <paramref name="y"/>.
        ///                     Zero
        ///                 <paramref name="x"/> equals <paramref name="y"/>.
        ///                     Greater than zero
        ///                 <paramref name="x"/> is greater than <paramref name="y"/>.
        /// </returns>
        /// <param name="x">The first object to compare.
        ///                 </param><param name="y">The second object to compare.
        ///                 </param>
        public int Compare(Point x, Point y){
            return  projection(x).CompareTo(projection(y));
        }


    }
}