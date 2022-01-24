using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.Msagl.Routing.Spline.Bundling;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;
using Microsoft.Msagl.Routing.Visibility;

#if TEST_MSAGL
using Microsoft.Msagl.DebugHelpers;
using System.Diagnostics.CodeAnalysis;
#endif

namespace Microsoft.Msagl.Routing {
  ///<summary>
  /// routing splines around shapes
  ///</summary>
  public class SplineRouter : AlgorithmBase {
    /// <summary>
    /// setting this to true forces the calculation to go on even when node overlaps are present
    /// </summary>
    /// 
    bool continueOnOverlaps = true;
    public bool ContinueOnOverlaps { get { return continueOnOverlaps; } set { continueOnOverlaps = value; } }

    Shape[] rootShapes;
    IEnumerable<EdgeGeometry> edgeGeometriesEnumeration {
      get {
        if (this._edges != null) {
          foreach (var item in this._edges.Select(e => e.EdgeGeometry)) {
            yield return item;
          }
        }
      }
    }
    double coneAngle;
    readonly double tightPadding;
    double LoosePadding { get; set; }
    bool rootWasCreated;
    Shape root;
    VisibilityGraph visGraph;
    Dictionary<Shape, Set<Shape>> ancestorSets;
    readonly Dictionary<Shape, TightLooseCouple> shapesToTightLooseCouples = new Dictionary<Shape, TightLooseCouple>();
    Dictionary<Port, Shape> portsToShapes;
    Dictionary<Port, Set<Shape>> portsToEnterableShapes;


    RTree<Point,Point> portRTree;
    readonly Dictionary<Point, Polyline> portLocationsToLoosePolylines = new Dictionary<Point, Polyline>();
    Shape looseRoot;
    internal BundlingSettings BundlingSettings { get; set; }
    Dictionary<EdgeGeometry, Set<Polyline>> enterableLoose;
    Dictionary<EdgeGeometry, Set<Polyline>> enterableTight;


    readonly GeometryGraph geometryGraph;
    double multiEdgesSeparation = 5;
    bool routeMultiEdgesAsBundles = true;
    internal bool UseEdgeLengthMultiplier;

    /// <summary>
    /// if set to true the algorithm will try to shortcut a shortest polyline inner points
    /// </summary>
    public bool UsePolylineEndShortcutting = true;
    /// <summary>
    /// if set to true the algorithm will try to shortcut a shortest polyline start and end
    /// </summary>
    public bool UseInnerPolylingShortcutting = true;

    internal bool AllowedShootingStraightLines = true;

    internal double MultiEdgesSeparation {
      get { return multiEdgesSeparation; }
      set { multiEdgesSeparation = value; }
    }

    /// <summary>
    /// Creates a spline group router for the given graph.
    /// </summary>
    public SplineRouter(GeometryGraph graph, EdgeRoutingSettings edgeRoutingSettings) :
        this(
        graph, edgeRoutingSettings.Padding, edgeRoutingSettings.PolylinePadding, edgeRoutingSettings.ConeAngle,
        edgeRoutingSettings.BundlingSettings) { }


    /// <summary>
    /// Creates a spline group router for the given graph.
    /// </summary>
    public SplineRouter(GeometryGraph graph, double tightTightPadding, double loosePadding, double coneAngle) :
        this(graph, graph.Edges, tightTightPadding, loosePadding, coneAngle, null) {
    }

    /// <summary>
    /// Creates a spline group router for the given graph
    /// </summary>
    public SplineRouter(GeometryGraph graph, double tightTightPadding, double loosePadding, double coneAngle, BundlingSettings bundlingSettings) :
        this(graph, graph.Edges, tightTightPadding, loosePadding, coneAngle, bundlingSettings) {
    }


    /// <summary>
    /// Creates a spline group router for the given graph.
    /// </summary>
    public SplineRouter(GeometryGraph graph, IEnumerable<Edge> edges, double tightPadding, double loosePadding, double coneAngle, BundlingSettings bundlingSettings) {
      ValidateArg.IsNotNull(graph, "graph");
      ValidateArg.IsPositive(tightPadding, "tightPadding");
      ValidateArg.IsPositive(loosePadding, "loosePadding");
      ValidateArg.IsNotNull(edges, "edges");
#if TEST_MSAGL
      // do not run the following check, cluster containment may be incorrect in float mode - need to make sure we handle this
      //graph.CheckClusterConsistency();
#endif

      this._edges = edges.ToArray();

      BundlingSettings = bundlingSettings;
      geometryGraph = graph;
      LoosePadding = loosePadding;
      this.tightPadding = tightPadding;
      IEnumerable<Shape> obstacles = ShapeCreator.GetShapes(geometryGraph);
      Initialize(obstacles, coneAngle);
    }

    readonly Edge[] _edges;

    internal Action<Edge> ReplaceEdgeByRails;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="tightPadding"></param>
    /// <param name="loosePadding"></param>
    /// <param name="coneAngle"></param>
    /// <param name="inParentEdges"></param>
    /// <param name="outParentEdges"></param>
    public SplineRouter(GeometryGraph graph, double tightPadding, double loosePadding, double coneAngle, List<Edge> inParentEdges, List<Edge> outParentEdges) {
#if TEST_MSAGL
      graph.CheckClusterConsistency();
#endif
      geometryGraph = graph;
      LoosePadding = loosePadding;
      this.tightPadding = tightPadding;
      IEnumerable<Shape> obstacles = ShapeCreatorForRoutingToParents.GetShapes(inParentEdges, outParentEdges);
      Initialize(obstacles, coneAngle);
    }

    void Initialize(IEnumerable<Shape> obstacles,
                    double coneAngleValue) {
      rootShapes = obstacles.Where(s => s.Parents == null || !s.Parents.Any()).ToArray();
      coneAngle = coneAngleValue;
      if (coneAngle == 0)
        coneAngle = Math.PI / 6;
    }

