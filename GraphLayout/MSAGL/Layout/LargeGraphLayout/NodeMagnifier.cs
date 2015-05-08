using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout
{
    class NodeMagnifier
    {
         Dictionary<LgNodeInfo,Tuple<double, double>> _nodesScale = new Dictionary<LgNodeInfo, Tuple<double, double>>();

        public void ScaleAllNodes(IEnumerable<LgNodeInfo> nodes, double scale)
        {
            _nodesScale = new Dictionary<LgNodeInfo, Tuple<double, double>>();

            foreach (var node in nodes)
            {
                var bbox = node.BoundingBox.Clone();
                double d = (scale - 1) * (Math.Min(bbox.Width, bbox.Height));
                bbox = new Rectangle(bbox.LeftBottom - new Point(d, d), bbox.RightTop + new Point(d, d));

                double xScale = bbox.Width / node.BoundingBox.Width;
                double yScale = bbox.Height / node.BoundingBox.Height;

                ScaleNode(node, xScale, yScale);

                _nodesScale[node] = new Tuple<double, double>(xScale, yScale);

            }
        }

        public static void ScaleNode(LgNodeInfo node, double xScale, double yScale)
        {
            var delta = node.BoundingBox.Center;
            node.GeometryNode.BoundaryCurve.Translate(-delta);
            node.GeometryNode.BoundaryCurve = node.GeometryNode.BoundaryCurve.ScaleFromOrigin(xScale, yScale);
            node.GeometryNode.BoundaryCurve.Translate(delta);
        }

        public void ScaleAllNodesBack()
        {
            if (_nodesScale == null) return;
            foreach (var node in _nodesScale.Keys)
            {
                var scale = _nodesScale[node];
                ScaleNode(node, 1 / scale.Item1, 1 / scale.Item2);
            }
        }

        public void ScaleAllNodesUniformly(IEnumerable<LgNodeInfo> nodes, double d)
        {
            foreach (var node in nodes)
            {
                ScaleNode(node, d, d);
            }
        }

        public void MakeAllNodesSameSize(List<LgNodeInfo> nodes)
        {
            double maxDim = (nodes.Select(n => Math.Max(n.BoundingBox.Width, n.BoundingBox.Height))).Max();

            double maxWidth = (nodes.Select(n => n.BoundingBox.Width)).Max();
            double maxHeight = (nodes.Select(n => n.BoundingBox.Height)).Max();
            foreach (var ni in nodes)
            {
                //var scale = maxDim/Math.Max(ni.BoundingBox.Width, ni.BoundingBox.Height);
                //var scaleX = maxWidth / ni.BoundingBox.Width;
                var scaleY = maxHeight / ni.BoundingBox.Height;
                ScaleNode(ni, scaleY, scaleY);
            }
        }
    }
}
