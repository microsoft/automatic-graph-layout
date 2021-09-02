using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.ProjectionSolver;
using Microsoft.Msagl.Routing;

namespace Microsoft.Msagl.Layout.Layered {
    internal class TwoLayerFlatEdgeRouter : AlgorithmBase {
        readonly int[] bottomLayer;
        InteractiveEdgeRouter interactiveEdgeRouter;
        double[] labelCenters;
        Dictionary<Label, ICurve> labelsToLabelObstacles = new Dictionary<Label, ICurve>();
        IntPair[] pairArray;
        readonly Routing routing;
        readonly int[] topLayer;
        private SugiyamaLayoutSettings settings;

        internal TwoLayerFlatEdgeRouter(SugiyamaLayoutSettings settings, Routing routing, int[] bottomLayer, int[] topLayer)
        {
            this.settings = settings;
            this.topLayer = topLayer;
            this.bottomLayer = bottomLayer;
            this.routing = routing;
            InitLabelsInfo();
        }

        Database Database {
            get { return routing.Database; }
        }

        int[] Layering {
            get { return routing.LayerArrays.Y; }
        }

        BasicGraphOnEdges<PolyIntEdge> IntGraph {
            get { return routing.IntGraph; }
        }

        LayerArrays LayerArrays {
            get { return routing.LayerArrays; }
        }


        double PaddingForEdges {
            get { return settings.LayerSeparation / 8; }
        }

        void InitLabelsInfo() {
            pairArray = new Set<IntPair>(from v in bottomLayer
                                         where v < IntGraph.NodeCount
                                         from edge in IntGraph.OutEdges(v)
                                         where edge.Source != edge.Target
                                         where Layering[edge.Target] == Layering[edge.Source]
                                         select new IntPair(edge.Source, edge.Target)).ToArray();
            labelCenters = new double[pairArray.Length];
            int i = 0;
            foreach (IntPair p in pairArray) {
                int leftNode, rightNode;
                if (LayerArrays.X[p.First] < LayerArrays.X[p.Second]) {
                    leftNode = p.First;
                    rightNode = p.Second;
                } else {
                    leftNode = p.Second;
                    rightNode = p.First;
                }
                labelCenters[i++] = (Database.Anchors[leftNode].Right + Database.Anchors[rightNode].Left)/2;
                //labelCenters contains ideal position for nodes at the moment
            }
            InitLabelsToLabelObstacles();
        }

        void InitLabelsToLabelObstacles() {
            labelsToLabelObstacles = new Dictionary<Label, ICurve>();
            IEnumerable<Label> labels = from p in pairArray from label in PairLabels(p) select label;
            foreach (Label label in labels)
                labelsToLabelObstacles[label] = CreatObstaceOnLabel(label);
        }

        double GetMaxLabelWidth(IntPair intPair) {
            IEnumerable<Label> multiEdgeLabels = PairLabels(intPair);

            if (multiEdgeLabels.Any())
                return multiEdgeLabels.Max(label => label.Width);
            return 0;
        }

        IEnumerable<Label> PairLabels(IntPair intPair) {
            return from edge in Database.GetMultiedge(intPair)
                   let label = edge.Edge.Label
                   where label != null
                   select label;
        }

        /// <summary>
        /// Executes the algorithm.
        /// </summary>
        protected override void RunInternal() {
            if (pairArray.Length > 0) {
                PositionLabelsOfFlatEdges();
                interactiveEdgeRouter = new InteractiveEdgeRouter(GetObstacles(), PaddingForEdges, PaddingForEdges/3, Math.PI/6);
                interactiveEdgeRouter.CalculateWholeTangentVisibilityGraph();
                foreach (PolyIntEdge intEdge in IntEdges())
                {
                    this.ProgressStep();
                    RouteEdge(intEdge);
                }
            }
        }

        IEnumerable<ICurve> GetObstacles() {
            return (from v in topLayer.Concat(bottomLayer)
                    where v < routing.OriginalGraph.Nodes.Count
                    select routing.IntGraph.Nodes[v].BoundaryCurve).Concat(LabelCurves());
        }

        IEnumerable<ICurve> LabelCurves() {
            return from edge in IntEdges()
                   let label = edge.Edge.Label
                   where label != null
                   select CreatObstaceOnLabel(label);
        }