    /// <summary>
    /// Executes the algorithm.
    /// </summary>
    protected override void RunInternal() {
      if (!edgeGeometriesEnumeration.Any())
        return;
      GetOrCreateRoot();
      RouteOnRoot();
      RemoveRoot();
      /*     var ll = new List<DebugCurve>();
           ll.AddRange(rootShapes.Select(s=>shapesToTightLooseCouples[s].TightPolyline).Select(p=>new DebugCurve(100,0.05,"black", p)));
           ll.AddRange(geometryGraph.Edges.Select(s => new DebugCurve(100, 0.05, "black", s.Curve)));               
           LayoutAlgorithmSettings.ShowDebugCurvesEnumeration (ll);
           LayoutAlgorithmSettings.ShowGraph(geometryGraph);*/
    }
    void RouteOnRoot() {
      CalculatePortsToShapes();
      CalculatePortsToEnterableShapes();
      CalculateShapeToBoundaries(root);
      if (OverlapsDetected && !ContinueOnOverlaps)
        return;
      BindLooseShapes();
      SetLoosePolylinesForAnywherePorts();
      CalculateVisibilityGraph();
      RouteOnVisGraph();
    }


    /*
    IEnumerable<Polyline> AllPolysDeb() {
        foreach (var rootShape in looseRoot.Children) {
            foreach (var poly in PolysDebugUnderShape(rootShape)) {
                yield return poly;
            }
        }
    }

    IEnumerable<Polyline> PolysDebugUnderShape(Shape shape) {
        yield return (Polyline)shape.BoundaryCurve;
        foreach (var child in shape.Children) {
            foreach (var poly in PolysDebugUnderShape(child)) {
                yield return poly;
            }
        }
    }
    */
    void CalculatePortsToEnterableShapes() {
      portsToEnterableShapes = new Dictionary<Port, Set<Shape>>();
      foreach (var portsToShape in portsToShapes) {
        var port = portsToShape.Key;
        var set = new Set<Shape>();
        if (!(EdgesAttachedToPortAvoidTheNode(port)))
          set.Insert(portsToShape.Value);
        portsToEnterableShapes[port] = set;
      }

      foreach (var rootShape in rootShapes) {
        foreach (var sh in rootShape.Descendants)
          foreach (var port in sh.Ports) {
            var enterableSet = portsToEnterableShapes[port];
            enterableSet.InsertRange(sh.Ancestors.Where(s => s.BoundaryCurve != null));
          }
      }
    }


    static bool EdgesAttachedToPortAvoidTheNode(Port port) {
      return port is CurvePort || port is ClusterBoundaryPort;
    }

    void SetLoosePolylinesForAnywherePorts() {
      foreach (var shapesToTightLooseCouple in shapesToTightLooseCouples) {
        var shape = shapesToTightLooseCouple.Key;
        foreach (var port in shape.Ports) {
          var aport = port as HookUpAnywhereFromInsidePort;
          if (aport != null)
            aport.LoosePolyline = (Polyline)shapesToTightLooseCouple.Value.LooseShape.BoundaryCurve;
          var clusterBoundaryPort = port as ClusterBoundaryPort;
          if (clusterBoundaryPort != null)
            clusterBoundaryPort.LoosePolyline = (Polyline)shapesToTightLooseCouple.Value.LooseShape.BoundaryCurve;
        }
      }
    }

    void BindLooseShapes() {
      looseRoot = new Shape();
#if TEST_MSAGL
      looseRoot.UserData = (string)root.UserData + "x";
#endif
      foreach (var shape in root.Children) {
        var looseShape = shapesToTightLooseCouples[shape].LooseShape;
        BindLooseShapesUnderShape(shape);
        looseRoot.AddChild(looseShape);
      }
    }

    void BindLooseShapesUnderShape(Shape shape) {
      var loose = shapesToTightLooseCouples[shape].LooseShape;
      foreach (var child in shape.Children) {
        var childLooseShape = shapesToTightLooseCouples[child].LooseShape;
        loose.AddChild(childLooseShape);
        BindLooseShapesUnderShape(child);
      }
    }




    void CalculateShapeToBoundaries(Shape shape) {
      ProgressStep();
      if (!shape.Children.Any()) return;

      foreach (Shape child in shape.Children)
        CalculateShapeToBoundaries(child);

      var obstacleCalculator = new ShapeObstacleCalculator(shape, tightPadding, AdjustedLoosePadding,
                                                           shapesToTightLooseCouples);
      obstacleCalculator.Calculate();

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=370
            //SharpKit/Colin - not supported directly!
            OverlapsDetected = OverlapsDetected | obstacleCalculator.OverlapsDetected;
#else
      OverlapsDetected |= obstacleCalculator.OverlapsDetected;
#endif
    }
    /// <summary>
    /// set to true if and only if there are overlaps in tight obstacles
    /// </summary>
    public bool OverlapsDetected { get; set; }

    internal double AdjustedLoosePadding {
      get { return BundlingSettings == null ? LoosePadding : LoosePadding * BundleRouter.SuperLoosePaddingCoefficient; }
    }


    void RouteOnVisGraph() {
      ancestorSets = GetAncestorSetsMap(root.Descendants);
      if (BundlingSettings == null) {
        foreach (var edgeGroup in _edges.GroupBy(EdgePassport)) {
          var passport = edgeGroup.Key;
          Set<Shape> obstacleShapes = GetObstaclesFromPassport(passport);
          var interactiveEdgeRouter = CreateInteractiveEdgeRouter(obstacleShapes);
          RouteEdgesWithTheSamePassport(edgeGroup, interactiveEdgeRouter, obstacleShapes);
        }
      }
      else
        RouteBundles();
    }


    void RouteEdgesWithTheSamePassport(IGrouping<Set<Shape>, Edge> edgeGeometryGroup, InteractiveEdgeRouter interactiveEdgeRouter, Set<Shape> obstacleShapes) {
      List<Edge> regularEdges;
      List<Edge[]> multiEdges;

      if (RouteMultiEdgesAsBundles) {
        SplitOnRegularAndMultiedges(edgeGeometryGroup, out regularEdges, out multiEdges);
        foreach (var edge in regularEdges)
          RouteEdge(interactiveEdgeRouter, edge);
        if (multiEdges != null) {
          ScaleDownLooseHierarchy(interactiveEdgeRouter, obstacleShapes);
          RouteMultiEdges(multiEdges, interactiveEdgeRouter, edgeGeometryGroup.Key);
        }
      }
      else
        foreach (var eg in edgeGeometryGroup)
          RouteEdge(interactiveEdgeRouter, eg);
    }

