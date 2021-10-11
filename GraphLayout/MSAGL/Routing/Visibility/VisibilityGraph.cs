using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Routing.Visibility {
  /// <summary>
  /// the visibility graph
  /// </summary>
  public class VisibilityGraph {

    Dictionary<VisibilityVertex, VisibilityEdge> _prevEdgesDictionary = new Dictionary<VisibilityVertex, VisibilityEdge>();

    internal Dictionary<VisibilityVertex, int> visVertexToId = new Dictionary<VisibilityVertex, int>();


    internal void ClearPrevEdgesTable() { _prevEdgesDictionary.Clear(); }

    internal void ShrinkLengthOfPrevEdge(VisibilityVertex v, double lengthMultiplier) {
      _prevEdgesDictionary[v].LengthMultiplier = lengthMultiplier;
    }
    /// <summary>
    /// needed for shortest path calculations
    /// </summary>        
    internal VisibilityVertex PreviosVertex(VisibilityVertex v) {
      VisibilityEdge prev;
      if (!_prevEdgesDictionary.TryGetValue(v, out prev))
        return null;
      if (prev.Source == v)
        return prev.Target;
      return prev.Source;
    }

    internal void SetPreviousEdge(VisibilityVertex v, VisibilityEdge e) {
      Debug.Assert(v == e.Source || v == e.Target);
      _prevEdgesDictionary[v] = e;
    }



    /// <summary>
    /// the default is just to return VisibilityVertex
    /// </summary>
    Func<Point, VisibilityVertex> vertexFactory = (point => new VisibilityVertex(point));

    internal Func<Point, VisibilityVertex> VertexFactory {
      get { return vertexFactory; }
      set { vertexFactory = value; }
    }

    readonly Dictionary<Point, VisibilityVertex> pointToVertexMap =
        new Dictionary<Point, VisibilityVertex>();


    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathStart"></param>
    /// <param name="pathEnd"></param>
    /// <param name="obstacles"></param>
    /// <param name="sourceVertex">graph vertex corresponding to the source</param>
    /// <param name="targetVertex">graph vertex corresponding to the target</param>
    /// <returns></returns>
    internal static VisibilityGraph GetVisibilityGraphForShortestPath(Point pathStart, Point pathEnd,
                                                                      IEnumerable<Polyline> obstacles,
                                                                      out VisibilityVertex sourceVertex,
                                                                      out VisibilityVertex targetVertex) {
      var holes = new List<Polyline>(OrientHolesClockwise(obstacles));
      var visibilityGraph = CalculateGraphOfBoundaries(holes);
      var polygons = holes.Select(hole => new Polygon(hole)).ToList();

      TangentVisibilityGraphCalculator.AddTangentVisibilityEdgesToGraph(polygons, visibilityGraph);
      PointVisibilityCalculator.CalculatePointVisibilityGraph(holes, visibilityGraph, pathStart,
                                                              VisibilityKind.Tangent, out sourceVertex);
      PointVisibilityCalculator.CalculatePointVisibilityGraph(holes, visibilityGraph, pathEnd,
                                                              VisibilityKind.Tangent, out targetVertex);

      return visibilityGraph;
    }

    /// <summary>
    /// Calculates the tangent visibility graph
    /// </summary>
    /// <param name="obstacles">a list of polylines representing obstacles</param>
    /// <returns></returns>
    public static VisibilityGraph FillVisibilityGraphForShortestPath(IEnumerable<Polyline> obstacles) {
      var holes = new List<Polyline>(OrientHolesClockwise(obstacles));
      var visibilityGraph = CalculateGraphOfBoundaries(holes);

      var polygons = holes.Select(hole => new Polygon(hole)).ToList();

      TangentVisibilityGraphCalculator.AddTangentVisibilityEdgesToGraph(polygons, visibilityGraph);
      return visibilityGraph;
    }

    static internal VisibilityGraph CalculateGraphOfBoundaries(List<Polyline> holes) {
      var graphOfHoleBoundaries = new VisibilityGraph();
      foreach (Polyline polyline in holes)
        graphOfHoleBoundaries.AddHole(polyline);
      return graphOfHoleBoundaries;
    }

    internal void AddHole(Polyline polyline) {
      var p = polyline.StartPoint;
      while (p != polyline.EndPoint) {
        AddEdge(p, p.Next);
        p = p.Next;
      }
      AddEdge(polyline.EndPoint, polyline.StartPoint);
    }


    internal static IEnumerable<Polyline> OrientHolesClockwise(IEnumerable<Polyline> holes) {
#if TEST_MSAGL || VERIFY
      CheckThatPolylinesAreConvex(holes);
#endif // TEST || VERIFY
      foreach (Polyline poly in holes) {
        for (PolylinePoint p = poly.StartPoint; ; p = p.Next) {
          // Find the first non-collinear segments and see which direction the triangle is.
          // If it's consistent with Clockwise, then return the polyline, else return its Reverse.
          var orientation = Point.GetTriangleOrientation(p.Point, p.Next.Point, p.Next.Next.Point);
          if (orientation != TriangleOrientation.Collinear) {
            yield return orientation == TriangleOrientation.Clockwise ? poly : (Polyline)poly.Reverse();
            break;
          }
        }
      }
    }

#if TEST_MSAGL || VERIFY
    internal static void CheckThatPolylinesAreConvex(IEnumerable<Polyline> holes) {
      foreach (var polyline in holes)
        CheckThatPolylineIsConvex(polyline);
    }

    [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
    internal static void CheckThatPolylineIsConvex(Polyline polyline) {
      Debug.Assert(polyline.Closed, "Polyline is not closed");
      PolylinePoint a = polyline.StartPoint;
      PolylinePoint b = a.Next;
      PolylinePoint c = b.Next;

      TriangleOrientation orient = Point.GetTriangleOrientation(a.Point, b.Point, c.Point);
      while (c != polyline.EndPoint) {
        a = a.Next;
        b = b.Next;
        c = c.Next;
        var currentOrient = Point.GetTriangleOrientation(a.Point, b.Point, c.Point);
        if (currentOrient == TriangleOrientation.Collinear) continue;

        if (orient == TriangleOrientation.Collinear)
          orient = currentOrient;
        else if (orient != currentOrient)
          throw new InvalidOperationException();
      }

      var o = Point.GetTriangleOrientation(polyline.EndPoint.Point, polyline.StartPoint.Point,
                                           polyline.StartPoint.Next.Point);
      if (o != TriangleOrientation.Collinear && o != orient)
        throw new InvalidOperationException();
    }
#endif // TEST || VERIFY

    /// <summary>
    /// Enumerate all VisibilityEdges in the VisibilityGraph.
    /// </summary>
    public IEnumerable<VisibilityEdge> Edges {
      get {
        return PointToVertexMap.Values.SelectMany(vertex => vertex.OutEdges);
      }
    }

    internal Dictionary<Point, VisibilityVertex> PointToVertexMap {
      get { return pointToVertexMap; }
    }

    internal int VertexCount { get { return PointToVertexMap.Count; } }


    internal VisibilityVertex AddVertex(PolylinePoint polylinePoint) {
      return AddVertex(polylinePoint.Point);
    }


    internal VisibilityVertex AddVertex(Point point) {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=370
            //SharpKit/Colin - http://code.google.com/p/sharpkit/issues/detail?id=277
            VisibilityVertex currentVertex;
            if (PointToVertexMap.TryGetValue(point, out currentVertex))
                return currentVertex;

            var newVertex = VertexFactory(point);
            PointToVertexMap[point] = newVertex;
            return newVertex;
#else
      VisibilityVertex vertex;
      return !PointToVertexMap.TryGetValue(point, out vertex)
             ? (PointToVertexMap[point] = VertexFactory(point))
             : vertex;
#endif
    }

    internal void AddVertex(VisibilityVertex vertex) {
      Debug.Assert(!PointToVertexMap.ContainsKey(vertex.Point), "A vertex already exists at this location");
      PointToVertexMap[vertex.Point] = vertex;

    }

    internal bool ContainsVertex(Point point) {
      return PointToVertexMap.ContainsKey(point);
    }


    static internal VisibilityEdge AddEdge(VisibilityVertex source, VisibilityVertex target) {
      VisibilityEdge visEdge;
      if (source.TryGetEdge(target, out visEdge))
        return visEdge;
      if (source == target) {
        Debug.Assert(false, "Self-edges are not allowed");
        throw new InvalidOperationException("Self-edges are not allowed");
      }

      var edge = new VisibilityEdge(source, target);

      source.OutEdges.Insert(edge);
      target.AddInEdge(edge);
      return edge;
    }

    void AddEdge(PolylinePoint source, PolylinePoint target) {
      AddEdge(source.Point, target.Point);
    }

    static internal void AddEdge(VisibilityEdge edge) {
      Debug.Assert(edge.Source != edge.Target);
      edge.Source.OutEdges.Insert(edge);
      edge.Target.AddInEdge(edge);
    }

    internal VisibilityEdge AddEdge(Point source, Point target) {
      VisibilityEdge edge;
      var sourceV = FindVertex(source);
      VisibilityVertex targetV = null;
      if (sourceV != null) {
        targetV = FindVertex(target);
        if (targetV != null && sourceV.TryGetEdge(targetV, out edge))
          return edge;
      }

      if (sourceV == null) { //then targetV is also null
        sourceV = AddVertex(source);
        targetV = AddVertex(target);
      }
      else if (targetV == null)
        targetV = AddVertex(target);
      edge = new VisibilityEdge(sourceV, targetV);
      sourceV.OutEdges.Insert(edge);
      targetV.AddInEdge(edge);
      return edge;
    }

    /*
    internal static bool DebugClose(Point target, Point source)
    {
        var a = new Point(307, 7);
        var b = new Point(540.6, 15);

        return (target - a).Length < 2 && (source - b).Length < 5 || 
            (source - a).Length < 2 && (target - b).Length<5;
    }
    */

    internal VisibilityEdge AddEdge(Point source, Point target, Func<VisibilityVertex, VisibilityVertex, VisibilityEdge> edgeCreator) {
      VisibilityEdge edge;
      var sourceV = FindVertex(source);
      VisibilityVertex targetV = null;
      if (sourceV != null) {
        targetV = FindVertex(target);
        if (targetV != null && sourceV.TryGetEdge(targetV, out edge))
          return edge;
      }

      if (sourceV == null) { //then targetV is also null
        sourceV = AddVertex(source);
        targetV = AddVertex(target);
      }
      else if (targetV == null)
        targetV = AddVertex(target);

      edge = edgeCreator(sourceV, targetV);
      sourceV.OutEdges.Insert(edge);
      targetV.AddInEdge(edge);
      return edge;
    }

    internal VisibilityVertex FindVertex(Point point) {
      return PointToVertexMap.TryGetValue(point, out VisibilityVertex v) ? v : null;
    }

    internal VisibilityVertex GetVertex(PolylinePoint polylinePoint) {
      return FindVertex(polylinePoint.Point);
    }

    internal IEnumerable<VisibilityVertex> Vertices() {
      return PointToVertexMap.Values;
    }

    internal void RemoveVertex(VisibilityVertex vertex) {
      // Debug.Assert(PointToVertexMap.ContainsKey(vertex.Point), "Cannot find vertex in PointToVertexMap");

      foreach (var edge in vertex.OutEdges)
        edge.Target.RemoveInEdge(edge);

      foreach (var edge in vertex.InEdges)
        edge.Source.RemoveOutEdge(edge);

      PointToVertexMap.Remove(vertex.Point);
    }

    internal void RemoveEdge(VisibilityVertex v1, VisibilityVertex v2) {
      VisibilityEdge edge;
      if (!v1.TryGetEdge(v2, out edge)) return;
      edge.Source.RemoveOutEdge(edge);
      edge.Target.RemoveInEdge(edge);
    }

    internal void RemoveEdge(Point p1, Point p2) {
      // the order of p1 and p2 is not important.
      VisibilityEdge edge = FindEdge(p1, p2);
      if (edge == null) return;
      edge.Source.RemoveOutEdge(edge);
      edge.Target.RemoveInEdge(edge);
    }

    static internal VisibilityEdge FindEdge(VisibilityEdge edge) {
      if (edge.Source.TryGetEdge(edge.Target, out edge))
        return edge;
      return null;
    }


    internal VisibilityEdge FindEdge(Point source, Point target) {
      var sourceV = FindVertex(source);
      if (sourceV == null)
        return null;
      var targetV = FindVertex(target);
      if (targetV == null)
        return null;

      VisibilityEdge edge;
      if (sourceV.TryGetEdge(targetV, out edge))
        return edge;

      return null;
    }

    static internal void RemoveEdge(VisibilityEdge edge) {
      edge.Source.OutEdges.Remove(edge);//not efficient!
      edge.Target.RemoveInEdge(edge);//not efficient
    }

    public void ClearEdges() {
      foreach (var visibilityVertex in Vertices()) {
        visibilityVertex.ClearEdges();
      }
    }
  }
}