        static ICurve CreatObstaceOnLabel(Label label) {
            var c = new Curve();
            double obstacleBottom = label.Center.Y - label.Height/4;
            c.AddSegment(new LineSegment(new Point(label.BoundingBox.Left, obstacleBottom),
                                         new Point(label.BoundingBox.Right, obstacleBottom)));
            Curve.ContinueWithLineSegment(c, label.BoundingBox.RightTop);
            Curve.ContinueWithLineSegment(c, label.BoundingBox.LeftTop);
            Curve.CloseCurve(c);
            return c;
        }

        IEnumerable<PolyIntEdge> IntEdges() {
            return from pair in pairArray from edge in Database.GetMultiedge(pair) select edge;
        }

        void RouteEdge(PolyIntEdge edge) {
            if (edge.HasLabel)
                RouteEdgeWithLabel(edge, edge.Edge.Label);
            else
                RouteEdgeWithNoLabel(edge);
        }

        void RouteEdgeWithLabel(PolyIntEdge intEdge, Label label) {
            //we allow here for the edge to cross its own label
            Node sourceNode = routing.IntGraph.Nodes[intEdge.Source];
            Node targetNode = routing.IntGraph.Nodes[intEdge.Target];
            var sourcePort = new FloatingPort(sourceNode.BoundaryCurve, sourceNode.Center);
            var targetPort = new FloatingPort(targetNode.BoundaryCurve, targetNode.Center);
            ICurve labelObstacle = labelsToLabelObstacles[label];
            var labelPort = new FloatingPort(labelObstacle, label.Center);
            SmoothedPolyline poly0;
            interactiveEdgeRouter.RouteSplineFromPortToPortWhenTheWholeGraphIsReady(sourcePort, labelPort, true, out poly0);
            SmoothedPolyline poly1;
            interactiveEdgeRouter.RouteSplineFromPortToPortWhenTheWholeGraphIsReady(labelPort, targetPort, true, out poly1);
            Site site = poly1.HeadSite.Next;

            Site lastSite = poly0.LastSite;
            lastSite.Next = site;
            site.Previous = lastSite;
            var eg = intEdge.Edge.EdgeGeometry;
            eg.SetSmoothedPolylineAndCurve(poly0);
            Arrowheads.TrimSplineAndCalculateArrowheads(eg, intEdge.Edge.Source.BoundaryCurve,
                                                             intEdge.Edge.Target.BoundaryCurve, eg.Curve, false);
        }

        void RouteEdgeWithNoLabel(PolyIntEdge intEdge) {
            Node sourceNode = routing.IntGraph.Nodes[intEdge.Source];
            Node targetNode = routing.IntGraph.Nodes[intEdge.Target];
            var sourcePort = new FloatingPort(sourceNode.BoundaryCurve, sourceNode.Center);
            var targetPort = new FloatingPort(targetNode.BoundaryCurve, targetNode.Center);
            var eg = intEdge.Edge.EdgeGeometry;
            SmoothedPolyline sp;
            eg.Curve = interactiveEdgeRouter.RouteSplineFromPortToPortWhenTheWholeGraphIsReady(sourcePort, targetPort, true, out sp);
            Arrowheads.TrimSplineAndCalculateArrowheads(eg, intEdge.Edge.Source.BoundaryCurve,
                                                             intEdge.Edge.Target.BoundaryCurve, eg.Curve, false);
            intEdge.Edge.EdgeGeometry = eg;
        }

        void PositionLabelsOfFlatEdges() {
            if (labelCenters == null || labelCenters.Length == 0)
                return;
            SortLabelsByX();
            CalculateLabelsX();
        }

        void CalculateLabelsX() {
            int i;
            var solver = new SolverShell();
            for (i = 0; i < pairArray.Length; i++)
                solver.AddVariableWithIdealPosition(i, labelCenters[i], GetLabelWeight(pairArray[i]));

            //add non overlapping constraints between to neighbor labels
            double prevLabelWidth = GetMaxLabelWidth(pairArray[0]);
            for (i = 0; i < pairArray.Length - 1; i++)
                solver.AddLeftRightSeparationConstraint(i, i + 1,
                                                        (prevLabelWidth +
                                                         (prevLabelWidth = GetMaxLabelWidth(pairArray[i + 1])))/2 +
                                                        settings.NodeSeparation);

            for (i = 0; i < labelCenters.Length; i++) {
                double x = labelCenters[i] = solver.GetVariableResolvedPosition(i);
                foreach (Label label in PairLabels(pairArray[i]))
                    label.Center = new Point(x, label.Center.Y);
            }
        }

        double GetLabelWeight(IntPair intPair) {
            return Database.GetMultiedge(intPair).Count;
        }

        void SortLabelsByX() {
            Array.Sort(labelCenters, pairArray);
        }
    }
}