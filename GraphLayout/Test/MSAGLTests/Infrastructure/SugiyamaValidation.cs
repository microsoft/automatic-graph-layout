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
