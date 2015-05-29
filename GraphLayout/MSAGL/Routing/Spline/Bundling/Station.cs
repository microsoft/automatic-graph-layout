using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    [DebuggerDisplay("[{SerialNumber}] ({Position.X},{Position.Y})")]
#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203
    //SharpKit/Colin - Interface implementations
    // (this needs to be public because it's used elsewhere in an interface implementation)
    public class Station {
#else
    internal class Station {
#endif
        internal Station(int serialNumber, bool isRealNode, Point position) {
            this.SerialNumber = serialNumber;
            this.IsRealNode = isRealNode;
            this.Position = position;
        }

        /// <summary>
        /// id of the station (used for comparison)
        /// </summary>
        internal readonly int SerialNumber;

        /// <summary>
        /// if true the station is a center of an obstacle
        /// </summary>
        internal readonly bool IsRealNode;

        /// <summary>
        /// radius of the corresponding hub
        /// </summary>
        internal double Radius;

        /// <summary>
        /// position of the corresponding hub
        /// </summary>
        internal Point Position;

        /// <summary>
        /// neighbors sorted in counter-clockwise order around the station
        /// </summary>
        internal Station[] Neighbors;

        /// <summary>
        /// it maps each neighbor to its hub
        /// </summary>
        internal Dictionary<Station, BundleBase> BundleBases = new Dictionary<Station, BundleBase>();

        /// <summary>
        /// it maps a node to a set of tight polylines that can contain the node
        /// </summary>
        internal Set<Polyline> EnterableTightPolylines;

        /// <summary>
        /// it maps a node to a set of loose polylines that can contain the node
        /// </summary>
        internal Set<Polyline> EnterableLoosePolylines;

        /// <summary>
        /// MetroNodeInfos corresponding to the node
        /// </summary>
        internal List<MetroNodeInfo> MetroNodeInfos = new List<MetroNodeInfo>();

        /// <summary>
        /// curve of the hub
        /// </summary>
        internal ICurve BoundaryCurve;

        public static bool operator <(Station a, Station b) {
            Debug.Assert(a == b || a.SerialNumber != b.SerialNumber);
            return a.SerialNumber < b.SerialNumber;
        }

        public static bool operator >(Station a, Station b) {
            Debug.Assert(a == b || a.SerialNumber != b.SerialNumber);
            return a.SerialNumber > b.SerialNumber;
        }

        #region cache

        /// <summary>
        /// triangle of cdt where the station is situated
        /// </summary>
        internal CdtTriangle CdtTriangle;

        internal double cachedRadiusCost;

        internal double cachedBundleCost;

        internal double cachedIdealRadius;

        #endregion

        internal void AddEnterableLoosePolyline(Polyline poly) {
            if (EnterableLoosePolylines == null)
                EnterableLoosePolylines = new Set<Polyline>();
            EnterableLoosePolylines.Insert(poly);
        }
        internal void AddEnterableTightPolyline(Polyline poly) {
            if (EnterableTightPolylines == null)
                EnterableTightPolylines = new Set<Polyline>();
            EnterableTightPolylines.Insert(poly);
        }
    }
}