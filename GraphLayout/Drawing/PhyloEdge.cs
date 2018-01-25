using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// a phylogenetic edge, an edge with the specified length
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Phylo")]
    public class PhyloEdge : Edge {
        private double realLength;
        /// <summary>
        /// the real edge length
        /// </summary>
        public double RealLength {
            get { return realLength; }
            set { realLength = value; }
        }
        double length = 1;
        /// <summary>
        /// the edge length
        /// </summary>
        public double Length {
            get { return length; }
            set { length = value; }
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public PhyloEdge(string source, string target) : base(source, null, target) { }
        /// <summary>
        /// constructor with the length as a parameter
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="lengthP"></param>
        public PhyloEdge(string source, string target, double lengthP)
            : base(source, null, target) {
            this.Length = lengthP;
        }


        private bool negative;
 /// <summary>
 /// 
 /// </summary>
        public bool Negative {
            get { return negative; }
            set { negative = value; }
        }

    }
}