    /// <summary>
    /// if set to true routes multi edges as ordered bundles
    /// </summary>
    public bool RouteMultiEdgesAsBundles {
      get { return routeMultiEdgesAsBundles; }
      set { routeMultiEdgesAsBundles = value; }
    }

    void RouteEdge(InteractiveEdgeRouter interactiveEdgeRouter, Edge edge) {
      var transparentShapes = MakeTransparentShapesOfEdgeGeometryAndGetTheShapes(edge.EdgeGeometry);
      ProgressStep();
      RouteEdgeGeometry(edge, interactiveEdgeRouter);
      SetTransparency(transparentShapes, false);
    }

    void ScaleDownLooseHierarchy(InteractiveEdgeRouter interactiveEdgeRouter, Set<Shape> obstacleShapes) {
      var loosePolys = new List<Polyline>();
      foreach (var obstacleShape in obstacleShapes) {
        var tl = shapesToTightLooseCouples[obstacleShape];
        loosePolys.Add(InteractiveObstacleCalculator.LoosePolylineWithFewCorners(tl.TightPolyline, tl.Distance / BundleRouter.SuperLoosePaddingCoefficient));
      }

      interactiveEdgeRouter.LooseHierarchy = CreateLooseObstacleHierarachy(loosePolys);

      interactiveEdgeRouter.ClearActivePolygons();

      interactiveEdgeRouter.AddActivePolygons(loosePolys.Select(polyline => new Polygon(polyline)));
    }


    void RouteMultiEdges(List<Edge[]> multiEdges, InteractiveEdgeRouter interactiveEdgeRouter, Set<Shape> parents) {
      var mer = new MultiEdgeRouter(multiEdges, interactiveEdgeRouter, parents.SelectMany(p => p.Children).Select(s => s.BoundaryCurve),
           new BundlingSettings { InkImportance = 0.00001, EdgeSeparation = MultiEdgesSeparation }, MakeTransparentShapesOfEdgeGeometryAndGetTheShapes);
      //giving more importance to ink might produce weird routings with huge detours, maybe 0 is the best value here
      mer.Run();

    }



    //        void ScaleLoosePolylinesOfInvolvedShapesDown(Set<Shape> parents) {        
    //            foreach (var parent in parents) {
    //                foreach (var shape in parent.Descendands) {
    //                    TightLooseCouple tl = this.shapesToTightLooseCouples[shape];
    //                    tl.LooseShape.BoundaryCurveBackup = tl.LooseShape.BoundaryCurve;
    //                    tl.LooseShape.BoundaryCurve = InteractiveObstacleCalculator.LoosePolylineWithFewCorners(tl.TightPolyline, tl.Distance / BundleRouter.SuperLoosePaddingCoefficient);
    //                }
    //            }
    //        }
    //
    //        void RestoreLoosePolylinesOfInvolvedShapes(Set<Shape> parents) {
    //            foreach (var parent in parents) {
    //                foreach (var shape in parent.Descendands) {
    //                    TightLooseCouple tl = shapesToTightLooseCouples[shape];
    //                    tl.LooseShape.BoundaryCurve = tl.LooseShape.BoundaryCurveBackup;
    //                }
    //            }
    //        }

    void SplitOnRegularAndMultiedges(IEnumerable<Edge> edges, out List<Edge> regularEdges, out List<Edge[]> multiEdges) {

      regularEdges = new List<Edge>();

      var portLocationPairsToEdges = new Dictionary<PointPair, List<Edge>>();
      foreach (var eg in edges) {
        if (IsEdgeToParent(eg.EdgeGeometry))
          regularEdges.Add(eg);
        else
          RegisterInPortLocationsToEdges(eg, portLocationPairsToEdges);
      }

      multiEdges = null;

      foreach (var edgeGroup in portLocationPairsToEdges.Values) {
        if (edgeGroup.Count == 1 || OverlapsDetected)
          regularEdges.AddRange(edgeGroup);
        else {
          if (multiEdges == null)
            multiEdges = new List<Edge[]>();

          multiEdges.Add(edgeGroup.ToArray());
        }
      }
    }

    static void RegisterInPortLocationsToEdges(Edge eg, Dictionary<PointPair, List<Edge>> portLocationPairsToEdges) {
      List<Edge> list;
      var pp = new PointPair(eg.SourcePort.Location, eg.TargetPort.Location);
      if (!portLocationPairsToEdges.TryGetValue(pp, out list))
        portLocationPairsToEdges[pp] = list = new List<Edge>();
      list.Add(eg);
    }

    static bool IsEdgeToParent(EdgeGeometry e) {
      return e.SourcePort is HookUpAnywhereFromInsidePort || e.TargetPort is HookUpAnywhereFromInsidePort;
    }

