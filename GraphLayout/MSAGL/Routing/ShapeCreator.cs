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
﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Routing
{
    /// <summary>
    /// Class for creating Shape elements from a Graph.
    /// </summary>
    static internal class ShapeCreator
    {
        /// <summary>
        /// For a given graph finds the obstacles for nodes and clusters, correctly parenting the obstacles
        /// according to the cluster hierarchy
        /// </summary>
        /// <param name="graph">graph with edges to route and nodes/clusters to route around</param>
        /// <returns>the set of obstacles with correct cluster hierarchy and ports</returns>
        public static IEnumerable<Shape> GetShapes(GeometryGraph graph)
        {
            var nodesToShapes = new Dictionary<Node, Shape>();
            var interestingNodes = graph.Nodes.Where(n => !n.UnderCollapsedCluster()).ToArray();
            foreach (var v in interestingNodes)
                nodesToShapes[v] = CreateShapeWithCenterPort(v);
            foreach (var c in graph.RootCluster.AllClustersDepthFirst()) {
                if (!c.IsCollapsed)
                    foreach (var v in c.Nodes)
                        if (!nodesToShapes.ContainsKey(v))
                            nodesToShapes[v] = CreateShapeWithCenterPort(v);


                if (c == graph.RootCluster) continue;
                var parent = nodesToShapes[c] = CreateShapeWithClusterBoundaryPort(c);
                if (c.IsCollapsed) continue;
                foreach (var v in c.Nodes)
                    parent.AddChild(nodesToShapes[v]);
                foreach (var d in c.Clusters)
                    parent.AddChild(nodesToShapes[d]);
            }

            foreach (var edge in graph.Edges) {
                Shape shape;
                if (nodesToShapes.TryGetValue(edge.Source, out shape)) {
                    if(edge.SourcePort!=null)
                        shape.Ports.Insert(edge.SourcePort);
                }
                if (nodesToShapes.TryGetValue(edge.Target, out shape)) {
                    if (edge.TargetPort != null)
                        shape.Ports.Insert(edge.TargetPort);
                }
                
            }

            return nodesToShapes.Values;
        }

        

        /// <summary>
        /// Creates a shape with a RelativeFloatingPort for the node center, attaches it to the shape and all edges
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Shape obstacle for the node with simple port</returns>
        static Shape CreateShapeWithCenterPort(Node node)
        {
            // Debug.Assert(ApproximateComparer.Close(node.BoundaryCurve.BoundingBox, node.BoundingBox), "node's curve doesn't fit its bounds!");
            var shape = new RelativeShape(() => node.BoundaryCurve);
            var port = new RelativeFloatingPort(() => node.BoundaryCurve, () => node.Center);
            shape.Ports.Insert(port);
            foreach (var e in node.InEdges)
                FixPortAtTarget(shape, port, e);
            foreach (var e in node.OutEdges)
                FixPortAtSource(shape, port, e);
            foreach (var e in node.SelfEdges) {
               FixPortAtSource(shape, port, e);
               FixPortAtTarget(shape, port, e);
            }
#if DEBUG
           // shape.UserData = node.ToString();
#endif
            return shape;
        }
        /// <summary>
        /// Creates a ClusterBoundaryPort for the cluster boundary, attaches it to the shape and all edges
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Shape obstacle for the node with simple port</returns>
        static Shape CreateShapeWithClusterBoundaryPort(Node node) {
            // Debug.Assert(ApproximateComparer.Close(node.BoundaryCurve.BoundingBox, node.BoundingBox), "node's curve doesn't fit its bounds!");
            Debug.Assert(node is Cluster);
            var shape = new RelativeShape(() => node.BoundaryCurve);
            var port = new ClusterBoundaryPort(()=>node.BoundaryCurve, ()=>node.Center);
            shape.Ports.Insert(port);
            foreach (var e in node.InEdges)
                FixPortAtTarget(shape, port, e);
            foreach (var e in node.OutEdges)
                FixPortAtSource(shape, port, e);
            foreach (var e in node.SelfEdges) {
                FixPortAtSource(shape, port, e);
                FixPortAtTarget(shape, port, e);
            }
#if DEBUG
            shape.UserData = node.ToString();
#endif
            return shape;
        }

        static void FixPortAtSource(Shape shape, Port port, Edge e) {
            if (e.SourcePort == null)
                e.SourcePort = port;
            else
                shape.Ports.Insert(e.SourcePort);
        }

        static void FixPortAtTarget(Shape shape, Port port, Edge e) {
            if (e.TargetPort == null)
                e.TargetPort = port;
            else 
                shape.Ports.Insert(e.TargetPort);
        }
    }
}
