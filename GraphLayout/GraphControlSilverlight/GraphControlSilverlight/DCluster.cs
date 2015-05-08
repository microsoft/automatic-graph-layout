using System;
using System.Net;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Layout;
using DrawingCluster = Microsoft.Msagl.Drawing.Subgraph;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public class DCluster : DNode
    {
        internal DCluster(DObject parent, DrawingCluster drawingCluster)
            : base(parent, drawingCluster)
        {
            Node.Attr.FillColor = Drawing.Color.White;
        }

        public DrawingCluster DrawingCluster { get { return DrawingObject as DrawingCluster; } }

        public Cluster GeometryCluster { get { return GeometryNode as Cluster; } }

        private List<DCluster> m_Clusters = new List<DCluster>();
        public IEnumerable<DCluster> Clusters { get { return m_Clusters; } }
        
        internal void AddCluster(DCluster cluster)
        {
            m_Clusters.Add(cluster);
        }

        private List<DNode> m_Nodes = new List<DNode>();
        public IEnumerable<DNode> Nodes { get { return m_Nodes; } }

        internal void AddNode(DNode node)
        {
            m_Nodes.Add(node);
        }

        public IEnumerable<DCluster> AllClustersDepthFirst()
        {
            foreach (DCluster c in m_Clusters)
                foreach (DCluster d in c.AllClustersDepthFirst())
                    yield return d;
            yield return this;
        }
    }
}
