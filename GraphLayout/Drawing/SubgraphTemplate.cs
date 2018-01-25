using System.Collections.Generic;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// used to keep the result of the parsing subgraphs temporarily
    /// </summary>
    internal class SubgraphTemplate {
        internal Subgraph Subgraph;
        internal List<string> SubgraphIdList = new List<string>();
        internal List<string> NodeIdList = new List<string>();
    }
}