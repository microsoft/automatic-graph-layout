using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Geometry;

namespace TestForGdi {
    class Com : IEqualityComparer<Point> {

        
        public bool Equals(Point x, Point y) {
            return x.Equals(y);
        }

        public int GetHashCode(Point obj) {
            return obj.GetHashCode();
        }
    }
}