    InteractiveEdgeRouter CreateInteractiveEdgeRouter(IEnumerable<Shape> obstacleShapes) {
      //we need to create a set here because one loose polyline can hold several original shapes
      var loosePolys = new Set<Polyline>(obstacleShapes.Select(sh => shapesToTightLooseCouples[sh].LooseShape.BoundaryCurve as Polyline));
            var router = new InteractiveEdgeRouter {
                VisibilityGraph = visGraph,
                TightHierarchy =
                    CreateTightObstacleHierarachy(obstacleShapes),
                LooseHierarchy =
                    CreateLooseObstacleHierarachy(loosePolys),
                UseSpanner = true,
                LookForRoundedVertices = true,
                TightPadding = tightPadding,
                LoosePadding = LoosePadding,
                UseEdgeLengthMultiplier = UseEdgeLengthMultiplier,
                UsePolylineEndShortcutting = UsePolylineEndShortcutting,
                UseInnerPolylingShortcutting = UseInnerPolylingShortcutting,
                AllowedShootingStraightLines = AllowedShootingStraightLines,
            };

      router.AddActivePolygons(loosePolys.Select(polyline => new Polygon(polyline)));
      return router;
    }
    /// <summary>
    /// 
    Set<Shape> GetObstaclesFromPassport(Set<Shape> passport) {
      if (passport.Count == 0)
        return new Set<Shape>(root.Children);
      var commonAncestors = GetCommonAncestorsAbovePassport(passport);
      var allAncestors = GetAllAncestors(passport);
      var ret = new Set<Shape>(passport.SelectMany(p => p.Children.Where(child => !allAncestors.Contains(child))));
      var enqueued = new Set<Shape>(passport.Concat(ret));
      var queue = new Queue<Shape>();
      foreach (var shape in passport.Where(shape => !commonAncestors.Contains(shape)))
        queue.Enqueue(shape);

      while (queue.Count > 0) {
        var a = queue.Dequeue();
        foreach (var parent in a.Parents) {
          foreach (var sibling in parent.Children)
            if (!allAncestors.Contains(sibling))
              ret.Insert(sibling);

          if (!commonAncestors.Contains(parent) && !enqueued.Contains(parent)) {
            queue.Enqueue(parent);
            enqueued.Insert(parent);
          }
        }
      }
      return ret;
    }

    Set<Shape> GetAllAncestors(Set<Shape> passport) {
      if (!passport.Any()) return new Set<Shape>();
      var ret = new Set<Shape>(passport);
      foreach (var shape in passport)
        ret += ancestorSets[shape];
      return ret;
    }

    Set<Shape> GetCommonAncestorsAbovePassport(Set<Shape> passport) {
      if (!passport.Any()) return new Set<Shape>();
      var ret = ancestorSets[passport.First()];
      foreach (var shape in passport.Skip(1))
        ret *= ancestorSets[shape];
      return ret;
    }

    void RouteBundles() {
      ScaleLooseShapesDown();

      CalculateEdgeEnterablePolylines();
      var looseHierarchy = GetLooseHierarchy();
      var cdt = BundleRouter.CreateConstrainedDelaunayTriangulation(looseHierarchy);
      // CdtSweeper.ShowFront(cdt.GetTriangles(), null, null,this.visGraph.Edges.Select(e=>new LineSegment(e.SourcePoint,e.TargetPoint)));

      var shortestPath = new SdShortestPath(MakeTransparentShapesOfEdgeGeometryAndGetTheShapes, cdt, FindCdtGates(cdt));
      var bundleRouter = new BundleRouter(geometryGraph, shortestPath, visGraph, BundlingSettings,
                                                   LoosePadding, GetTightHierarchy(),
                                                   looseHierarchy, enterableLoose, enterableTight,
                                                   port => LoosePolyOfOriginalShape(portsToShapes[port]));

      bundleRouter.Run();
    }

    void CreateTheMapToParentLooseShapes(Shape shape, Dictionary<ICurve, Shape> loosePolylinesToLooseParentShapeMap) {
      foreach (var childShape in shape.Children) {
        var tightLooseCouple = shapesToTightLooseCouples[childShape];
        var poly = tightLooseCouple.LooseShape.BoundaryCurve;
        loosePolylinesToLooseParentShapeMap[poly] = shape;
        CreateTheMapToParentLooseShapes(childShape, loosePolylinesToLooseParentShapeMap);
      }
    }

    Set<CdtEdge> FindCdtGates(Cdt cdt) {
      Dictionary<ICurve, Shape> loosePolylinesToLooseParentShapeMap = new Dictionary<ICurve, Shape>();

      CreateTheMapToParentLooseShapes(root, loosePolylinesToLooseParentShapeMap);
      //looking for Cdt edges connecting two siblings; only those we define as gates
      var gates = new Set<CdtEdge>();
      foreach (var cdtSite in cdt.PointsToSites.Values) {
        foreach (var cdtEdge in cdtSite.Edges) {

          if (cdtEdge.CwTriangle == null && cdtEdge.CcwTriangle == null)
            continue;

          var a = (Polyline)cdtSite.Owner;
          var b = (Polyline)cdtEdge.lowerSite.Owner;
          if (a == b) continue;
          Shape aParent;
          Shape bParent;
          if (loosePolylinesToLooseParentShapeMap.TryGetValue(a, out aParent)
              && loosePolylinesToLooseParentShapeMap.TryGetValue(b, out bParent) && aParent == bParent)
            gates.Insert(cdtEdge);
        }
      }
      //CdtSweeper.ShowFront(cdt.GetTriangles(), null,
      //                    gates.Select(g => new LineSegment(g.upperSite.Point, g.lowerSite.Point)), null);
      return gates;
    }


    void CalculateEdgeEnterablePolylines() {
      enterableLoose = new Dictionary<EdgeGeometry, Set<Polyline>>();
      enterableTight = new Dictionary<EdgeGeometry, Set<Polyline>>();
      foreach (var edgeGeometry in edgeGeometriesEnumeration) {
        Set<Polyline> looseSet;
        Set<Polyline> tightSet;
        GetEdgeEnterablePolylines(edgeGeometry, out looseSet, out tightSet);
        enterableLoose[edgeGeometry] = looseSet;
        enterableTight[edgeGeometry] = tightSet;
      }
    }

    void GetEdgeEnterablePolylines(EdgeGeometry edgeGeometry, out Set<Polyline> looseEnterable,
        out Set<Polyline> tightEnterable) {
      looseEnterable = new Set<Polyline>();
      tightEnterable = new Set<Polyline>();
      var sourceShape = portsToShapes[edgeGeometry.SourcePort];
      var targetShape = portsToShapes[edgeGeometry.TargetPort];

      if (sourceShape != root) {
        looseEnterable.InsertRange(ancestorSets[sourceShape].Select(LoosePolyOfOriginalShape).Where(p => p != null));
        tightEnterable.InsertRange(ancestorSets[sourceShape].Select(TightPolyOfOriginalShape).Where(p => p != null));
      }

      if (targetShape != root) {
        looseEnterable.InsertRange(ancestorSets[targetShape].Select(LoosePolyOfOriginalShape).Where(p => p != null));
        tightEnterable.InsertRange(ancestorSets[targetShape].Select(TightPolyOfOriginalShape).Where(p => p != null));
      }
    }

