using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Msagl;
using Microsoft.Msagl.ControlForWpfObsolete;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.Msagl.Routing.Rectilinear;
using LineSegment = System.Windows.Media.LineSegment;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Polyline = Microsoft.Msagl.Core.Geometry.Curves.Polyline;
using Shape = Microsoft.Msagl.Routing.Shape;

namespace TestForAvalon {
    internal class PolygonManager {
        int lastGivenId = -1;
        readonly Dictionary<Path, Shape> pathsToShapes = new Dictionary<Path, Shape>();
        readonly Diagram diagram;
        readonly GraphScroller scroller;
        Path currentPath;
        System.Windows.Point pathStart;
        System.Windows.Point pathEnd;
        PathSegmentCollection pathSegmentCollection;
        Dictionary<Shape, Set<Point>> shapesToInnerPoints = new Dictionary<Shape, Set<Point>>();
       

        public PolygonManager(GraphScroller scroller) {
            this.scroller = scroller;
            diagram = scroller.Diagram;
        }

        internal void AttachEvents() {
            scroller.MouseDown += scroller_MouseDown;
            scroller.MouseMove += scroller_MouseMove;
            scroller.MouseUp += scroller_MouseUp;
        }

        internal void DetachEvents() {
            scroller.MouseDown -= scroller_MouseDown;
            scroller.MouseMove -= scroller_MouseMove;
            scroller.MouseUp -= scroller_MouseUp;
        }

        void scroller_MouseUp(object sender, MsaglMouseEventArgs e) {
            MouseUp();
        }

        void scroller_MouseMove(object sender, MsaglMouseEventArgs e) {
            MouseMove(e);
        }

        void scroller_MouseDown(object sender, MsaglMouseEventArgs e) {
            MouseDown(e);
        }

        public void MouseMove(MsaglMouseEventArgs e) {
            if (currentPath == null) return;
            var point = scroller.GetWpfPosition(e);

            if (DistanceBetweenWpfPoints(point, pathEnd) < 3 * currentPath.StrokeThickness) return;

            pathEnd = point;

            pathSegmentCollection.Add(new LineSegment(point, true));
            var pathFigure = new PathFigure(pathStart, pathSegmentCollection, true);
            var pathFigureCollection = new PathFigureCollection { pathFigure };
            var pathGeometry = new PathGeometry(pathFigureCollection);
            currentPath.Data = pathGeometry;
        }

