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