    RectangleNode<Polyline, Point> GetTightHierarchy() {
      return RectangleNode<Polyline, Point>.CreateRectangleNodeOnEnumeration(shapesToTightLooseCouples.Values.
          Select(tl => new RectangleNode<Polyline, Point>(tl.TightPolyline, tl.TightPolyline.BoundingBox)));
    }

    RectangleNode<Polyline, Point> GetLooseHierarchy() {
      var loosePolylines = new Set<Polyline>(shapesToTightLooseCouples.Values.Select(tl => (Polyline)(tl.LooseShape.BoundaryCurve)));
      return RectangleNode<Polyline, Point>.CreateRectangleNodeOnEnumeration(loosePolylines.Select(p => new RectangleNode<Polyline, Point>(p, p.BoundingBox)));
    }

    void ScaleLooseShapesDown() {
      foreach (var shapesToTightLooseCouple in shapesToTightLooseCouples) {
        var tl = shapesToTightLooseCouple.Value;
        tl.LooseShape.BoundaryCurve = InteractiveObstacleCalculator.LoosePolylineWithFewCorners(tl.TightPolyline, tl.Distance / BundleRouter.SuperLoosePaddingCoefficient);
      }
    }

    /// <summary>
    ///  The set of shapes where the edgeGeometry source and target ports shapes are citizens.
    ///  In the simple case it is the union of the target port shape parents and the sourceport shape parents.
    ///  When one end shape contains another, the passport is the set consisting of the end shape and all other shape parents.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    Set<Shape> EdgePassport(Edge edge) {
      EdgeGeometry edgeGeometry = edge.EdgeGeometry;
      var ret = new Set<Shape>();
      var sourceShape = portsToShapes[edgeGeometry.SourcePort];
      var targetShape = portsToShapes[edgeGeometry.TargetPort];

      if (IsAncestor(sourceShape, targetShape)) {
        ret.InsertRange(targetShape.Parents);
        ret.Insert(sourceShape);
        return ret;
      }
      if (IsAncestor(targetShape, sourceShape)) {
        ret.InsertRange(sourceShape.Parents);
        ret.Insert(targetShape);
        return ret;
      }

      if (sourceShape != looseRoot)
        ret.InsertRange(sourceShape.Parents);

      if (targetShape != looseRoot)
        ret.InsertRange(targetShape.Parents);

      return ret;
    }

    IEnumerable<Port> AllPorts() {
      foreach (var edgeGeometry in edgeGeometriesEnumeration) {
        yield return edgeGeometry.SourcePort;
        yield return edgeGeometry.TargetPort;
      }
    }

    void CalculatePortsToShapes() {
      portsToShapes = new Dictionary<Port, Shape>();
      foreach (var shape in root.Descendants)
        foreach (var port in shape.Ports)
          portsToShapes[port] = shape;
      //assign all orphan ports to the root 
      foreach (var port in AllPorts().Where(p => !portsToShapes.ContainsKey(p))) {
        root.Ports.Insert(port);
        portsToShapes[port] = root;
      }
    }



