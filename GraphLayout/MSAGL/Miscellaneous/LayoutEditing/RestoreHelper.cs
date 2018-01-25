using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Prototype.LayoutEditing
{
    /// <summary>
    /// Helper class for creating RestoreData objects from GeometryGraph objects.
    /// </summary>
    public static class RestoreHelper
    {
        /// <summary>
        /// creates node restore datat
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(Node node)
        {
            return new NodeRestoreData(node.BoundaryCurve.Clone());
        }

        /// <summary>
        /// gets restore data for an edge
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(Edge edge)
        {
            return new EdgeRestoreData(edge);
        }

        /// <summary>
        /// creates graph restore data
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(GeometryGraph graph)
        {
            return new GraphRestoreData();
        }

        /// <summary>
        /// creates label restore data
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(Label label)
        {
            return new LabelRestoreData(label.Center);
        }

        /// <summary>
        /// calculates the restore data
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(GeometryObject geometryObject)
        {
            Node node = geometryObject as Node;
            if (node != null)
            {
                return GetRestoreData(node);
            }

            Edge edge = geometryObject as Edge;
            if (edge != null)
            {
                return GetRestoreData(edge);
            }

            Label label = geometryObject as Label;
            if (label != null)
            {
                return GetRestoreData(label);
            }

            GeometryGraph graph = geometryObject as GeometryGraph;
            if (graph != null)
            {
                return GetRestoreData(graph);
            }

            return null;
        }
    }
}
