using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;

namespace TestGraphmaps {
    internal class GraphmlNode {

        internal string Id;
        internal Set<GraphmlEdge> outEdges = new Set<GraphmlEdge>();
        internal Set<GraphmlEdge> inEdges = new Set<GraphmlEdge>();

        public GraphmlNode(string id) {
            Id = id;
        }

        public string Pubid { get; set; }
        public string Fullname { get; set; }

        public Dictionary<String,String> Data = new Dictionary<string, string>(); 
    }
}