        private double DistanceBetweenWpfPoints(System.Windows.Point a, System.Windows.Point b) {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        


        public void MouseUp() {
            if (currentPath == null)
                return;
            if (pathSegmentCollection.Count > 2) {
                var col=new System.Windows.Media.Color();
                col.A=100;
                col.G=255;

                currentPath.Stroke=new SolidColorBrush(col);
                
                currentPath.ContextMenu = new ContextMenu();
                var path = currentPath;
                currentPath.ContextMenu.Items.Add(GraphScroller.CreateMenuItem("Remove polyline", () => RemovePath(path)));
                PathToShape(currentPath);
            }
            currentPath = null;
        }

        void PathToShape(Path path) {
            var shape= new Shape(PolylineFromPath(path));
            InsertShapeIntoHierarchy(shape);
            pathsToShapes[path] = shape;
        }

        void InsertShapeIntoHierarchy(Shape shape) {
            FindParents(shape);
            FindChildren(shape);
        }

        void FindChildren(Shape shape) {
            Set<Shape> successors = GetShapeSuccessors(shape);
            foreach (var s in successors)
                if (s.Parents.All(h => !successors.Contains(h)))
                    shape.AddChild(s);
        }
        void FindParents(Shape shape) {
            Set<Shape> ancestors = GetShapeAncestors(shape);
            foreach (var s in ancestors)
                if (s.Children.All(h => !ancestors.Contains(h)))
                    s.AddChild(shape);
        }

        Set<Shape> GetShapeAncestors(Shape shape) {
            var ret = new Set<Shape>();
            foreach (var sh in pathsToShapes.Values)
                if (CurveInsideOther(shape.BoundaryCurve, sh.BoundaryCurve))
                    ret.Insert(sh);
            return ret;
        }

        Set<Shape> GetShapeSuccessors(Shape shape) {
            var ret = new Set<Shape>();
            foreach (var sh in pathsToShapes.Values)
                if (CurveInsideOther(sh.BoundaryCurve, shape.BoundaryCurve))
                    ret.Insert(sh);
            return ret;
        }
        ///<summary>
        ///</summary>
        ///<param name="a"></param>
        ///<param name="b"></param>
        ///<returns>true if a is inside b</returns>
        static bool CurveInsideOther(ICurve a, ICurve b) {
            return b.BoundingBox.Contains(a.BoundingBox) && TestPoints(a).All(p => Curve.PointRelativeToCurveLocation(p, b) != PointLocation.Outside);
        }

        static IEnumerable<Point> TestPoints(ICurve curve) {
            const int n = 10;
            double del = (curve.ParEnd - curve.ParStart)/n;
            for (int i = 0; i < n; i++)
                yield return curve[curve.ParStart + i*del];

        }

        static ICurve PolylineFromPath(Path path) {
            return new Polyline(GetPathPoints(path).Select(p => new Point(p.X, p.Y))) { Closed = true };
        }

        static IEnumerable<System.Windows.Point> GetPathPoints(Path path) {
            var pathGeometry = (PathGeometry)path.Data;
            var pathFigure = pathGeometry.Figures.First();
            yield return pathFigure.StartPoint;
            foreach (var lineSeg in pathFigure.Segments.Cast<LineSegment>())
                yield return lineSeg.Point;
        }

        IComparable CreateId() {
            lastGivenId++;
            return "unique"+lastGivenId;
        }


        void RemovePath(Path path) {
            var sh = pathsToShapes[path];
            pathsToShapes.Remove(path);
            diagram.Canvas.Children.Remove(path);
            var listToRemove = new List<Shape>();
            foreach(var parent in sh.Parents)
                listToRemove.Add(parent);
            foreach (var parent in listToRemove) 
                parent.RemoveChild(sh);
            listToRemove.Clear();
            foreach (var child in sh.Children)
                listToRemove.Add(child);
            foreach (var s in listToRemove) 
                s.RemoveParent(sh);
      //      RouteAroundGroups(erm);
        }

        public void MouseDown(MsaglMouseEventArgs e) {
            StartNewPath(scroller.GetWpfPosition(e));
        }

        void StartNewPath(System.Windows.Point point) {
            var color=new System.Windows.Media.Color();
            color.A=255;
            color.R=255;

            currentPath = new Path
            {
                Stroke = new SolidColorBrush(color),
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeThickness = scroller.GetScale()
            };
            pathSegmentCollection = new PathSegmentCollection();
            pathEnd = pathStart = point;
            diagram.Canvas.Children.Add(currentPath);
        }

        public void ClearGroups() {
            diagram.Canvas.Children.Clear();
            pathsToShapes.Clear();
        }

        
       
       

        internal void Tesselate() {
            OrientShapes();
            double minLength = GetMinSideLength();
            SubdividePolylines(minLength);
            EnterInnerPoints(minLength);
            RunTessellation();
        }

        private void SubdividePolylines(double minLength) {
            foreach (var shape in ParentShapes())
                SubdivideUnderShape(shape,minLength);
        }

        private void SubdivideUnderShape(Shape shape, double minLength) {
            SubdividePolyline((Polyline)shape.BoundaryCurve, minLength);
            foreach (var ch in shape.Children)
                SubdivideUnderShape(ch, minLength);
        }

        private void SubdividePolyline(Polyline polyline, double minLength) {
            var q = new Queue<PolylinePoint>();
            foreach (var pp in polyline.PolylinePoints) {
                var ppn = pp.NextOnPolyline; 
                if ((pp.Point - ppn.Point).Length > minLength)
                    q.Enqueue(pp);
            }

            while (q.Count > 0) {
                var pp = q.Dequeue();
                var ppn = pp.NextOnPolyline;
                var p = new PolylinePoint(0.5 * (pp.Point + ppn.Point)) { Polyline = polyline };                 
                bool tooBig=(p.Point-pp.Point).Length>minLength;
                if (pp.Next != null) {
                    p.Prev = pp;
                    p.Next = ppn;
                    pp.Next = p;
                    ppn.Prev = p;
                    if(tooBig){
                        q.Enqueue(pp);
                        q.Enqueue(p);
                    }
                } else {
                    polyline.AddPoint(p.Point);
                    if(tooBig){
                        q.Enqueue(polyline.EndPoint);
                        q.Enqueue(polyline.EndPoint.Prev);
                    }

                }
                                    
            }
   
        }

        private void RunTessellation() {
            foreach(var shape in ParentShapes())
                TesselateUnderShape(shape);
         
        }

        private void TesselateUnderShape(Shape shape) {
            var polys=new List<Polyline>(shape.Children.Select(sh=>(Polyline)sh.BoundaryCurve));
            polys.Add(shape.BoundaryCurve as Polyline);
            var cdt=new Cdt(shapesToInnerPoints[shape], polys,null);
            cdt.Run();
            Set<CdtTriangle> triangles=cdt.GetTriangles();
            cdt.SetInEdges();
            CleanUpTriangles(shape, ref triangles,cdt);
            foreach(var t in triangles){
                AddTriangleToCanvas(t);
            }

        }

        private void CleanUpTriangles(Shape shape, ref Set<CdtTriangle> triangles, Cdt cdt) {
            CleanUpExternalTriangles(shape, ref triangles, cdt);
            foreach (var sh in shape.Children)
                CleanUpInternalTriangles(sh, ref triangles, cdt);
        }

        private void CleanUpInternalTriangles(Shape shape, ref Set<CdtTriangle> triangles, Cdt cdt) {
            Set<CdtTriangle> removedTriangles = new Set<CdtTriangle>();
            var poly = (Polyline)shape.BoundaryCurve;
            for (var p = poly.StartPoint; p.Next != null; p = p.Next)
                FillRemovedForExternalTriangle(p, p.Next, removedTriangles, cdt);

            FillRemovedForExternalTriangle(poly.EndPoint, poly.StartPoint, removedTriangles, cdt);
            //var l = new List<DebugCurve>();
            //l.Add(new DebugCurve(100, 1, "black", poly));
            //l.AddRange(removedTriangles.Select(t => new DebugCurve(100, 0.1, "red", new Polyline(t.Sites.Select(s => s.Point).ToArray()) { Closed = true })));
           // LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
            triangles -= removedTriangles;
        }

        private void CleanUpExternalTriangles(Shape shape, ref Set<CdtTriangle> triangles, Cdt cdt) {
            Set<CdtTriangle> removedTriangles = new Set<CdtTriangle>();
            var poly = (Polyline)shape.BoundaryCurve;
            for (var p = poly.StartPoint; p.Next != null; p = p.Next)
                FillRemovedForExternalTriangle(p, p.Next, removedTriangles, cdt);

            FillRemovedForExternalTriangle(poly.EndPoint, poly.StartPoint, removedTriangles, cdt);
            //var l = new List<DebugCurve>();
            //l.Add(new DebugCurve(100, 1, "black", poly));
            //l.AddRange(removedTriangles.Select(t => new DebugCurve(100, 0.1, "red", new Polyline(t.Sites.Select(s => s.Point).ToArray()) { Closed = true })));
            //LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
            triangles -= removedTriangles;
        }

        private void FillRemovedForExternalTriangle(PolylinePoint a, PolylinePoint b, Set<CdtTriangle> removedTriangles, Cdt cdt) {
            CdtEdge e = FindEdge(a, b, cdt);
            if (e.CcwTriangle == null || e.CwTriangle == null) return;

            bool aligned = a.Point == e.upperSite.Point;
            if (aligned) {               
//                LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(100, 0.1, "black", a.Polyline), new DebugCurve(100, 0.1, "red", new Polyline(e.CcwTriangle.Sites.Select(s => s.Point).ToArray()) { Closed = true }));
                FillRemovedConnectedRegion(e.CcwTriangle, removedTriangles);
            } else {
  //              LayoutAlgorithmSettings.ShowDebugCurves(new DebugCurve(100, 0.1, "black", a.Polyline), new DebugCurve(100,0.1,"red", new Polyline(e.CwTriangle.Sites.Select(s => s.Point).ToArray()) { Closed = true }));
                FillRemovedConnectedRegion(e.CwTriangle, removedTriangles);
            }

        }

        private void FillRemovedConnectedRegion(CdtTriangle t, Set<CdtTriangle> removedTriangles) {
            if (removedTriangles.Contains(t))
                return;
            var q = new Queue<CdtTriangle>();
            q.Enqueue(t);
            while (q.Count > 0) {
                t = q.Dequeue();
                removedTriangles.Insert(t);
                foreach(var e in t.Edges)
                    if (!e.Constrained) {
                        var tr = e.GetOtherTriangle(t);
                        if (tr!=null && !removedTriangles.Contains(tr))
                            q.Enqueue(tr);
                    }
            }
        }

        private CdtEdge FindEdge(PolylinePoint a, PolylinePoint b, Cdt cdt) {
            int aIsAboveB = Cdt.Above(a.Point, b.Point);
            System.Diagnostics.Debug.Assert(aIsAboveB != 0);
            if (aIsAboveB > 0) {
                foreach (var e in cdt.FindSite(a.Point).Edges)
                    if (e.lowerSite.Point == b.Point)
                        return e;
            } else {
                foreach (var e in cdt.FindSite(b.Point).Edges)
                    if (e.lowerSite.Point == a.Point)
                        return e;           
            }

            throw new InvalidOperationException("cannot find an edge of a polyline in the tessellation");

        }

        private void AddTriangleToCanvas(CdtTriangle t) {
            var color = new System.Windows.Media.Color();
            color.A = 100;
            color.B = 255;
            var wpfTriangle = new Polygon {
                Stroke = new SolidColorBrush(color),
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeThickness = scroller.GetScale()
            };

            

            
            PointCollection trianglePoints = new PointCollection();
            trianglePoints.Add(ToWpfPoint( t.Sites[0].Point));
            trianglePoints.Add(ToWpfPoint(t.Sites[1].Point));
            trianglePoints.Add(ToWpfPoint(t.Sites[2].Point));
            wpfTriangle.Points = trianglePoints;
            diagram.Canvas.Children.Add(wpfTriangle);

           
        }

        private System.Windows.Point ToWpfPoint(Point point) {
            return new System.Windows.Point(point.X, point.Y);
        }

        private void EnterInnerPoints(double minLength) {
            Set<Point> points = new Set<Point>();
            foreach (var shape in ParentShapes()) {
                EnterInnerPointsUnderShape(shape, minLength, points);
                shapesToInnerPoints[shape] = points;
            }
        }

        private void EnterInnerPointsUnderShape(Shape shape, double l, Set<Point> points) {
            var h = l * Math.Sqrt(3) / 2*10;
            var box=shape.BoundingBox;
            var p = shape.BoundingBox.LeftBottom;
            bool odd = false;
            while (p.Y < box.Top) {
                AddStringToShape(shape, p, box, l, points);
                p += new Point(odd? l/2:-l/2 , h);
                odd = !odd;
            }
              

        }

        private void AddStringToShape(Shape shape, Point p, Microsoft.Msagl.Core.Geometry.Rectangle box, double l, Set<Point> points) {
            while (p.X <= box.Right) {
                if (IsInsideShape(shape, p)) 
                    points.Insert(p);
                
                p.X += l;
            }
        }

        private bool IsInsideShape(Shape shape, Point p) {
            return Curve.PointRelativeToCurveLocation(p, shape.BoundaryCurve) == PointLocation.Inside &&
                shape.Children.All(ch => Curve.PointRelativeToCurveLocation(p, ch.BoundaryCurve) == PointLocation.Outside);
        }

        private double GetMinSideLength() {
            double l = double.PositiveInfinity;
            foreach (var s in pathsToShapes.Values) {
                var d = s.BoundingBox.Diagonal / 10;
                if (d < l)
                    l = d;
            }
            return l;
        }

        private void OrientShapes() {
            foreach (var shape in ParentShapes())
                OrientUnderShape(shape, true);
        }

        private IEnumerable<Shape> ParentShapes() {
            return pathsToShapes.Values.Where(s => s.Parents == null || s.Parents.Count() == 0);
        }

        private void OrientUnderShape(Shape shape, bool clockwise) {
            shape.BoundaryCurve = OrientPolyline(shape.BoundaryCurve as Polyline, clockwise);
            foreach(var sh in shape.Children)
                OrientUnderShape(sh,!clockwise);
        }

        private ICurve OrientPolyline(Polyline polyline, bool clockwise) {
            bool isClockwise = IsClockwise(polyline);
            if (isClockwise == clockwise)
                return polyline;
            return polyline.Reverse();
        }

        private bool IsClockwise(Polyline polyline) {
            PolylinePoint convexHullPolyPoint = FindConvexHullPolyPoint(polyline);
            return Point.GetTriangleOrientation(convexHullPolyPoint.PrevOnPolyline.Point, convexHullPolyPoint.Point, convexHullPolyPoint.NextOnPolyline.Point) == TriangleOrientation.Clockwise;
        }

        private static PolylinePoint FindConvexHullPolyPoint(Polyline polyline) {
            PolylinePoint convexHullPolyPoint = polyline.StartPoint;

            for (var p = polyline.StartPoint.Next; p != null; p = p.Next) {
                if (p.Point.Y < convexHullPolyPoint.Point.Y)
                    convexHullPolyPoint = p;
                else if (p.Point.Y > convexHullPolyPoint.Point.Y) continue;

                if (p.Point.X < convexHullPolyPoint.Point.X)
                    convexHullPolyPoint = p;
            }
            return convexHullPolyPoint;
        }
    }
}