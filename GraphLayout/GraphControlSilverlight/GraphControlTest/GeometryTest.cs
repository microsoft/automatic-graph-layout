/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Net;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace GraphControlTest
{
    public static class GeometryTest
    {
        public static GeometryGraph CreateComplex()
        {
            GeometryGraph graph = new GeometryGraph();
            var nodeInD = new Node();
            graph.Nodes.Add(nodeInD);
            var nodePrecursor = new Node();
            graph.Nodes.Add(nodePrecursor);
            var nodeEarlyMeiosis = new Node();
            graph.Nodes.Add(nodeEarlyMeiosis);
            var nodeMeiosis = new Node();
            graph.Nodes.Add(nodeMeiosis);



            return graph;
        }

        public static GeometryGraph Create()
        {
            GeometryGraph graph = new GeometryGraph();
            var nodeA0 = new Node();
            graph.Nodes.Add(nodeA0);
            var nodeA1 = new Node();
            graph.Nodes.Add(nodeA1);
            var nodeA2 = new Node();
            graph.Nodes.Add(nodeA2);
            var nodeA3 = new Node();
            graph.Nodes.Add(nodeA3);
            var edgeA0A1 = new Edge(nodeA0, nodeA1);
            edgeA0A1.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA0.AddOutEdge(edgeA0A1);
            nodeA1.AddInEdge(edgeA0A1);
            graph.Edges.Add(edgeA0A1);
            var edgeA0A2 = new Edge(nodeA0, nodeA2);
            edgeA0A2.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA0.AddOutEdge(edgeA0A2);
            nodeA2.AddInEdge(edgeA0A2);
            graph.Edges.Add(edgeA0A2);
            var edgeA2A1 = new Edge(nodeA2, nodeA1);
            edgeA2A1.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA2.AddOutEdge(edgeA2A1);
            nodeA1.AddInEdge(edgeA2A1);
            graph.Edges.Add(edgeA2A1);
            var edgeA0A3 = new Edge(nodeA0, nodeA3);
            edgeA0A3.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA0.AddOutEdge(edgeA0A3);
            nodeA3.AddInEdge(edgeA0A3);
            graph.Edges.Add(edgeA0A3);

            graph.RootCluster.AddChild(nodeA0);
            graph.RootCluster.AddChild(nodeA3);
            var cluster = new Cluster();
            graph.RootCluster.AddChild(cluster);
            cluster.AddChild(nodeA1);
            cluster.AddChild(nodeA2);

            // This is where I add the edge connecting to a cluster.
            var edgeA0cluster = new Edge(nodeA0, cluster);
            edgeA0cluster.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA0.AddOutEdge(edgeA0cluster);
            cluster.AddInEdge(edgeA0cluster);
            graph.Edges.Add(edgeA0cluster);

            var nodeA01 = new Node();
            graph.Nodes.Add(nodeA01);
            graph.RootCluster.AddChild(nodeA01);
            var nodeA02 = new Node();
            graph.Nodes.Add(nodeA02);
            graph.RootCluster.AddChild(nodeA02);
            var nodeA03 = new Node();
            graph.Nodes.Add(nodeA03);
            graph.RootCluster.AddChild(nodeA03);
            var edgeA0A01 = new Edge(nodeA0, nodeA01);
            edgeA0A01.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA0.AddOutEdge(edgeA0A01);
            nodeA01.AddInEdge(edgeA0A01);
            graph.Edges.Add(edgeA0A01);
            var edgeA01A02 = new Edge(nodeA01, nodeA02);
            edgeA01A02.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA01.AddOutEdge(edgeA01A02);
            nodeA02.AddInEdge(edgeA01A02);
            graph.Edges.Add(edgeA01A02);
            var edgeA02A03 = new Edge(nodeA02, nodeA03);
            edgeA02A03.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA02.AddOutEdge(edgeA02A03);
            nodeA03.AddInEdge(edgeA02A03);
            graph.Edges.Add(edgeA02A03);

            return graph;
        }

        public static GeometryGraph CreateSimple()
        {
            GeometryGraph graph = new GeometryGraph();
            var nodeA0 = new Node();
            graph.Nodes.Add(nodeA0);
            var nodeA1 = new Node();
            graph.Nodes.Add(nodeA1);

            graph.RootCluster.AddChild(nodeA0);
            var cluster = new Cluster();
            graph.RootCluster.AddChild(cluster);
            cluster.AddChild(nodeA1);

            // This is where I add the edge connecting to a cluster.
            var edgeA0cluster = new Edge(nodeA0, cluster);
            edgeA0cluster.EdgeGeometry = new EdgeGeometry() { TargetArrowhead = new Arrowhead() };
            nodeA0.AddOutEdge(edgeA0cluster);
            cluster.AddInEdge(edgeA0cluster);
            graph.Edges.Add(edgeA0cluster);

            return graph;
        }

        public static void Layout(GeometryGraph graph)
        {
            foreach (Node n in graph.Nodes)
                n.BoundaryCurve = CurveFactory.CreateEllipse(20.0, 10.0, new Point());
            foreach (Cluster c in graph.RootCluster.AllClustersDepthFirst())
                c.BoundaryCurve = c.BoundingBox.Perimeter();

            var settings = new FastIncrementalLayoutSettings();
            settings.AvoidOverlaps = true;
            settings.NodeSeparation = 30;
            settings.RouteEdges = true;

            LayoutHelpers.CalculateLayout(graph, settings, new CancelToken());
            foreach (Cluster c in graph.RootCluster.AllClustersDepthFirst())
                c.BoundaryCurve = c.BoundingBox.Perimeter();

            var bundlingsettings = new BundlingSettings() { EdgeSeparation = 5, CreateUnderlyingPolyline = true };
            var router = new SplineRouter(graph, 10.0, 1.25, Math.PI / 6.0, bundlingsettings);
            router.Run();

            graph.UpdateBoundingBox();
        }

        public static void Go()
        {
            GeometryGraph gg = Create();
            for (int i = 0; i < 100; i++)
                Layout(gg);
        }
    }
}
