using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using System.Diagnostics;
using System;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class BundleInfo {
        const double FeasibleWidthEpsilon = 0.1; //??

        internal readonly BundleBase SourceBase;
        internal readonly BundleBase TargetBase;
        readonly Set<Polyline> obstaclesToIgnore;
        readonly internal double EdgeSeparation;
        readonly internal double[] HalfWidthArray;
        readonly double longEnoughSideLength;

        List<Polyline> tightObstaclesInTheBoundingBox;
        internal double TotalRequiredWidth;

        internal BundleInfo(BundleBase sourceBase, BundleBase targetBase, Set<Polyline> obstaclesToIgnore, double edgeSeparation, double[] halfWidthArray) {
            SourceBase = sourceBase;
            TargetBase = targetBase;
            this.obstaclesToIgnore = obstaclesToIgnore;
            EdgeSeparation = edgeSeparation;
            HalfWidthArray = halfWidthArray;
            TotalRequiredWidth = EdgeSeparation * (HalfWidthArray.Length-1) + HalfWidthArray.Sum() * 2;
            longEnoughSideLength = new Rectangle(sourceBase.Curve.BoundingBox, targetBase.Curve.BoundingBox).Diagonal;

            //sometimes TotalRequiredWidth is too large to fit into the circle, so we evenly scale everything
            double mn = Math.Max(sourceBase.Curve.BoundingBox.Diagonal, targetBase.Curve.BoundingBox.Diagonal);
            if (TotalRequiredWidth > mn) {
                double scale = TotalRequiredWidth / mn;
                for (int i = 0; i < HalfWidthArray.Length; i++)
                    HalfWidthArray[i] /= scale;
                TotalRequiredWidth /= scale;
                EdgeSeparation /= scale;
            }
        }

        internal void SetParamsFeasiblySymmetrically(RectangleNode<Polyline, Point> tightTree) {
            CalculateTightObstaclesForBundle(tightTree, obstaclesToIgnore);
            SetEndParamsSymmetrically();
        }

        void CalculateTightObstaclesForBundle(RectangleNode<Polyline, Point> tightTree, Set<Polyline> obstaclesToIgnore) {
            double sRadius = SourceBase.Curve.BoundingBox.Diagonal / 2;
            double tRadius = TargetBase.Curve.BoundingBox.Diagonal / 2;

            //Polyline bundle = Intersections.Create4gon(SourceBase.CurveCenter, TargetBase.CurveCenter, sRadius * 2, tRadius * 2);
            Polyline bundle = Intersections.Create4gon(SourceBase.Position, TargetBase.Position, sRadius * 2, tRadius * 2);

            tightObstaclesInTheBoundingBox = tightTree.AllHitItems(bundle.BoundingBox,
                p => !obstaclesToIgnore.Contains(p) && Curve.ClosedCurveInteriorsIntersect(bundle, p)).ToList();
        }

        void SetEndParamsSymmetrically() {
            Point targetPos = TargetBase.Position;
            Point sourcePos = SourceBase.Position;

            var dir = (targetPos - sourcePos).Normalize();
            var perp = dir.Rotate90Ccw();
            var middle = 0.5 * (targetPos + sourcePos);
            var a = middle + longEnoughSideLength * dir;
            var b = middle - longEnoughSideLength * dir; // [a,b] is a long enough segment

            //we are already fine
            if (SetRLParamsIfWidthIsFeasible(TotalRequiredWidth * perp / 2, a, b)) {
                SetInitialMidParams();
                return;
            }

            //find the segment using binary search
            var uw = TotalRequiredWidth;
            var lw = 0.0;
            var mw = uw / 2;
            while (uw - lw > FeasibleWidthEpsilon) {
                if (SetRLParamsIfWidthIsFeasible(mw * perp / 2, a, b))
                    lw = mw;
                else
                    uw = mw;
                mw = 0.5 * (uw + lw);
            }

            if (mw <= FeasibleWidthEpsilon) {
                //try one side
                if (SetRLParamsIfWidthIsFeasibleTwoPerps(2 * FeasibleWidthEpsilon * perp / 2, new Point(), a, b)) {
                    mw = 2 * FeasibleWidthEpsilon;
                } else if (SetRLParamsIfWidthIsFeasibleTwoPerps(new Point(), -2 * FeasibleWidthEpsilon * perp / 2, a, b)) {
                    mw = 2 * FeasibleWidthEpsilon;
                }
            }

            Debug.Assert(mw > FeasibleWidthEpsilon);

            SourceBase.InitialMidParameter = SourceBase.AdjustParam(SourceBase.ParRight + SourceBase.Span / 2);
            TargetBase.InitialMidParameter = TargetBase.AdjustParam(TargetBase.ParRight + TargetBase.Span / 2);
        }

        bool SetRLParamsIfWidthIsFeasible(Point perp, Point a, Point b) {
            return SetRLParamsIfWidthIsFeasibleTwoPerps(perp, -perp, a, b);
        }

        bool SetRLParamsIfWidthIsFeasibleTwoPerps(Point perpL, Point perpR, Point a, Point b) {
            double sourceRParam, targetRParam, sourceLParam, targetLParam;
            var ls = TrimSegWithBoundaryCurves(new LineSegment(a + perpL, b + perpL), out sourceLParam, out targetRParam);
            if (ls == null)
                return false;
            if (tightObstaclesInTheBoundingBox.Any(t => Intersections.LineSegmentIntersectPolyline(ls.Start, ls.End, t)))
                return false;

            ls = TrimSegWithBoundaryCurves(new LineSegment(a + perpR, b + perpR), out sourceRParam, out targetLParam);
            if (ls == null)
                return false;
            if (tightObstaclesInTheBoundingBox.Any(t => Intersections.LineSegmentIntersectPolyline(ls.Start, ls.End, t)))
                return false;

            if (SourceBase.IsParent) {
                SourceBase.ParRight = sourceLParam;
                SourceBase.ParLeft = sourceRParam;
            }
            else {
                SourceBase.ParRight = sourceRParam;
                SourceBase.ParLeft = sourceLParam;
            }

            //SourceBase.InitialMidParameter = SourceBase.AdjustParam(SourceBase.ParRight + SourceBase.Span / 2);

            if (TargetBase.IsParent) {
                TargetBase.ParRight = targetLParam;
                TargetBase.ParLeft = targetRParam;
            }
            else {
                TargetBase.ParRight = targetRParam;
                TargetBase.ParLeft = targetLParam;
            }

            //TargetBase.InitialMidParameter = TargetBase.AdjustParam(TargetBase.ParRight + TargetBase.Span / 2);

            return true;
        }

        void SetInitialMidParams() {
            double sourceParam, targetParam;
            TrimSegWithBoundaryCurves(new LineSegment(TargetBase.CurveCenter, SourceBase.CurveCenter), out sourceParam, out targetParam);

            SourceBase.InitialMidParameter = sourceParam;
            TargetBase.InitialMidParameter = targetParam;
        }

        LineSegment TrimSegWithBoundaryCurves(LineSegment ls, out double sourcePar, out double targetPar) {
            //ls goes from target to source
            var inters = Curve.GetAllIntersections(ls, SourceBase.Curve, true);
            if (inters.Count == 0) {
                sourcePar = targetPar = 0;
                return null;
            }
            IntersectionInfo i0;
            if (inters.Count == 1)
                i0 = inters[0];
            else {
                if (!SourceBase.IsParent)
                    i0 = inters[0].Par0 < inters[1].Par0 ? inters[0] : inters[1];
                else
                    i0 = inters[0].Par0 < inters[1].Par0 ? inters[1] : inters[0];
            }

            inters = Curve.GetAllIntersections(ls, TargetBase.Curve, true);
            if (inters.Count == 0) {
                sourcePar = targetPar = 0;
                return null;
            }
            IntersectionInfo i1;
            if (inters.Count == 1)
                i1 = inters[0];
            else {
                if (!TargetBase.IsParent)
                    i1 = inters[0].Par0 > inters[1].Par0 ? inters[0] : inters[1];
                else
                    i1 = inters[0].Par0 > inters[1].Par0 ? inters[1] : inters[0];
            }

            sourcePar = i0.Par1;
            targetPar = i1.Par1;
            return new LineSegment(i0.IntersectionPoint, i1.IntersectionPoint);
        }

        internal void RotateBy(int rotationOfSourceRightPoint, int rotationOfSourceLeftPoint, int rotationOfTargetRightPoint, int rotationOfTargetLeftPoint, double parameterChange) {
            bool needToUpdateSource = rotationOfSourceRightPoint != 0 || rotationOfSourceLeftPoint != 0;
            bool needToUpdateTarget = rotationOfTargetRightPoint != 0 || rotationOfTargetLeftPoint != 0;

            if (needToUpdateSource)
                SourceBase.RotateBy(rotationOfSourceRightPoint, rotationOfSourceLeftPoint, parameterChange);

            if (needToUpdateTarget)
                TargetBase.RotateBy(rotationOfTargetRightPoint, rotationOfTargetLeftPoint, parameterChange);

            UpdateSourceAndTargetBases(needToUpdateSource, needToUpdateTarget);
        }

        internal void UpdateSourceAndTargetBases(bool sourceChanged, bool targetChanged) {
            if (sourceChanged)
                UpdatePointsOnBundleBase(SourceBase);
            if (targetChanged)
                UpdatePointsOnBundleBase(TargetBase);

            UpdateTangentsOnBases();
        }

        private void UpdateTangentsOnBases() {
            int count = TargetBase.Count;
            //updating tangents
            for (int i = 0; i < count; i++) {
                Point d = TargetBase.Points[i] - SourceBase.Points[count - 1 - i];
                double len = d.Length;
                if (len >= ApproximateComparer.Tolerance) {
                    d /= len;
                    TargetBase.Tangents[i] = d;
                    SourceBase.Tangents[count - 1 - i] = d.Negate();
                }
            }
        }

        
        void UpdatePointsOnBundleBase(BundleBase bb) {
            int count = bb.Count;

            Point[] pns = bb.Points;
            var ls = new LineSegment(bb.LeftPoint, bb.RightPoint);
            var scale = 1 / TotalRequiredWidth;
            var t = HalfWidthArray[0];
            pns[0] = ls[t*scale];
            for (int i = 1; i < count; i++) {
                t += HalfWidthArray[i-1]+EdgeSeparation + HalfWidthArray[i];
                pns[i] = ls[t * scale];
            }
        }

        internal bool RotationIsLegal(int rotationOfSourceRightPoint, int rotationOfSourceLeftPoint,
            int rotationOfTargetRightPoint, int rotationOfTargetLeftPoint, double parameterChange) {
            //1. we can't have intersections with obstacles
            //(we check borderlines of the bundle only)
            if (!SourceBase.IsParent && !TargetBase.IsParent) {
                if (rotationOfSourceLeftPoint != 0 || rotationOfTargetRightPoint != 0) {
                    Point rSoP = SourceBase.RotateLeftPoint(rotationOfSourceLeftPoint, parameterChange);
                    Point lTarP = TargetBase.RotateRigthPoint(rotationOfTargetRightPoint, parameterChange);
                    if (!LineIsLegal(rSoP, lTarP))
                        return false;
                }

                if (rotationOfSourceRightPoint != 0 || rotationOfTargetLeftPoint != 0) {
                    Point lSoP = SourceBase.RotateRigthPoint(rotationOfSourceRightPoint, parameterChange);
                    Point rTarP = TargetBase.RotateLeftPoint(rotationOfTargetLeftPoint, parameterChange);
                    if (!LineIsLegal(lSoP, rTarP))
                        return false;
                }
            }
            else {
                if (rotationOfSourceLeftPoint != 0 || rotationOfTargetLeftPoint != 0) {
                    Point lSoP = SourceBase.RotateLeftPoint(rotationOfSourceLeftPoint, parameterChange);
                    Point lTarP = TargetBase.RotateLeftPoint(rotationOfTargetLeftPoint, parameterChange);
                    if (!LineIsLegal(lSoP, lTarP))
                        return false;
                }

                if (rotationOfSourceRightPoint != 0 || rotationOfTargetRightPoint != 0) {
                    Point rSoP = SourceBase.RotateRigthPoint(rotationOfSourceRightPoint, parameterChange);
                    Point rTarP = TargetBase.RotateRigthPoint(rotationOfTargetRightPoint, parameterChange);
                    if (!LineIsLegal(rSoP, rTarP))
                        return false;
                }
            }

            //2. we are also not allowed to change the order of bundles around a hub
            if (rotationOfSourceRightPoint != 0 || rotationOfSourceLeftPoint != 0)
                if (!SourceBase.RelativeOrderOfBasesIsPreserved(rotationOfSourceRightPoint, rotationOfSourceLeftPoint, parameterChange))
                    return false;

            if (rotationOfTargetRightPoint != 0 || rotationOfTargetLeftPoint != 0)
                if (!TargetBase.RelativeOrderOfBasesIsPreserved(rotationOfTargetRightPoint, rotationOfTargetLeftPoint, parameterChange))
                    return false;

            return true;
        }

        bool LineIsLegal(Point a, Point b) {
            return tightObstaclesInTheBoundingBox.All(t => !Intersections.LineSegmentIntersectPolyline(a, b, t));
        }
    }
}
