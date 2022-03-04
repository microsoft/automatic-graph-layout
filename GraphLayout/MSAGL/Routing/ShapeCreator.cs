using System;
using System.Collections.Generic;
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
            
            foreach (var c in graph.RootCluster.Clusters)
                GetShapesOnDict(nodesToShapes, c);
                
            return nodesToShapes.Values;
        }

        private static void GetShapesOnDict(Dictionary<Node, Shape> nodesToShapes, Cluster c) {
            if (c.IsCollapsed) return;
            nodesToShapes.TryGetValue(c, out Shape cShape);
            if (cShape == null) {
              cShape=  nodesToShapes[c] = CreateShapeWithClusterBoundaryPort(c);
            }
            foreach (var n in c.Nodes) {
                nodesToShapes.TryGetValue(n, out Shape nShape);
                if (nShape == null) {
                    nShape = nodesToShapes[n] = CreateShapeWithCenterPort(n);
                }
                cShape.AddChild(nShape);
            }
            foreach (var cc in c.Clusters) {
                GetShapesOnDict(nodesToShapes, cc);
                cShape.AddChild(nodesToShapes[cc]);
            }
        }



        /// <summary>
        /// Creates a shape with a RelativeFloatingPort for the node center, attaches it to the shape and all edges
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Shape obstacle for the node with simple port</returns>
        static Shape CreateShapeWithCenterPort(Node node)
        {
            Debug.Assert(node is Cluster == false);
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
#if TEST_MSAGL
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
#if TEST_MSAGL
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
