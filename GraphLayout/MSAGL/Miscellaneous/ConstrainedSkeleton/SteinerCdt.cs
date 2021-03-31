using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Routing.Visibility;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;
//using Microsoft.Msagl.Miscellaneous.Rounded;

namespace Microsoft.Msagl.Miscellaneous.ConstrainedSkeleton
{
    internal class SteinerCdt
    {
        Dictionary<Point, int> _pointsToIndices = new Dictionary<Point, int>();
        readonly List<Point> _pointList = new List<Point>();
        readonly Set<SymmetricTuple<int>> _segments = new Set<SymmetricTuple<int>>();

        public Dictionary<int, VisibilityVertex> _outPoints = new Dictionary<int, VisibilityVertex>();
        public VisibilityGraph _visGraph;
        readonly IEnumerable<LgNodeInfo> _nodeInfos;
        Rectangle _boundingBox;
        readonly Random _random = new Random(3);
        public SteinerCdt(VisibilityGraph visGraph, IEnumerable<LgNodeInfo> nodeInfos)
        {
            _visGraph = visGraph;
            _nodeInfos = nodeInfos;
            //comment out by jyoti
            //MakeSureThatNodeBoundariesAreInVisGraph(nodeInfos);
        }

        void MakeSureThatNodeBoundariesAreInVisGraph(IEnumerable<LgNodeInfo> nodeInfos)
        {
            foreach (var nodeInfo in nodeInfos)
                foreach (var polypoint in nodeInfo.BoundaryOnLayer.PolylinePoints)
                    _visGraph.AddVertex(polypoint.Point);
        }

        public void SaveInputFilePoly(string path)
        {
            IndexPoints();
            InitSegments();

            Debug.Assert(TopologyForCallingTriangleIsCorrect());

            using (var file = new StreamWriter(path))
            {
                WritePoints(file);
                WriteSegments(file);
                WriteHoles(file);
            }
        }

        void WriteHoles(StreamWriter file)
        {
            file.WriteLine("# holes");
            file.WriteLine(_nodeInfos.Count());
            int j = 0;
            foreach (var ni in _nodeInfos)
            {
                var c = ni.Center;
                file.WriteLine((j + 1) + " " + c.X + " " + c.Y);
                j++;
            }
        }

        void WriteSegments(StreamWriter file)
        {
            file.WriteLine("# segments");
            file.WriteLine(_segments.Count + " 0");
            int i = 1;
            foreach (var seg in _segments)
            {
                file.WriteLine(i + " " + (seg.A + 1) + " " + (seg.B + 1));
                i++;
            }
        }

        void WritePoints(StreamWriter file)
        {
            file.WriteLine("# vertices");
            file.WriteLine(_pointsToIndices.Count + " 2 0 0");
            foreach (var tuple in _pointsToIndices)
            {
                file.WriteLine(tuple.Value + 1 + " " + tuple.Key.X + " " + tuple.Key.Y);
            }
        }

        public void LoadOutputFileNode(string path)
        {
            int oldPoints = _pointList.Count;
            using (var file = new StreamReader(path))
            {
                string line = file.ReadLine();
                if (line == null) throw new InvalidOperationException("unexpected end of file");
                Regex.Split(line, @"\s{2,}");

                int numVertices = Int32.Parse(line.Split(' ').First());

                for (int i = 0; i < numVertices; i++)
                {
                    line = file.ReadLine();
                    if (line == null) break;

                    line = line.TrimStart(' ');
                    var lineParsed = Regex.Split(line, @"\s{2,}");
                    int ind = Int32.Parse(lineParsed[0]) - 1;
                    if (ind < oldPoints)
                        _outPoints[ind] = _visGraph.FindVertex(_pointList[ind]);
                    else
                    {
                        double x = Double.Parse(lineParsed[1]);
                        double y = Double.Parse(lineParsed[2]);
                        _outPoints[ind] = _visGraph.AddVertex(new Point(x, y));
                    }
                }
            }
        }


