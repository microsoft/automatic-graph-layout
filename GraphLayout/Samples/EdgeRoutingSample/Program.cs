using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using Node = Microsoft.Msagl.Core.Layout.Node;

namespace EdgeRoutingSample {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
#if TEST
            DisplayGeometryGraph.SetShowFunctions();
#else 
            System.Diagnostics.Debug.WriteLine("run the Debug version to see the edge routes");
#endif
            var graph = GeometryGraphReader.CreateFromFile("channel.msagl.geom");
            foreach (var edge in graph.Edges) {
                if (edge.SourcePort == null)
                    edge.SourcePort = new FloatingPort(edge.Source.BoundaryCurve, edge.Source.Center);
                if (edge.TargetPort == null)
                    edge.TargetPort = new FloatingPort(edge.Target.BoundaryCurve, edge.Target.Center);
            }
         
            DemoEdgeRouterHelper(graph);
            
        }

        static void DemoEdgeRouterHelper(GeometryGraph graph) {

            DemoRoutingFromPortToPort(graph);

            var bundlingSettings = new BundlingSettings(); 
            // set bundling settings to null to disable the bundling of the edges

            var router = new SplineRouter(graph, 3, 3, Math.PI / 6, bundlingSettings);


            router.Run();
#if TEST
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif

            var rectRouter = new RectilinearEdgeRouter(graph, 3,3, true);
            rectRouter.Run();
#if TEST
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
            
           
        }

        static void DemoRoutingFromPortToPort(GeometryGraph graph) {
            var edges = graph.Edges.ToArray();
            SetCurvesToNull(edges);
            var portRouter = new InteractiveEdgeRouter(graph.Nodes.Select(n => n.BoundaryCurve), 3, 0.65*3, 0);
            portRouter.Run(); //calculates the whole visibility graph, takes a long time
            DrawEdgeWithPort(edges[0], portRouter, 0.3, 0.4);
            DrawEdgeWithPort(edges[1], portRouter, 0.7, 1.5*Math.PI);
                //I know here that my node boundary curves are ellipses so the parameters run from 0 to 2Pi
            //otherwise the curve parameter runs from curve.ParStart, to curve.ParEnd

#if TEST
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
        }

        static void SetCurvesToNull(Edge[] edges) {
            foreach (var edge in edges)
                edge.Curve = null;
        }

        static void DrawEdgeWithPort(Edge edge, InteractiveEdgeRouter portRouter, double par0, double par1) {

            var port0 = new CurvePort(edge.Source.BoundaryCurve, par0);
            var port1 = new CurvePort(edge.Target.BoundaryCurve,par1);

            SmoothedPolyline sp;
            var spline = portRouter.RouteSplineFromPortToPortWhenTheWholeGraphIsReady(port0, port1, true, out sp);
            
            Arrowheads.TrimSplineAndCalculateArrowheads(edge.EdgeGeometry,
                                                         edge.Source.BoundaryCurve,
                                                         edge.Target.BoundaryCurve,
                                                         spline, true);

        }
    }
}