    void RouteEdgeGeometry(Edge edge, InteractiveEdgeRouter iRouter) {
      var edgeGeometry = edge.EdgeGeometry;

      var addedEdges = new List<VisibilityEdge>();
      if (!(edgeGeometry.SourcePort is HookUpAnywhereFromInsidePort))
        addedEdges.AddRange(AddVisibilityEdgesFromPort(edgeGeometry.SourcePort));
      if (!(edgeGeometry.TargetPort is HookUpAnywhereFromInsidePort))
        addedEdges.AddRange(AddVisibilityEdgesFromPort(edgeGeometry.TargetPort));
      SmoothedPolyline smoothedPolyline;
      if (!ApproximateComparer.Close(edgeGeometry.SourcePort.Location, edgeGeometry.TargetPort.Location))
        edgeGeometry.Curve = iRouter.RouteSplineFromPortToPortWhenTheWholeGraphIsReady(
        edgeGeometry.SourcePort, edgeGeometry.TargetPort, true, out smoothedPolyline);
      else {
        edgeGeometry.Curve = Edge.RouteSelfEdge(edgeGeometry.SourcePort.Curve, Math.Max(LoosePadding * 2, edgeGeometry.GetMaxArrowheadLength()), out smoothedPolyline);
      }
      edgeGeometry.SmoothedPolyline = smoothedPolyline;

      if (edgeGeometry.Curve == null)
        throw new NotImplementedException();


      foreach (var visibilityEdge in addedEdges)
        VisibilityGraph.RemoveEdge(visibilityEdge);

      Arrowheads.TrimSplineAndCalculateArrowheads(edgeGeometry, edgeGeometry.SourcePort.Curve,
                                                  edgeGeometry.TargetPort.Curve, edgeGeometry.Curve,
                                                  false);
      if (ReplaceEdgeByRails != null)
        ReplaceEdgeByRails(edge);
      //  SetTransparency(transparentShapes, false);
    }
    /// <summary>
    /// if set to true the original spline is kept under the corresponding EdgeGeometry
    /// </summary>
    public bool KeepOriginalSpline { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public double ArrowHeadRatio { get; set; }

    internal Point[] LineSweeperPorts { get; set; }

    /// <summary>
    /// 
    /// </summary>


    IEnumerable<VisibilityEdge> AddVisibilityEdgesFromPort(Port port) {

      Shape portShape;
      TightLooseCouple boundaryCouple;
      if (port is CurvePort || !portsToShapes.TryGetValue(port, out portShape) ||
          !shapesToTightLooseCouples.TryGetValue(portShape, out boundaryCouple))
        return new VisibilityEdge[] { };

      var portLoosePoly = boundaryCouple.LooseShape;
      return (from point in portLoosePoly.BoundaryCurve as Polyline
              where visGraph.FindEdge(port.Location, point) == null
              select visGraph.AddEdge(port.Location, point));
    }
    List<Shape> MakeTransparentShapesOfEdgeGeometryAndGetTheShapes(EdgeGeometry edgeGeometry) {
      //it is OK here to repeat a shape in the returned list
      Shape sourceShape = portsToShapes[edgeGeometry.SourcePort];
      Shape targetShape = portsToShapes[edgeGeometry.TargetPort];

      var transparentLooseShapes = new List<Shape>();

      foreach (var shape in GetTransparentShapes(edgeGeometry.SourcePort, edgeGeometry.TargetPort, sourceShape, targetShape))
        if (shape != null)
          transparentLooseShapes.Add(LooseShapeOfOriginalShape(shape));
      foreach (var shape in portsToEnterableShapes[edgeGeometry.SourcePort])
        transparentLooseShapes.Add(LooseShapeOfOriginalShape(shape));
      foreach (var shape in portsToEnterableShapes[edgeGeometry.TargetPort])
        transparentLooseShapes.Add(LooseShapeOfOriginalShape(shape));

      SetTransparency(transparentLooseShapes, true);
      return transparentLooseShapes;
    }

    Shape LooseShapeOfOriginalShape(Shape s) {
      if (s == root)
        return looseRoot;
      return shapesToTightLooseCouples[s].LooseShape;
    }

    Polyline LoosePolyOfOriginalShape(Shape s) {
      return (Polyline)(LooseShapeOfOriginalShape(s).BoundaryCurve);
    }
    Polyline TightPolyOfOriginalShape(Shape s) {
      if (s == root)
        return null;
      return shapesToTightLooseCouples[s].TightPolyline;
    }
    #region debugging

#if TEST_MSAGL
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    static internal void AnotherShowMethod(VisibilityGraph visGraph,
        Port sourcePort, Port targetPort, IEnumerable<Shape> obstacleShapes, ICurve curve) {
      var dd = new List<DebugCurve>(
   visGraph.Edges.Select(
      e =>
      new DebugCurve(100, 0.1, GetEdgeColor(e, sourcePort, targetPort),
                     new LineSegment(e.SourcePoint, e.TargetPoint))));
      if (obstacleShapes != null)
        dd.AddRange(
            obstacleShapes.Select(s => new DebugCurve(1, s.BoundaryCurve)));
      if (sourcePort != null && targetPort != null)
        dd.AddRange(new[] {
                                    new DebugCurve(CurveFactory.CreateDiamond(3, 3, sourcePort.Location)),
                                    new DebugCurve(CurveFactory.CreateEllipse(3, 3, targetPort.Location)),
                                });
      if (curve != null)
        dd.Add(new DebugCurve(5, "purple", curve));

      LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(dd);
    }


    static string GetEdgeColor(VisibilityEdge e, Port sourcePort, Port targetPort) {
      if (sourcePort == null || targetPort == null)
        return "green";
      if (ApproximateComparer.Close(e.SourcePoint, sourcePort.Location) ||
          ApproximateComparer.Close(e.SourcePoint, targetPort.Location)
          || ApproximateComparer.Close(e.TargetPoint, sourcePort.Location) ||
          ApproximateComparer.Close(e.TargetPoint, targetPort.Location))
        return "lightgreen";
      return e.IsPassable == null || e.IsPassable() ? "green" : "red";
    }
#endif

    #endregion

    IEnumerable<Shape> GetTransparentShapes(
        Port sourcePort,
        Port targetPort, Shape sourceShape, Shape targetShape) {
      foreach (var s in ancestorSets[sourceShape])
        yield return s;
      foreach (var s in ancestorSets[targetShape])
        yield return s;
      var routingOutsideOfSourceBoundary = EdgesAttachedToPortAvoidTheNode(sourcePort);
      var routingOutsideOfTargetBoundary = EdgesAttachedToPortAvoidTheNode(targetPort);
      if (!routingOutsideOfSourceBoundary && !routingOutsideOfTargetBoundary) {
        yield return sourceShape;
        yield return targetShape;
      }
      else if (routingOutsideOfSourceBoundary) {
        if (IsAncestor(sourceShape, targetShape))
          yield return sourceShape;
      }
      else {
        if (IsAncestor(targetShape, sourceShape))
          yield return targetShape;
      }
    }

    static void SetTransparency(IEnumerable<Shape> shapes, bool v) {
      foreach (Shape shape in shapes)
        shape.IsTransparent = v;
    }


    /*
    /// <summary>
    /// it is the set of parents with all children being obstacle candidates
    /// </summary>
    /// <param name="port"></param>
    /// <param name="shape"></param>
    /// <param name="otherShape"></param>
    /// <returns></returns>
    IEnumerable<Shape> FindMinimalParents(Port port, Shape shape, Shape otherShape) {
        if (shape == null)
            yield return rootOfLooseShapes;
        else if (port is CurvePort) {
            if (IsLgAncestor(shape, otherShape))
                yield return shape;
            else
                foreach (Shape parent in shape.Parents)
                    yield return parent;
        } else if (PortIsInsideOfShape(port, shape) && shape.Children.Any())
            yield return shape;
        else
            foreach (Shape parent in shape.Parents)
                yield return parent;
    }
*/

    bool IsAncestor(Shape possibleAncestor, Shape possiblePredecessor) {
      Set<Shape> ancestors;
      return possiblePredecessor != null &&
             ancestorSets.TryGetValue(possiblePredecessor, out ancestors) && ancestors != null &&
             ancestors.Contains(possibleAncestor);
    }

    /*
            static bool PortIsInsideOfShape(Port port, Shape shape) {
                if (shape == null)
                    return false;
                return Curve.PointRelativeToCurveLocation(ApproximateComparer.Round(port.Location), shape.BoundaryCurve) ==
                       PointLocation.Inside;
            }
    */

    static RectangleNode<Polyline, Point> CreateLooseObstacleHierarachy(IEnumerable<Polyline> loosePolys) {
      return
          RectangleNode<Polyline, Point>.CreateRectangleNodeOnEnumeration(
              loosePolys.Select(
                  poly => new RectangleNode<Polyline, Point>(poly, poly.BoundingBox)));
    }

    RectangleNode<Polyline, Point> CreateTightObstacleHierarachy(IEnumerable<Shape> obstacles) {
      var tightPolys = obstacles.Select(sh => shapesToTightLooseCouples[sh].TightPolyline);
      return
          RectangleNode<Polyline, Point>.CreateRectangleNodeOnEnumeration(
              tightPolys.Select(tightPoly => new RectangleNode<Polyline, Point>(
                                                 tightPoly,
                                                 tightPoly.BoundingBox)));
    }

    void CalculateVisibilityGraph() {
      var setOfPortLocations = LineSweeperPorts != null ? new Set<Point>(LineSweeperPorts) : new Set<Point>();
      ProcessHookAnyWherePorts(setOfPortLocations);
      portRTree =
          new RTree<Point,Point>(
              setOfPortLocations.Select(p => new KeyValuePair<IRectangle<Point>, Point>(new Rectangle(p), p)));
      visGraph = new VisibilityGraph();

      FillVisibilityGraphUnderShape(root);
                  //ShowVisGraph(visGraph, new Set<Polyline>(shapesToTightLooseCouples.Values.Select(tl => (Polyline)(tl.LooseShape.BoundaryCurve))),
                    //  geometryGraph.Nodes.Select(n => n.BoundaryCurve).Concat(root.Descendants.Select(d => d.BoundaryCurve)), null);
    }

    private void ProcessHookAnyWherePorts(Set<Point> setOfPortLocations) {
      foreach (var edgeGeometry in edgeGeometriesEnumeration) {
        if (!(edgeGeometry.SourcePort is HookUpAnywhereFromInsidePort || edgeGeometry.SourcePort is ClusterBoundaryPort))
          setOfPortLocations.Insert(edgeGeometry.SourcePort.Location);
        if (!(edgeGeometry.TargetPort is HookUpAnywhereFromInsidePort || edgeGeometry.TargetPort is ClusterBoundaryPort))
          setOfPortLocations.Insert(edgeGeometry.TargetPort.Location);
      }
    }

    /// <summary>
    /// this function might change the shape's loose polylines by inserting new points
    /// </summary>
    void FillVisibilityGraphUnderShape(Shape shape) {
      //going depth first 
      var children = shape.Children;
      foreach (Shape child in children)
        FillVisibilityGraphUnderShape(child);
      TightLooseCouple tightLooseCouple;
      Polyline looseBoundary = shapesToTightLooseCouples.TryGetValue(shape, out tightLooseCouple) ? tightLooseCouple.LooseShape.BoundaryCurve as Polyline : null;
      Shape looseShape = tightLooseCouple != null ? tightLooseCouple.LooseShape : looseRoot;
      var obstacles = new Set<Polyline>(looseShape.Children.Select(c => c.BoundaryCurve as Polyline));

      var portLocations = RemoveInsidePortsAndSplitBoundaryIfNeeded(looseBoundary);
      //this run will split the polyline enough to route later from the inner ports
      var tmpVisGraph = new VisibilityGraph();
      var coneSpanner = new ConeSpanner(new Polyline[] { }, tmpVisGraph, coneAngle, portLocations, looseBoundary);
      coneSpanner.Run();
      //now run the spanner again to create the correct visibility graph around the inner obstacles
      tmpVisGraph = new VisibilityGraph();
      coneSpanner = new ConeSpanner(obstacles, tmpVisGraph, coneAngle, portLocations, looseBoundary) {
        Bidirectional = Bidirectional && obstacles.Count > 0
      };
      coneSpanner.Run();

      ProgressStep();

      foreach (VisibilityEdge edge in tmpVisGraph.Edges)
        TryToCreateNewEdgeAndSetIsPassable(edge, looseShape);

      AddBoundaryEdgesToVisGraph(looseBoundary);
      //            if (obstacles.Count > 0)
      //                SplineRouter.ShowVisGraph(tmpVisGraph, obstacles, null, null);
    }

    /// <summary>
    /// If set to true then a smaller visibility graph is created.
    /// An edge is added to the visibility graph only if it is found at least twice: 
    /// once sweeping with a direction d and the second time with -d
    /// </summary>
    internal bool Bidirectional { get; set; }

#if TEST_MSAGL
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static internal void ShowVisGraph(VisibilityGraph tmpVisGraph, IEnumerable<Polyline> obstacles, IEnumerable<ICurve> greenCurves = null, IEnumerable<ICurve> redCurves = null) {
          var l = new List<DebugCurve>(tmpVisGraph.Edges.Select(e => new DebugCurve(100, 1,
              e.IsPassable != null && e.IsPassable() ? "green" : "black"
              , new LineSegment(e.SourcePoint, e.TargetPoint))));
          if (obstacles != null)
            l.AddRange(obstacles.Select(p => new DebugCurve(100, 1, "brown", p)));
          if (greenCurves != null)
            l.AddRange(greenCurves.Select(p => new DebugCurve(100, 10, "navy", p)));
          if (redCurves != null)
            l.AddRange(redCurves.Select(p => new DebugCurve(100, 10, "red", p)));
          LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        }
#endif
    void TryToCreateNewEdgeAndSetIsPassable(VisibilityEdge edge, Shape looseShape) {
      var e = visGraph.FindEdge(edge.SourcePoint, edge.TargetPoint);
      if (e != null) return;
      e = visGraph.AddEdge(edge.SourcePoint, edge.TargetPoint);
      if (looseShape != null)
        e.IsPassable = () => looseShape.IsTransparent;
    }


    void AddBoundaryEdgesToVisGraph(Polyline boundary) {
      if (boundary == null) return;
      var p = boundary.StartPoint;
      do {
        var pn = p.NextOnPolyline;
        visGraph.AddEdge(p.Point, pn.Point);
        if (pn == boundary.StartPoint)
          break;
        p = pn;
      } while (true);
    }

    Set<Point> RemoveInsidePortsAndSplitBoundaryIfNeeded(Polyline boundary) {
      var ret = new Set<Point>();

      if (boundary == null) {
        foreach (var point in portRTree.GetAllLeaves())
          ret.Insert(point);
        portRTree.Clear();
        return ret;
      }
      Rectangle boundaryBox = boundary.BoundingBox;
      var portLocationsInQuestion = portRTree.GetAllIntersecting(boundaryBox).ToArray();
      foreach (var point in portLocationsInQuestion) {
        switch (Curve.PointRelativeToCurveLocation(point, boundary)) {
          case PointLocation.Inside:
            ret.Insert(point);
            portLocationsToLoosePolylines[point] = boundary;
            portRTree.Remove(new Rectangle(point), point);
            break;
          case PointLocation.Boundary:
            portRTree.Remove(new Rectangle(point), point);
            portLocationsToLoosePolylines[point] = boundary;
            PolylinePoint polylinePoint = FindPointOnPolylineToInsertAfter(boundary, point);
            if (polylinePoint != null)
              LineSweeper.InsertPointIntoPolylineAfter(boundary, polylinePoint, point);
            else
              throw new InvalidOperationException();
            break;
        }
      }
      return ret;
    }

    static PolylinePoint FindPointOnPolylineToInsertAfter(Polyline boundary, Point point) {
      for (PolylinePoint p = boundary.StartPoint; ;) {
        PolylinePoint pn = p.NextOnPolyline;

        if (ApproximateComparer.Close(point, p.Point) || ApproximateComparer.Close(point, pn.Point))
          return null; //the point is already inside
        double par;
        if (ApproximateComparer.Close(Point.DistToLineSegment(point, p.Point, pn.Point, out par), 0))
          return p;
        p = pn;
        if (p == boundary.StartPoint)
          throw new InvalidOperationException();
      }
    }


    /// <summary>
    /// creates a root; a shape with BoundaryCurve set to null 
    /// </summary>
    void GetOrCreateRoot() {
      if (rootShapes.Count() == 0) return;

      if (rootShapes.Count() == 1) {
        Shape r = rootShapes.First();
        if (r.BoundaryCurve == null) {
          root = r;
          return;
        }
      }
      rootWasCreated = true;
      root = new Shape();
#if TEST_MSAGL
      root.UserData = "root";
#endif
      foreach (var rootShape in rootShapes)
        root.AddChild(rootShape);
    }

    void RemoveRoot() {
      if (rootWasCreated)
        foreach (var rootShape in rootShapes)
          rootShape.RemoveParent(root);
    }


#if TEST_MSAGL
    // ReSharper disable UnusedMember.Local
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    static void Show(
        IEnumerable<EdgeGeometry> edgeGeometries, IEnumerable<Shape> listOfShapes) {
      // ReSharper restore UnusedMember.Local
      var r = new Random(1);
      LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(
          listOfShapes.Select(s => s.BoundaryCurve).Select(
              c => new DebugCurve(50, 1, DebugCurve.Colors[r.Next(DebugCurve.Colors.Length - 1)], c)).Concat(
                  edgeGeometries.Select(e => new DebugCurve(100, 1, "red", e.Curve))));
    }
#endif


    internal static Dictionary<Shape, Set<Shape>> GetAncestorSetsMap(IEnumerable<Shape> shapes) {
      var ancSets = new Dictionary<Shape, Set<Shape>>();
      foreach (var child in shapes.Where(child => !ancSets.ContainsKey(child)))
        ancSets[child] = GetAncestorSet(child, ancSets);
      return ancSets;
    }

    static Set<Shape> GetAncestorSet(Shape child, Dictionary<Shape, Set<Shape>> ancSets) {
      var ret = new Set<Shape>(child.Parents);
      foreach (var parent in child.Parents) {
        Set<Shape> grandParents;
        ret += ancSets.TryGetValue(parent, out grandParents)
                   ? grandParents
                   : ancSets[parent] = GetAncestorSet(parent, ancSets);
      }
      return ret;
    }

    static internal void CreatePortsIfNeeded(IEnumerable<Edge> edges) {
      foreach (var edge in edges) {
        if (edge.SourcePort == null) {
          var e = edge;
#if SHARPKIT // Lambdas bind differently in JS
                    edge.SourcePort = ((Func<Edge,RelativeFloatingPort>)(ed => new RelativeFloatingPort(() => ed.Source.BoundaryCurve,
                        () => ed.Source.Center)))(e);
#else
          edge.SourcePort = new RelativeFloatingPort(() => e.Source.BoundaryCurve, () => e.Source.Center);
#endif
        }
        if (edge.TargetPort == null) {
          var e = edge;
#if SHARPKIT // Lambdas bind differently in JS
                    edge.TargetPort = ((Func<Edge, RelativeFloatingPort>)(ed => new RelativeFloatingPort(() => ed.Target.BoundaryCurve,
                        () => ed.Target.Center)))(e);
#else
          edge.TargetPort = new RelativeFloatingPort(() => e.Target.BoundaryCurve, () => e.Target.Center);
#endif
        }
      }
    }

    /// <summary>
    ///  computes loosePadding for spline routing obstacles from node separation and EdgePadding.
    /// </summary>
    /// <param name="nodeSeparation"></param>
    /// <param name="edgePadding"></param>
    /// <returns></returns>
    static public double ComputeLooseSplinePadding(double nodeSeparation, double edgePadding) {
      Debug.Assert(edgePadding > 0, "require EdgePadding > 0");
      double twicePadding = 2.0 * edgePadding;
      Debug.Assert(nodeSeparation > twicePadding, "require OverlapSeparation > 2*EdgePadding");

      // the 8 divisor is just to guarantee the final postcondition
      double loosePadding = (nodeSeparation - twicePadding) / 8;
      Debug.Assert(loosePadding > 0, "require LoosePadding > 0");
      Debug.Assert(twicePadding + (2 * loosePadding) < nodeSeparation, "EdgePadding too big!");
      return loosePadding;
    }


  }
}