        public void LoadOutputFileSides(string path)
        {
            _visGraph.ClearEdges();
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                if (line == null) throw new Exception("unexpected end of file");
                line = line.TrimStart(' ');

                int numTriangles = Int32.Parse(line.Split(' ').First());

                for (int i = 0; i < numTriangles; i++)
                {
                    line = file.ReadLine();
                    if (line == null) break;

                    line = line.TrimStart(' ');
                    var lineParsed = Regex.Split(line, @"\s{2,}");
                    int id0 = Int32.Parse(lineParsed[1]) - 1;
                    int id1 = Int32.Parse(lineParsed[2]) - 1;
                    int id2 = Int32.Parse(lineParsed[3]) - 1;

                    var v0 = _outPoints[id0];
                    var v1 = _outPoints[id1];
                    var v2 = _outPoints[id2];
                    AddVisEdge(v0, v1);
                    AddVisEdge(v1, v2);
                    AddVisEdge(v2, v0);
                }
            }
        }

        public void AddVisEdge(VisibilityVertex v0, VisibilityVertex v1)
        {
            if (v0 != v1)
            {
                VisibilityGraph.AddEdge(v0, v1);
            }
        }

        void InitSegment(Point p1, Point p2)
        {
            int id0 = _pointsToIndices[p1];
            int id1 = _pointsToIndices[p2];
            _segments.Insert(new SymmetricTuple<int>(id0, id1));
        }

        void IndexPoints()
        {
            _pointsToIndices.Clear();
            IndexVisGraphVertices();
            IndexNodeInfos();
            CreateAndIndexBoundingBox();
        }

        void IndexNodeInfos()
        {
            foreach (var ni in _nodeInfos)
            {
                AddNodeBoundaryToPointIndices(ni);
            }
        }

        void IndexVisGraphVertices()
        {
            if (_visGraph == null) return;
            foreach (var v in _visGraph.Vertices())
            {
                _pointsToIndices[v.Point] = _pointsToIndices.Count;
                _pointList.Add(v.Point);
            }
        }

        void CreateAndIndexBoundingBox()
        {
            _boundingBox = Rectangle.CreateAnEmptyBox();
            foreach (var p in _pointsToIndices.Keys) _boundingBox.Add(p);
            _boundingBox.Pad(1);
            IndexAPoint(_boundingBox.LeftBottom);
            IndexAPoint(_boundingBox.RightBottom);
            IndexAPoint(_boundingBox.LeftTop);
            IndexAPoint(_boundingBox.RightTop);
            //SplineRouter.ShowVisGraph(_visGraph, null, new[] { _boundingBox.Perimeter() }, null);
        }

        void AddNodeBoundaryToPointIndices(LgNodeInfo ni)
        {
            foreach (var polylinePoint in ni.BoundaryOnLayer.PolylinePoints)
                IndexAPoint(polylinePoint.Point);
        }

        void IndexAPoint(Point p)
        {
            if (!_pointsToIndices.ContainsKey(p))
            {
                _pointsToIndices[p] = _pointsToIndices.Count;
                _pointList.Add(p);
                _visGraph.AddVertex(p);
            }
        }

        bool TopologyForCallingTriangleIsCorrect()
        {
            Point[] indexToPoints = new Point[_pointsToIndices.Count];
            foreach (var pp in _pointsToIndices)
            {
                indexToPoints[pp.Value] = pp.Key;
            }

            var tree =
                new RTree<Point,Point>(_pointsToIndices.Keys.Select(p => new KeyValuePair<IRectangle<Point>, Point>(new Rectangle(p), p)));
            var badSegs = (from e in _segments let overlaps = GetPointsOverlappingSeg(e, tree, indexToPoints) where overlaps.Count > 2 select e).ToList();

#if TEST_MSAGL
            if (badSegs.Any())
                ShowInputSegments(badSegs, indexToPoints);
#endif
            return !badSegs.Any();
        }

#if TEST_MSAGL
        private void ShowInputSegments(List<SymmetricTuple<int>> badSegs, Point[] indexToPoints) {
            var l = new List<DebugCurve>();
            foreach (var seg in _segments)
            {
                var p1 = indexToPoints[seg.A];
                var p2 = indexToPoints[seg.B];
                var ls = new LineSegment(p1, p2);

                string color = badSegs.Contains(seg) ? "red" : "black";
                double width = badSegs.Contains(seg) ? 3 : 1;
                l.Add(new DebugCurve(100, width, color, ls));
            }
            LayoutAlgorithmSettings.ShowDebugCurves(l.ToArray());
        }
#endif

