using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// The class calculates obstacles under the shape.
    /// We assume that the boundaries are not set for the shape children yet
    /// </summary>
    internal class ShapeObstacleCalculator {
        RectangleNode<Polyline,Point> tightHierarchy;
        RectangleNode<TightLooseCouple,Point> coupleHierarchy;

        public RectangleNode<Shape,Point> RootOfLooseHierarchy { get; set; }

        internal ShapeObstacleCalculator(Shape shape, double tightPadding, double loosePadding, 
            Dictionary<Shape, TightLooseCouple> shapeToTightLooseCouples) {
            MainShape = shape;
            TightPadding = tightPadding;
            LoosePadding = loosePadding;
            ShapesToTightLooseCouples = shapeToTightLooseCouples;
        }

        Dictionary<Shape, TightLooseCouple> ShapesToTightLooseCouples { get; set; }

        double TightPadding { get; set; }
        double LoosePadding { get; set; }

        Shape MainShape { get; set; }

        internal bool OverlapsDetected { get; set; }


        internal void Calculate() {
            if (MainShape.Children.Count() == 0) return;
            CreateTightObstacles();
            CreateTigthLooseCouples();
            FillTheMapOfShapeToTightLooseCouples();
        }

        void FillTheMapOfShapeToTightLooseCouples() {
            var childrenShapeHierarchy =
                RectangleNode<Shape,Point>.CreateRectangleNodeOnEnumeration(
                    MainShape.Children.Select(s => new RectangleNode<Shape,Point>(s, s.BoundingBox)));
            RectangleNodeUtils.CrossRectangleNodes(childrenShapeHierarchy, coupleHierarchy,
                                                   TryMapShapeToTightLooseCouple);
        }

        void TryMapShapeToTightLooseCouple(Shape shape, TightLooseCouple tightLooseCouple) {
            if (ShapeIsInsideOfPoly(shape, tightLooseCouple.TightPolyline))
                ShapesToTightLooseCouples[shape] = tightLooseCouple;
#if TEST_MSAGL
            tightLooseCouple.LooseShape.UserData = (string) shape.UserData + "x";
#endif
        }


        /// <summary>
        /// this test is valid in our situation were the tight polylines are disjoint and the shape can cross only one of them
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="tightPolyline"></param>
        /// <returns></returns>
        static bool ShapeIsInsideOfPoly(Shape shape, Polyline tightPolyline) {
            return Curve.PointRelativeToCurveLocation(shape.BoundaryCurve.Start, tightPolyline) == PointLocation.Inside;
        }

        void CreateTigthLooseCouples() {
            var couples = new List<TightLooseCouple>();

            foreach (var tightPolyline in tightHierarchy.GetAllLeaves()) {
                var distance = InteractiveObstacleCalculator.FindMaxPaddingForTightPolyline(tightHierarchy, tightPolyline, LoosePadding);
                var loosePoly = InteractiveObstacleCalculator.LoosePolylineWithFewCorners(tightPolyline, distance);
                couples.Add(new TightLooseCouple(tightPolyline, new Shape(loosePoly), distance));
            }
            coupleHierarchy = RectangleNode<TightLooseCouple,Point>.
                CreateRectangleNodeOnEnumeration(couples.Select(c => new RectangleNode<TightLooseCouple,Point>(c, c.TightPolyline.BoundingBox)));
        }

        void CreateTightObstacles() {
            var tightObstacles = new Set<Polyline>(MainShape.Children.Select(InitialTightPolyline));
            int initialNumberOfTightObstacles = tightObstacles.Count;
            tightHierarchy = InteractiveObstacleCalculator.RemovePossibleOverlapsInTightPolylinesAndCalculateHierarchy(tightObstacles);
            OverlapsDetected = initialNumberOfTightObstacles > tightObstacles.Count;
        }

        Polyline InitialTightPolyline(Shape shape) {
            var poly = InteractiveObstacleCalculator.PaddedPolylineBoundaryOfNode(shape.BoundaryCurve, TightPadding);
            var stickingPointsArray = LoosePolylinesUnderShape(shape).SelectMany(l => l).Where(
                p => Curve.PointRelativeToCurveLocation(p, poly) == PointLocation.Outside).ToArray();
            if (stickingPointsArray.Length <= 0) return poly;
            return new Polyline(
                ConvexHull.CalculateConvexHull(poly.Concat(stickingPointsArray))) {
                    Closed = true
                };
        }

        IEnumerable<Polyline> LoosePolylinesUnderShape(Shape shape) {
            return shape.Children.Select(child => (Polyline)(ShapesToTightLooseCouples[child].LooseShape.BoundaryCurve));
        }
    }
}