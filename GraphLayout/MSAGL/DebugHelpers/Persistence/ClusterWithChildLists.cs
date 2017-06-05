using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.DebugHelpers {
    /// <summary>
    /// this class is needed for temporarily holding the list of integers representing cluster childs,
    /// and the list of node Id's representing cluster child nodes
    /// </summary>
    public class ClusterWithChildLists {
        internal List<string> ChildClusters = new List<string>();
        internal List<string> ChildNodes = new List<string>();
        internal Cluster Cluster;
        internal ClusterWithChildLists(Cluster cl) {
            Cluster = cl;
        }
    }
}