using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Visibility;
using System.Linq;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner
{
    internal class ConeSpannerForPortLocations : AlgorithmBase {

        readonly IEnumerable<Polyline> obstacles;
        // double coneAngle = Math.PI / 18;// ten degrees
        //double coneAngle = Math.PI / 9;// twenty degrees
        double coneAngle = 
            Math.PI / 6;// thirty degrees

        //double coneAngle = Math.PI / 4;// 45 degrees
        //double coneAngle = Math.PI / 2;// 90 degrees!

        internal ConeSpannerForPortLocations(IEnumerable<Polyline> obstacles, VisibilityGraph visibilityGraph,
            IEnumerable<Point> portLocationsPointSet) {
            PortLocations = portLocationsPointSet;
            this.obstacles = VisibilityGraph.OrientHolesClockwise(obstacles);
            VisibilityGraph = visibilityGraph;
        }

        protected IEnumerable<Point> PortLocations { get; set; }

        public VisibilityGraph VisibilityGraph { get; private set; }

        internal static int GetTotalSteps(double coneAngle)
        {
            return (int)(2 * Math.PI / coneAngle);
        }

        protected override void RunInternal() {
            //we need to run the full circle of directions since we do not emanate cones from
            //obstacle vertices here
            for (int i = 0; i<GetTotalSteps(coneAngle); i++) {
                var angle = coneAngle*i;
                AddDirection(new Point(Math.Cos(angle), Math.Sin(angle)));
                ProgressStep();
            }
        }

       

        void AddDirection(Point direction) {
            var visibilityGraph = new VisibilityGraph();
            LineSweeperForPortLocations.Sweep(obstacles, direction, coneAngle, visibilityGraph,
                                              PortLocations);
            foreach (var edge in visibilityGraph.Edges)
                VisibilityGraph.AddEdge(edge.SourcePoint, edge.TargetPoint,
                                        ((a, b) => new TollFreeVisibilityEdge(a, b)));
        }

    }
}
