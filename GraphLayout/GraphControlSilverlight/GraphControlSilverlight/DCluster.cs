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
