using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    internal class ConeSpanner : AlgorithmBase {
        readonly IEnumerable<Polyline> _obstacles;
        // double coneAngle = Math.PI / 18;// ten degrees
        //double coneAngle = Math.PI / 9;// twenty degrees


        readonly VisibilityGraph _visibilityGraph;
        double coneAngle = Math.PI/6; // thirty degrees
        bool _bidirectional;



        internal ConeSpanner(IEnumerable<Polyline> obstacles, VisibilityGraph visibilityGraph) {
            this._obstacles = VisibilityGraph.OrientHolesClockwise(obstacles);
            this._visibilityGraph = visibilityGraph;
        }

        internal ConeSpanner(IEnumerable<Polyline> obstacles, VisibilityGraph visibilityGraph, double coneAngle,
                             Set<Point> ports, Polyline borderPolyline)
            : this(obstacles, visibilityGraph) {
            Debug.Assert(borderPolyline == null || obstacles.All(o => Curve.CurveIsInsideOther(o, borderPolyline)));
            Debug.Assert(borderPolyline == null ||
                         ports.All(o => Curve.PointRelativeToCurveLocation(o, borderPolyline) == PointLocation.Inside));

            //Debug.Assert(obstacles.All(o => ports.All(p => Curve.PointRelativeToCurveLocation(p, o) == PointLocation.Outside)));
            //todo: uncomment this assert - it failes on D:\progression\src\ddsuites\src\vs\Progression\ScenarioTests\Grouping\GroupingResources\GroupBySelection2.dgml
            //when dragging
            Debug.Assert(coneAngle > Math.PI/180*2 && coneAngle <= Math.PI/2);

            Ports = ports;
            BorderPolyline = borderPolyline;
            ConeAngle = coneAngle;
        }

        internal double ConeAngle {
            get { return coneAngle; }
            set { coneAngle = value; }
        }

        Set<Point> Ports { get; set; }
        Polyline BorderPolyline { get; set; }

        /// <summary>
        /// If set to true then a smaller visibility graph is created.
        /// An edge is added to the visibility graph only if it is found at least twice: 
        /// once sweeping with a direction d and the second time with -d
        /// </summary>
        internal bool Bidirectional {
            get { return _bidirectional; }
            set { _bidirectional = value; }
        }

        internal static int GetTotalSteps(double coneAngle) {
#if SHARPKIT //https://github.com/SharpKit/SharpKit/issues/4 integer rounding issue
            return ((int)((2 * Math.PI - coneAngle / 2) / coneAngle)) + 1;
#else
            return (int)((2 * Math.PI - coneAngle / 2) / coneAngle) + 1;
#endif
        }

        protected override void RunInternal() {
            double offset =  2*Math.PI - coneAngle/2;
            if (!Bidirectional) {
                double angle;
                for (int i = 0; (angle = coneAngle*i) <= offset; i++) {
                    ProgressStep();
                    AddDirection(new Point(Math.Cos(angle), Math.Sin(angle)), BorderPolyline, _visibilityGraph);
                }
            }
            else
                HandleBideractionalCase();
        }

        void HandleBideractionalCase() {
            int k = (int)(Math.PI/coneAngle);
            for (int i = 0; i < k; i++) {
                var angle = i*coneAngle;
                var vg0 = new VisibilityGraph();
                AddDirection(new Point(Math.Cos(angle), Math.Sin(angle)), BorderPolyline, vg0);
                var vg1 = new VisibilityGraph();
                AddDirection(new Point(-Math.Cos(angle), -Math.Sin(angle)), BorderPolyline, vg1);
                AddIntersectionOfBothDirectionSweepsToTheResult(vg0, vg1);                
            }

          

        }

        void AddIntersectionOfBothDirectionSweepsToTheResult(VisibilityGraph vg0, VisibilityGraph vg1) {
            foreach (var edge in vg0.Edges)
                if (vg1.FindEdge(edge.SourcePoint, edge.TargetPoint) != null)
                    _visibilityGraph.AddEdge(edge.SourcePoint, edge.TargetPoint);
        }

        void AddDirection(Point direction, Polyline borderPolyline, VisibilityGraph visibilityGraph) {
            LineSweeper.Sweep(_obstacles, direction, coneAngle, visibilityGraph, Ports, borderPolyline);
        }
    }
}