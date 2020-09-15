using System;
using System.ComponentModel;
using Microsoft.Msagl.Routing.Spline.Bundling;

namespace Microsoft.Msagl.Core.Routing {
    ///<summary>
    /// defines egde routing behaviour
    ///</summary>
    [DisplayName("Edge routing settings")]
    [Description("Sets the edge routing method")]

    [TypeConverter(typeof (ExpandableObjectConverter))]
    public class EdgeRoutingSettings {
        EdgeRoutingMode edgeRoutingMode = EdgeRoutingMode.SugiyamaSplines;

        ///<summary>
        /// defines the way edges are routed
        /// 
        ///</summary>
        public EdgeRoutingMode EdgeRoutingMode {
            get { return edgeRoutingMode; }
            set { edgeRoutingMode = value; }
        }

        double coneAngle = 30*Math.PI/180;

        ///<summary>
        /// the angle in degrees of the cones in the routing fith the spanner
        ///</summary>
        public double ConeAngle {
            get { return coneAngle; }
            set { coneAngle = value; }
        }

        double padding = 3;

        /// <summary>
        /// Amount of space to leave around nodes
        /// </summary>
        public double Padding {
            get { return padding; }
            set { padding = value; }
        }

        double polylinePadding = 1.5;

        /// <summary>
        /// Additional amount of padding to leave around nodes when routing with polylines
        /// </summary>
        public double PolylinePadding {
            get { return polylinePadding; }
            set { polylinePadding = value; }
        }

        /// <summary>
        /// For rectilinear, the degree to round the corners
        /// </summary>
        public double CornerRadius { get; set; }

        /// <summary>
        /// For rectilinear, the penalty for a bend, as a percentage of the Manhattan distance between the source and target ports.
        /// </summary>
        public double BendPenalty { get; set; }

        ///<summary>
        ///the settings for general edge bundling
        ///</summary>
        public BundlingSettings BundlingSettings { get; set; }

        
        /// <summary>
        /// For rectilinear, whether to use obstacle bounding boxes in the visibility graph.
        /// </summary>
        public bool UseObstacleRectangles { get; set; }

        double routingToParentConeAngle = Math.PI/6;

        /// <summary>
        /// this is a cone angle to find a relatively close point on the parent boundary
        /// </summary>
        public double RoutingToParentConeAngle {
            get { return routingToParentConeAngle; }
            set { routingToParentConeAngle = value; }
        }

        int simpleSelfLoopsForParentEdgesThreshold = 200;

        /// <summary>
        /// if the number of the nodes participating in the routing of the parent edges is less than the threshold 
        /// then the parent edges are routed avoiding the nodes
        /// </summary>
        public int SimpleSelfLoopsForParentEdgesThreshold {
            get { return simpleSelfLoopsForParentEdgesThreshold; }
            set { simpleSelfLoopsForParentEdgesThreshold = value; }
        }

       
        int incrementalRoutingThreshold = 5000000; //debugging
        bool routeMultiEdgesAsBundles = true;

        /// <summary>
        /// defines the size of the changed graph that could be routed fast with the standard spline routing when dragging
        /// </summary>
        public int IncrementalRoutingThreshold {
            get { return incrementalRoutingThreshold; }
            set { incrementalRoutingThreshold = value; }
        }
        /// <summary>
        /// if set to true the original spline is kept under the corresponding EdgeGeometry
        /// </summary>
        public bool KeepOriginalSpline { get; set; }

        /// <summary>
        /// if set to true routes multi edges as ordered bundles, when routing in a spline mode
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public bool RouteMultiEdgesAsBundles {
            get { return routeMultiEdgesAsBundles; }
            set { routeMultiEdgesAsBundles = value; }
        }
    }
}