using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Controls;
using GeometryGraph = Microsoft.Msagl.Core.Layout.GeometryGraph;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using GeometryPoint = Microsoft.Msagl.Core.Geometry.Point;
using DrawingGraph = Microsoft.Msagl.Drawing.Graph;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Core.Routing;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /*
    * The basic idea of nested graphs is that a DNode gets a DLabel with a DGraph as its Content. There are two problems to solve: layout, and
    * layout. More accurately, there are two unrelated things called "layout" involved, and both are a problem.
    * 
    * The GRAPH layout is the one that's invoked with the DGraph.BeginLayout() method. We need to make sure that the layout of each inner node is
    * completed before we invoke BeginLayout on the outer graph. This can be done by setting event handlers on the inner graphs' GraphLayoutDone
    * events. If we don't do this, the outer graph won't know the size of its nodes and will be unable to produce a correct layout. In order to do
    * this, I'm using a Queue of graphs which need to be laid out (in the correct order). With some tweaks, we could layout multiple graphs at
    * the same time, provided that they are "cousins" and not descendants, but I doubt this would provide a noticeable performance improvement except
    * in extreme cases.
    * 
    * The XAML layout, on the other hand, is what the XAML framework does to render stuff to the screen. It involves calculating the size of
    * XAML elements and figuring out where they need to be placed. The details are hidden from us, and most of the time it works behind the scenes.
    * However, there is one key issue - layout is NOT done if an element is not in a visual tree (i.e. is not somewhere in the MainWindow). This
    * is a problem because the graph layout needs to clear its own visual tree before starting, which means that elements are going to end up
    * outside the visual tree, which in turn means that their size will be set to zero.
    * 
    * Fortunately, we can explicitly tell the XAML engine to figure out their size even if they are outside a visual tree; this is what the
    * "Measure" method down below does. We need to do this in the GraphLayoutStarting event, which happens after the elements have been
    * removed from the visual tree.
    */
    public class NestedGraphHelper
    {
        private DGraph m_Graph;

        private NestedGraphHelper(DGraph graph)
        {
            m_Graph = graph;
        }

        private Queue<DNestedGraphLabel> m_LayoutQueue;

        private void BeginLayout()
        {
            m_LayoutQueue = new Queue<DNestedGraphLabel>();

            SetupQueue(m_Graph);

            m_Graph.GraphLayoutDone += graph_GraphLayoutDone;

            BeginNextLayout();
        }

        private void PopulateAllCrossEdges(DGraph graph)
        {
            graph.PopulateCrossEdges();
            foreach (DGraph g in graph.NestedGraphs)
                PopulateAllCrossEdges(g);
        }

        void graph_GraphLayoutDone(object sender, EventArgs e)
        {
            m_Graph.GraphLayoutDone -= graph_GraphLayoutDone;
            try
            {
                PopulateAllCrossEdges(m_Graph);
            }
            catch (ArgumentException)
            {
                // DNestedGraphLabel.GetDGraphOffset has failed with an ArgumentException. This means that the graph has not been loaded into
                // the visual tree yet, which means that I can't reliably get the positions of subgraphs within the labels, which prevents me
                // from drawing cross-edges properly. As near as I can tell, there is no good reason for this; I can get the sizes of everything
                // without loading, so why does TransformToVisual require loading? Anyway, the only thing I can do is wait until the graph has
                // been loaded.
                m_Graph.Loaded += m_Graph_Loaded;
            }
        }

        void m_Graph_Loaded(object sender, RoutedEventArgs e)
        {
            m_Graph.Loaded -= m_Graph_Loaded;
            PopulateAllCrossEdges(m_Graph);
        }

        internal static void BeginLayout(DGraph graph)
        {
            NestedGraphHelper helper = new NestedGraphHelper(graph);
            helper.BeginLayout();
        }

        private void SetupQueue(DGraph g)
        {
            foreach (DNode n in g.Nodes())
            {
                DNestedGraphLabel l = n.Label as DNestedGraphLabel;
                if (l != null)
                {
                    foreach (DGraph graph in l.Graphs)
                    {
                        SetupQueue(graph);
                        //graph.PopulateChildren();
                    }
                    m_LayoutQueue.Enqueue(l);
                }
            }
        }

        private int count = 0;

        private void BeginNextLayout()
        {
            if (--count > 0)
                return;
            if (!m_LayoutQueue.Any())
            {
                m_Graph.Dispatcher.BeginInvoke((Action)(() => m_Graph.BeginLayout(true)));
                return;
            }
            DNestedGraphLabel l = m_LayoutQueue.Dequeue();
            count = 0;
            foreach (DGraph graph in l.Graphs)
            {
                count++;
                graph.GraphLayoutDone += SubGraphLayoutDone;
                graph.Dispatcher.BeginInvoke(() => graph.BeginLayout(true));
            }
        }

        public static T FindAncestorOrSelf<T>(DependencyObject obj)
            where T : DependencyObject
        {
            while (obj != null)
            {
                T objTest = obj as T;
                if (objTest != null)
                    return objTest;
                obj = VisualTreeHelper.GetParent(obj);
            }

            return null;
        }

        private void SubGraphLayoutDone(object sender, EventArgs e)
        {
            DGraph g = sender as DGraph;
            g.GraphLayoutDone -= SubGraphLayoutDone;
            g.Measure();
            g.Dispatcher.BeginInvoke((Action)(() => BeginNextLayout()));
        }

        private static void MeasureAllNestedGraphs(DGraph dg)
        {
            foreach (DNestedGraphLabel label in dg.Nodes().Cast<DNode>().Where(n => n.Label is DNestedGraphLabel).Select(n => n.Label as DNestedGraphLabel))
            {
                foreach (DGraph idg in label.Graphs)
                {
                    MeasureAllNestedGraphs(idg);
                    idg.Measure();
                }
                if (label.Content is FrameworkElement)
                    (label.Content as FrameworkElement).Measure();
            }
        }

        /// <summary>
        /// Creates an auxilliary geometry graph, which contains all the nodes of a flattened nested graph. Produces a map from the original graph nodes to the auxilliary graph nodes. 
        /// </summary>
        /// <param name="gg">The auxilliary graph to be filled.</param>
        /// <param name="graph">The original graph.</param>
        /// <param name="offset">Offset to be applied to all nodes.</param>
        /// <param name="connectedNodes">Map from original graph nodes to auxilliary graph nodes.</param>
        private static void FillAuxGraph(GeometryGraph gg, DGraph graph, GeometryPoint offset, Dictionary<DNode, GeometryNode> connectedNodes)
        {
            foreach (DNode n in graph.Nodes())
            {
                GeometryNode clone = new GeometryNode(n.GeometryNode.BoundaryCurve.Clone());
                clone.BoundaryCurve.Translate(offset);
                gg.Nodes.Add(clone);
                connectedNodes[n] = clone;
                DNestedGraphLabel ngLabel = n.Label as DNestedGraphLabel;
                if (ngLabel != null)
                {
                    GeometryPoint labelPosition = new GeometryPoint(Canvas.GetLeft(ngLabel), Canvas.GetTop(ngLabel));
                    foreach (DGraph dg in ngLabel.Graphs)
                    {
                        Point p = ngLabel.GetDGraphOffset(dg);
                        GeometryPoint offsetToLabel = new GeometryPoint(p.X, p.Y);
                        GeometryPoint graphOffset = labelPosition + offsetToLabel;
                        GeometryPoint normalizedOffset = graphOffset - dg.Graph.BoundingBox.LeftBottom;
                        FillAuxGraph(gg, dg, offset + normalizedOffset, connectedNodes);
                    }
                }
            }
        }

        public static void DrawCrossEdges(DGraph graph, IEnumerable<DEdge> edges)
        {
            GeometryGraph ggAux = new GeometryGraph();
            Dictionary<DNode, GeometryNode> nodeMap = new Dictionary<DNode, GeometryNode>();
            foreach (DEdge edge in edges)
            {
                if (edge.Label != null)
                    edge.Label.MeasureLabel();
                nodeMap[edge.Source] = null;
                nodeMap[edge.Target] = null;
            }
            FillAuxGraph(ggAux, graph, new GeometryPoint(0.0, 0.0), nodeMap);
            Dictionary<DEdge, GeometryEdge> edgeMap = new Dictionary<DEdge, GeometryEdge>();
            foreach (DEdge edge in edges)
            {
                GeometryEdge gEdge = new GeometryEdge(nodeMap[edge.Source], nodeMap[edge.Target]) { GeometryParent = ggAux };
                gEdge.EdgeGeometry.SourceArrowhead = edge.GeometryEdge.EdgeGeometry.SourceArrowhead;
                gEdge.EdgeGeometry.TargetArrowhead = edge.GeometryEdge.EdgeGeometry.TargetArrowhead;
                gEdge.Label = edge.GeometryEdge.Label;
                edgeMap[edge] = gEdge;
                ggAux.Edges.Add(gEdge);
            }
            var router = new SplineRouter(ggAux, 3.0, 2.0, Math.PI / 6.0) { ContinueOnOverlaps = true };
            router.Run();
            foreach (DEdge edge in edges)
            {
                edge.GeometryEdge = edgeMap[edge];
                if (edge.DrawingEdge.Label != null)
                {
                    if (edge.GeometryEdge.Label.Center == new GeometryPoint())
                        edge.GeometryEdge.Label.Center = edge.GeometryEdge.BoundingBox.Center;
                    edge.DrawingEdge.Label.GeometryLabel = edge.GeometryEdge.Label;
                }
            }
            foreach (DEdge edge in edges)
            {
                edge.MakeVisual();
                Canvas.SetZIndex(edge, 20000);
                graph.MainCanvas.Children.Add(edge);
                if (edge.Label != null)
                {
                    edge.Label.MakeVisual();
                    Canvas.SetZIndex(edge.Label, 20000);
                    graph.MainCanvas.Children.Add(edge.Label);
                }
            }
        }

        public static void DrawCrossEdge(DEdge edge)
        {
            DrawCrossEdges(edge.ParentGraph, new DEdge[] { edge });
        }
    }
}