        List<Point> GetPointsOverlappingSeg(SymmetricTuple<int> seg, RTree<Point, Point> tree, Point[] indexToPoints)
        {
            Point p0 = indexToPoints[seg.A];
            Point p1 = indexToPoints[seg.B];
            var rect = new Rectangle(p0, p1);
            rect.Pad(1e-5);

            Point[] vts = tree.GetAllIntersecting(rect).ToArray();

            double t;
            var vtsOverlapping = vts.Where(p => Point.DistToLineSegment(p, p0, p1, out t) < 1e-5).ToList();

            vtsOverlapping = vtsOverlapping.OrderBy(p => (p - p0).Length).ToList();
            return vtsOverlapping;
        }

        void InitSegments()
        {
            _segments.Clear();
            InitSegsOfVisGraph();
            foreach (var lgNodeInfo in _nodeInfos)
                InitSegsOfPolyline(lgNodeInfo.BoundaryOnLayer);
            InitSegments(_boundingBox.LeftTop, _boundingBox.RightTop, _boundingBox.RightBottom, _boundingBox.LeftBottom);
        }

        void InitSegsOfPolyline(Polyline polyline)
        {
            var pp = polyline.StartPoint;
            int startId = _pointsToIndices[pp.Point];
            int id = startId;
            do
            {
                pp = pp.Next;
                if (pp == null) break;
                int nextId = _pointsToIndices[pp.Point];
                _segments.Insert(new SymmetricTuple<int>(id, nextId));
                id = nextId;
            } while (true);
            _segments.Insert(new SymmetricTuple<int>(id, startId));
        }

        void InitSegments(params Point[] pts)
        {
            for (int i = 0; i < pts.Length - 1; i++)
                InitSegment(pts[i], pts[i + 1]);

            InitSegment(pts[pts.Length - 1], pts[0]);
        }

        void InitSegsOfVisGraph()
        {
            if (_visGraph == null) return;
            foreach (var v in _visGraph.Vertices())
            {
                int vId = _pointsToIndices[v.Point];
                foreach (var e in v.OutEdges)
                    _segments.Insert(new SymmetricTuple<int>(vId, _pointsToIndices[e.Target.Point]));
            }
        }


        public void LaunchTriangleExe(string pathExe, string arguments)
        {
#if !SHARPKIT
            const string triangleMessage =
                "Cannot start Triangle.exe To build Triangle.exe, please open http://www.cs.cmu.edu/~quake/triangle.html and build it by following the instructions from the site. Copy Triange.exe to a directory in your PATH." +
                "Unfortunately we cannot distribute Triangle.exe because of the license restrictions.";
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = pathExe,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = arguments
            };

            try
            {
                using (Process exeProcess = Process.Start(startInfo))
                {
                    if (exeProcess == null)
                    {
                        Environment.Exit(1);
                    }
                    exeProcess.WaitForExit();
                    if (exeProcess.ExitCode != 0)
                        Environment.Exit(exeProcess.ExitCode);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(triangleMessage);
                System.Diagnostics.Debug.WriteLine("Exiting now.");
                Environment.Exit(1);
            }
#endif
        }

        internal void ReadTriangleOutputAndPopulateTheLevelVisibilityGraphFromTriangulation()
        {
            string outPath = Path.Combine(Path.GetTempPath(), "pointssegments" + _random.Next(10000));
            SaveInputFilePoly(outPath + ".poly");
            const string exePath = "triangle.exe";
            string arguments = outPath + ".poly -c -q10 -S100000000";
            LaunchTriangleExe(exePath, arguments);
            LoadOutputFileNode(outPath + ".1.node");
            LoadOutputFileSides(outPath + ".1.ele");
            File.Delete(outPath + ".poly");
            File.Delete(outPath + ".1.node");
            File.Delete(outPath + ".1.ele");
            File.Delete(outPath + ".1.poly");
        }
    }
}
