using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// written in assumption of a single parent
    /// </summary>
    internal static class ShapeCreatorForRoutingToParents {
        public static IEnumerable<Shape> GetShapes(IEnumerable<Edge> inParentEdges, List<Edge> outParentEdges) {
            var nodesToShapes = new Dictionary<Node, Shape>();
            foreach (var edge in inParentEdges) {
                ProcessAncestorDescendantCouple((Cluster)edge.Target, edge.Source, nodesToShapes);
                InsertEdgePortsToShapes(nodesToShapes, edge);
            }
            foreach (var edge in outParentEdges) {
                ProcessAncestorDescendantCouple((Cluster)edge.Source, edge.Target, nodesToShapes);
                InsertEdgePortsToShapes(nodesToShapes, edge);
            }

            BindShapes(nodesToShapes);
            return nodesToShapes.Values;
        }

        private static void InsertEdgePortsToShapes(Dictionary<Node, Shape> nodesToShapes, Edge edge) {
            nodesToShapes[edge.Target].Ports.Insert(edge.TargetPort);
            nodesToShapes[edge.Source].Ports.Insert(edge.SourcePort);
        }

        static void BindShapes(Dictionary<Node, Shape> nodesToShapes) {
            foreach (var nodeShape in nodesToShapes) {
                var cluster = nodeShape.Key as Cluster;
                if(cluster==null) continue;
                var shape=nodeShape.Value;
                foreach (var child in Children(cluster) ) {
                    Shape childShape;
                    if(nodesToShapes.TryGetValue(child, out childShape))
                        shape.AddChild(childShape);
                }
            }
        }

        static void ProcessAncestorDescendantCouple(Cluster ancestor, Node node, Dictionary<Node, Shape> nodesToShapes) {
            Cluster parent=Parent(node);
            do {
                foreach (var n in Children(parent))
                    CreateShapeIfNeeeded(n, nodesToShapes);
                if (parent == ancestor)
                    break;
                parent = Parent(parent);                
            } while (true);
            CreateShapeIfNeeeded(parent, nodesToShapes);
        }

        static void CreateShapeIfNeeeded(Node n, Dictionary<Node, Shape> nodesToShapes) {
            if (nodesToShapes.ContainsKey(n)) return;
            nodesToShapes[n] = new RelativeShape(() => n.BoundaryCurve)
#if TEST_MSAGL
        {
                        UserData = n.ToString()
        }
#endif       
                ;
        }

        static IEnumerable<Node> Children(Cluster parent) {
            return parent.Clusters.Concat(parent.Nodes);
        }


        static Cluster Parent(Node node) {
            return node.ClusterParent;
        }

        internal static bool NumberOfActiveNodesIsUnderThreshold(List<Edge> inParentEdges, List<Edge> outParentEdges, int threshold) {
            var usedNodeSet = new Set<Node>();
            foreach (var edge in inParentEdges) 
                if(SetOfActiveNodesIsLargerThanThreshold((Cluster)edge.Target, edge.Source, usedNodeSet, threshold))
                   return false;
            
            foreach (var edge in outParentEdges) 
                if(SetOfActiveNodesIsLargerThanThreshold((Cluster)edge.Source, edge.Target, usedNodeSet, threshold))
                  return false;
            
            return true;
        }

        private static bool SetOfActiveNodesIsLargerThanThreshold(Cluster ancestor, Node node, Set<Node> usedNodeSet, int threshold) {
            Cluster parent = Parent(node);
            do {
                foreach (var n in Children(parent)) {
                    usedNodeSet.Insert(n);
                    if (usedNodeSet.Count > threshold)
                        return true;
                }
                if (parent == ancestor)
                    break;
                parent = Parent(parent);
            } while (true);
            
            usedNodeSet.Insert(parent);
            return usedNodeSet.Count > threshold;
        }
    }
}