using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Drawing
{
    ///<summary>
    ///</summary>
    public class Subgraph : Node {
        /// <summary>
        /// the settings that should be applied to this cluster layout
        /// </summary>
        public LayoutAlgorithmSettings LayoutSettings { get; set; }

        double diameterOfOpenCollapseButton=10;

        /// <summary>
        /// the diameter of the symbol for collapsing/opening the subgraph
        /// </summary>
        public double DiameterOfOpenCollapseButton {
            get { return diameterOfOpenCollapseButton; }
            set { diameterOfOpenCollapseButton = value; }
        }

        Color collapseButtonColorInactive=Color.Cornsilk;
        Color collapseButtonColorActive=Color.Bisque;
        /// <summary>
        /// 
        /// </summary>
        public Color CollapseButtonColorInactive {
            get { return collapseButtonColorInactive; }
            set { collapseButtonColorInactive = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Color CollapseButtonColorActive
        {
            get { return collapseButtonColorActive; }
            set { collapseButtonColorActive = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Subgraph ParentSubgraph { get; set; }
        ///<summary>
        ///</summary>
        public Subgraph(string id) : base(id)
        {
        }

        internal Set<Subgraph> subgraphs = new Set<Subgraph>();
        internal Set<Node> nodes = new Set<Node>();
        bool isUpdated;
        

#if TEST_MSAGL 
        /// <summary>
        /// to string
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Id;
        }
#endif

        ///<summary>
        ///</summary>
        public IEnumerable<Node> Nodes { get { return nodes; } }
        ///<summary>
        ///</summary>
        public IEnumerable<Subgraph> Subgraphs { get { return subgraphs; } }
        
        /// <summary>
        /// the flag showing that the was a change under this cluster
        /// </summary>
        public bool IsUpdated
        {
            get { return isUpdated; }
            set {
                isUpdated = value;
                if (ParentSubgraph != null)
                    ParentSubgraph.IsUpdated = value;
            }
        }

        

        ///<summary>
        ///</summary>
        public IEnumerable<Subgraph> AllSubgraphsDepthFirst()
        {
            foreach (Subgraph c in subgraphs)
            {
                foreach (Subgraph d in c.AllSubgraphsDepthFirst())
                {
                    yield return d;
                }
            }
            yield return this;
        }

        ///<summary>
        ///</summary>
        public IEnumerable<Subgraph> AllSubgraphsDepthFirstExcludingSelf()
        {
            foreach (Subgraph c in subgraphs)
            {
                foreach (Subgraph d in c.AllSubgraphsDepthFirst())
                {
                    yield return d;
                }
            }
            
        }

        ///<summary>
        ///</summary>
        ///<param name="node"></param>
        public void AddNode(Node node)
        {
            nodes.Insert(node);
            IsUpdated = true;
        }

        /// <summary>
        /// Removes the node from this subgraph.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        public void RemoveNode(Node node)
        {
            nodes.Remove(node);
            IsUpdated = true;
        }

        ///<summary>
        ///</summary>
        ///<param name="subgraph"></param>
        public void AddSubgraph(Subgraph subgraph) {
            if (subgraph.ParentSubgraph != null) 
                subgraph.ParentSubgraph.RemoveSubgraph(subgraph);
            subgraph.ParentSubgraph = this;
            subgraphs.Insert(subgraph);
            IsUpdated = true;
        }

        ///<summary>
        ///</summary>
        ///<param name="subgraph"></param>
        public void RemoveSubgraph(Subgraph subgraph)
        {
            subgraphs.Remove(subgraph);
            subgraph.ParentSubgraph = null;
            IsUpdated = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Subgraph> AllSubgraphsWidthFirstExcludingSelf() {
            foreach (Subgraph c in subgraphs) {
                yield return c;
                foreach (Subgraph d in c.AllSubgraphsWidthFirstExcludingSelf())
                    yield return d;
            }
        }
    }
}