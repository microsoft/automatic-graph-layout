//-----------------------------------------------------------------------
// <copyright file="SugiyamaValidation.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Some GeometryGraph validity verification helpers
    /// </summary>
    public static class SugiyamaValidation
    {
        private const double Tolerance = 0.1;

        /// <summary>
        /// Validate two nodes not overlapping with each other
        /// </summary>        
        internal static void ValidateNoNodeOverlapping(Node node, Node node2)
        {
            if (node == node2 || node is Cluster || node2 is Cluster)
            {
                return;
            }
            Assert.IsFalse(node.BoundingBox.Intersects(node2.BoundingBox), string.Format("Node (ID: {0}, BoundingBox: {1}) overlaps with Node (ID: {2}, BoundingBox: {3})", node.UserData, node.BoundingBox.ToString(), node2.UserData, node2.BoundingBox.ToString()));            
        }

        /// <summary>
        /// Validate two nodes on same layer have valid separation between them
        /// </summary>
        internal static void ValidateIntraLayerNodeSeparation(Node node, Node node2, SugiyamaLayoutSettings settings)
        {

            if (node == node2 || node is Cluster || node2 is Cluster)
            {
                return;
            }
            if (OnSameLayer(node, node2, settings))
            {
                if (Math.Abs(node.Center.Y - node2.Center.Y) <= Tolerance)
                {
                    Rectangle left = node.BoundingBox;
                    Rectangle right = node2.BoundingBox;
                    Rectangle temp;
                    if (left.Left > right.Left)
                    {
                        temp = left;
                        left = right;
                        right = temp;
                    }
                    Assert.IsTrue(Math.Abs(right.Left - left.Right) >= settings.NodeSeparation, string.Format("Node (ID: {0}, BoundingBox: {1}) has less separation from Node (ID: {2}, BoundingBox: {3})", node.UserData, node.BoundingBox.ToString(), node2.UserData, node2.BoundingBox.ToString()));
                }
                else
                {
                    Rectangle top = node.BoundingBox;
                    Rectangle bottom = node2.BoundingBox;
                    Rectangle temp;
                    if (top.Top < bottom.Top)
                    {
                        temp = top;
                        top = bottom;
                        bottom = temp;
                    }
                    Assert.IsTrue(Math.Abs(top.Bottom - bottom.Top) >= settings.NodeSeparation, string.Format("Node (ID: {0}, BoundingBox: {1}) has less separation from Node (ID: {2}, BoundingBox: {3})", node.UserData, node.BoundingBox.ToString(), node2.UserData, node2.BoundingBox.ToString()));
                }
            }
        }

        /// <summary>
        /// Validate all layers have valid separation
        /// </summary>
        internal static void ValidateLayerSeparation(GeometryGraph graph, SugiyamaLayoutSettings settings)
        {
            bool isVertical = IsVerticallyLayered(settings.Transformation);
            SortedList<double, SortedList<double, Node>> layers = GetLayers(graph, isVertical);
            
#if TEST_MSAGL
            Console.WriteLine("Setting for layer separation: " + settings.LayerSeparation);
            for (int j = 0; j < layers.Count; j++)
            {                
                SortedList<double, Node> layer = layers.Values[j];
                Console.WriteLine("Layer No. " + (j + 1));
                for (int k = 0; k < layer.Count; k++)
                {
                    Node node = layer.Values[k];
                    Console.WriteLine("\t On this layer, No. " + (k + 1) + " Node's boundary is " + node.BoundingBox);
                }
            }
#endif
            //Make sure no src/target of one edge stay in same layer
            foreach (IList<Node> layer in layers.Values.Select(l => l.Values))
            {
                ValidateSourceTargetNotOnSameLayer(layer);
            }            

            Tuple<Node, Node>[] layerNodes = new Tuple<Node, Node>[layers.Keys.Count];

            for (int i = 0; i < layers.Keys.Count; i++)
            {
                layerNodes[i] = GetBiggestNodesInLayer(layers.Values[i].Values, isVertical);
            }

            for (int i = 1; i < layers.Keys.Count - 1; i++)
            {
                if (isVertical)
                {
                    Assert.IsTrue(Math.Abs(layerNodes[i - 1].Item2.BoundingBox.Bottom - layerNodes[i].Item1.BoundingBox.Top) + Tolerance >= settings.LayerSeparation, string.Format("layer {0} not having right separation with layer {1}", i - 1, i));
                    Assert.IsTrue(Math.Abs(layerNodes[i + 1].Item2.BoundingBox.Top - layerNodes[i].Item1.BoundingBox.Bottom) + Tolerance >= settings.LayerSeparation, string.Format("layer {0} not having right separation with layer {1}", i + 1, i));
                }                                                                                                                           
                else                                                                                                                        
                {                                                                                                                           
                    Assert.IsTrue(Math.Abs(layerNodes[i - 1].Item2.BoundingBox.Right - layerNodes[i].Item1.BoundingBox.Left) + Tolerance >= settings.LayerSeparation, string.Format("layer {0} not having right separation with layer {1}", i - 1, i));
                    Assert.IsTrue(Math.Abs(layerNodes[i + 1].Item2.BoundingBox.Left - layerNodes[i].Item1.BoundingBox.Right) + Tolerance >= settings.LayerSeparation, string.Format("layer {0} not having right separation with layer {1}", i + 1, i));
                }
            }
        }

        /// <summary>
        /// Validate node edge not intersecting with each other
        /// </summary>
        internal static void ValidateNodeEdgeSeparation(Node node, Edge edge)
        {
            if (edge.Source == node || edge.Target == node || node is Cluster)
            {
                return;
            }
            Assert.IsFalse(Curve.CurvesIntersect(node.BoundaryCurve, edge.Curve), string.Format("Edge from node with ID {0} to node with ID {1} intersects with node with ID {2}", edge.Source.UserData, edge.Target.UserData, node.UserData));
        }

        //This method should be called with care since those constraints related data structures may be changed later in the product code        
        internal static void ValidateConstraints(GeometryGraph graph, SugiyamaLayoutSettings settings)
        {
            if (settings == null)
            {
                return;
            }            

            //First deals with Horizontal constraints
            HorizontalConstraintsForSugiyama horizontals = settings.HorizontalConstraints;

            //left right first
            foreach (Tuple<Node, Node> couple in horizontals.LeftRightConstraints)
            {
                ValidateLeftRightConstraint(couple.Item1, couple.Item2);
            }

            //up down vertical next
            foreach (Tuple<Node, Node> couple in horizontals.UpDownVerticalConstraints)
            {
                ValidateUpDownVerticalConstraint(couple.Item1, couple.Item2);
            }
            
            //Second deals with Vertical constraints
            VerticalConstraintsForSugiyama verticals = settings.VerticalConstraints;

            //up down first
            foreach (Tuple<Node, Node> couple in verticals.UpDownConstraints)
            {
                ValidateUpDownConstraint(couple.Item1, couple.Item2);
            }

            //same layer next

            //get layers information
            bool isVertical = IsVerticallyLayered(settings.Transformation);
            SortedList<double, SortedList<double, Node>> layers = GetLayers(graph, isVertical);

            foreach (Tuple<Node, Node> couple in verticals.SameLayerConstraints)
            {
                SortedList<double, Node> layer = isVertical ? layers[couple.Item1.Center.Y] : layers[couple.Item1.Center.X];
                ValidateNeighborConstraint(layer, isVertical, couple.Item1, couple.Item2);
            }
        }

        /// <summary>
        /// Verify Up Down Constraint
        /// </summary>
        internal static void ValidateUpDownConstraint(Node up, Node down)
        {
            if (up == down || up is Cluster || down is Cluster)
            {
                return;
            }
            Assert.IsTrue(up.BoundingBox.Bottom > down.BoundingBox.Top, string.Format("Node (ID: {0}, BoundingBox: {1}) not over Node (ID: {2}, BoundingBox: {3})", up.UserData, up.BoundingBox.ToString(), down.UserData, down.BoundingBox.ToString()));
        }

        /// <summary>
        /// Verify Up Down Vertical Constraint
        /// </summary>
        internal static void ValidateUpDownVerticalConstraint(Node up, Node down)
        {
            ValidateUpDownConstraint(up, down);
            Assert.AreEqual(up.Center.X, down.Center.X, Tolerance, string.Format("Node (ID: {0}, BoundingBox: {1}) not exactly vertically over Node (ID: {2}, BoundingBox: {3})", up.UserData, up.BoundingBox.ToString(), down.UserData, down.BoundingBox.ToString()));
        }

        /// <summary>
        /// Verify Left Right Constraint
        /// </summary>
        internal static void ValidateLeftRightConstraint(Node left, Node right)
        {
            if (left == right || left is Cluster || right is Cluster)
            {
                return;
            }
            Assert.IsTrue(left.BoundingBox.Right < right.BoundingBox.Left, string.Format("Node (ID: {0}, BoundingBox: {1}) not on left side of Node (ID: {2}, BoundingBox: {3})", left.UserData, left.BoundingBox.ToString(), right.UserData, right.BoundingBox.ToString()));
        }

        /// <summary>
        /// Verify Same Layer Neighbor Constraint
        /// </summary>
        internal static void ValidateNeighborConstraint(GeometryGraph graph, Node node, Node node2, SugiyamaLayoutSettings settings)
        {
            if (node == node2 || node is Cluster || node2 is Cluster)
            {
                return;
            }
          
            bool isVertical = IsVerticallyLayered(settings.Transformation);
            SortedList<double, SortedList<double, Node>> layers = GetLayers(graph, isVertical);

            SortedList<double, Node> layer = isVertical ? layers[node.Center.Y] : layers[node.Center.X];
            ValidateNeighborConstraint(layer, isVertical, node, node2);
        }

        /// <summary>
        /// Overload for Verify Same Layer Neighbor Constraint
        /// </summary>
        internal static void ValidateNeighborConstraint(SortedList<double, Node> layer, bool isVertical, Node node, Node node2)
        {
            if (node == node2 || node is Cluster || node2 is Cluster)
            {
                return;
            }

            Assert.IsTrue(layer.Values.Contains(node) && layer.Values.Contains(node2), "Layer not containing all nodes");            

            if (isVertical)
            {
                Assert.IsTrue(Math.Abs(layer.IndexOfKey(node.Center.X) - layer.IndexOfKey(node2.Center.X)) == 1, string.Format("Node (ID: {0}, BoundingBox: {1}) not neighbor with Node (ID: {2}, BoundingBox: {3})", node.UserData, node.BoundingBox.ToString(), node2.UserData, node2.BoundingBox.ToString()));
            }
            else
            {
                Assert.IsTrue(Math.Abs(layer.IndexOfKey(node.Center.Y) - layer.IndexOfKey(node2.Center.Y)) == 1, string.Format("Node (ID: {0}, BoundingBox: {1}) not neighbor with Node (ID: {2}, BoundingBox: {3})", node.UserData, node.BoundingBox.ToString(), node2.UserData, node2.BoundingBox.ToString()));
            }
        }

        /// <summary>
        /// Validate graph has met all validity requirements
        /// </summary>
        internal static void ValidateGraph(GeometryGraph graph, SugiyamaLayoutSettings settings)
        {
            //Node first
            foreach (Node node in graph.Nodes)
            {
                foreach (Node node2 in graph.Nodes)
                {
                    ValidateNoNodeOverlapping(node, node2);
                    ValidateIntraLayerNodeSeparation(node, node2, settings);                    
                }
            }

            //Layer next
            ValidateLayerSeparation(graph, settings);

            //Node/Edge last
            foreach (Node node in graph.Nodes)
            {
                foreach (Edge edge in graph.Edges)
                {
                    ValidateNodeEdgeSeparation(node, edge);
                }
            }
        }

        /// <summary>
        /// Validate on one layer, no nodes have edge relationship
        /// </summary>
        private static void ValidateSourceTargetNotOnSameLayer(IList<Node> layer)
        {
            foreach (Node node in layer)
            {
                foreach (Edge edge in node.InEdges)
                {
                    Node srcNode = edge.Source;
                    if (srcNode != node)
                    {
                        Assert.IsFalse(layer.Contains(srcNode), string.Format("Node (ID: {0}) in same layer with one of its edge's source node (ID: {1})", node.UserData, srcNode.UserData));
                    }
                }
                foreach (Edge edge in node.OutEdges)
                {
                    Node targetNode = edge.Target;
                    if (targetNode != node)
                    {
                        Assert.IsFalse(layer.Contains(targetNode), string.Format("Node (ID: {0}) in same layer with one of its edge's target node (ID: {1})", node.UserData, targetNode.UserData));
                    }
                }
            }
        }

        /// <summary>
        /// Get two "largest" nodes in a layer depending on the layer direction
        /// if one layer has only one node, the return value would be same node twice
        /// </summary>
        /// <returns>Two largest nodes of one layer based on layer direction</returns>
        private static Tuple<Node, Node> GetBiggestNodesInLayer(IList<Node> layer, bool isVertical)
        {
            Node item1 = layer[0];
            Node item2 = layer[0];
            for (int i = 1; i < layer.Count; i++)
            {
                if (isVertical)
                {
                    if (layer[i].BoundingBox.Top > item1.BoundingBox.Top)
                    {
                        item1 = layer[i];
                    }
                    if (layer[i].BoundingBox.Bottom > item2.BoundingBox.Bottom)
                    {
                        item2 = layer[i];
                    }
                }
                else
                {
                    if (layer[i].BoundingBox.Left < item1.BoundingBox.Left)
                    {
                        item1 = layer[i];
                    }
                    if (layer[i].BoundingBox.Right > item2.BoundingBox.Right)
                    {
                        item2 = layer[i];
                    }
                }
            }
            return new Tuple<Node, Node>(item1, item2);
        }

        /// <summary>
        /// Get all layers in a graph
        /// return structure uses x/y of layer center as the key and all layer nodes as the values
        /// </summary>
        /// <returns>A dictionary containing all layer information</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static SortedList<double, SortedList<double, Node>> GetLayers(GeometryGraph graph, bool isVertical)
        {
            if (graph == null)
            {
                throw new ArgumentNullException("graph");
            }

            SortedList<double, SortedList<double, Node>> layers = new SortedList<double, SortedList<double, Node>>();
            
            foreach (Node node in graph.Nodes)
            {
                if (node is Cluster)
                {
                    //skip clusters
                    continue;
                }

                double layerLocation = isVertical ? node.Center.Y : node.Center.X;
                double location = isVertical ? node.Center.X : node.Center.Y;

                double nearestKey = layers.Keys.FirstOrDefault(k => Math.Abs(k - layerLocation) <= Tolerance);

                if (nearestKey == default(double))
                {
                    SortedList<double, Node> newLayer = new SortedList<double, Node>();
                    layers.Add(layerLocation, newLayer);
                    newLayer.Add(location, node);
                }
                else
                {
                    layers[nearestKey].Add(location, node);
                }
            }

            return layers;
        }

        /// <summary>
        /// Check whether two nodes on the same layer
        /// </summary>
        /// <returns>True if they are on the same layer</returns>
        private static bool OnSameLayer(Node node, Node node2, SugiyamaLayoutSettings settings)
        {
            PlaneTransformation transformation = settings.Transformation;
            if (IsVerticallyLayered(transformation))
            {
                return Math.Abs(node.Center.Y - node2.Center.Y) <= Tolerance;
            }
            else
            {
                return Math.Abs(node.Center.X - node2.Center.X) <= Tolerance;
            }
        }

        /// <summary>
        /// Check whether graph layers in a vertical order
        /// </summary>
        /// <returns>True if layer is based on node Y coordinate</returns>
        private static bool IsVerticallyLayered(PlaneTransformation transformation)
        {
            //just check LR and RL cases first, other than that, assume to be TD or DT
            PlaneTransformation leftRight = PlaneTransformation.Rotation(Math.PI / 2);
            bool horizontial = true;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (Math.Abs(transformation.Elements[i][j] - leftRight.Elements[i][j]) > Tolerance)
                    {
                        horizontial = false;
                        break;
                    }
                }
                if (!horizontial)
                {
                    break;
                }
            }

            if (horizontial)
            {
                return false;
            }

            PlaneTransformation rightLeft = PlaneTransformation.Rotation(-Math.PI / 2);
            horizontial = true;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (Math.Abs(transformation.Elements[i][j] - rightLeft.Elements[i][j]) > Tolerance)
                    {
                        horizontial = false;
                        break;
                    }
                }
                if (!horizontial)
                {
                    break;
                }
            }

            return !horizontial;
        }
    }
}
