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
using Microsoft.Msagl.Core.DataStructures;

namespace Microsoft.Msagl.Drawing {
    internal class VerticalConstraintsForLayeredLayout {
        internal readonly Set<Node> _maxLayerOfDrawingGraph = new Set<Node>();
        internal readonly Set<Node> _minLayerOfDrawingGraph = new Set<Node>();

        public void PinNodeToMaxLayer(Node node) {
            _maxLayerOfDrawingGraph.Insert(node);
        }

        public void PinNodeToMinLayer(Node node)
        {
            System.Diagnostics.Debug.Assert(node != null);
            _minLayerOfDrawingGraph.Insert(node);
        }

        /// <summary>
        /// unpins a node from max layer
        /// </summary>
        /// <param name="node"></param>
        internal void UnpinNodeFromMaxLayer(Node node)
        {
            _maxLayerOfDrawingGraph.Remove(node);
        }

        
        /// <summary>
        /// unpins a node from min layer
        /// </summary>
        /// <param name="node"></param>
        internal void UnpinNodeFromMinLayer(Node node)
        {
            _minLayerOfDrawingGraph.Remove(node);
        }


        internal Set<Tuple<Node, Node>> SameLayerConstraints = new Set<Tuple<Node, Node>>();
        internal readonly Set<Tuple<Node, Node>> UpDownConstraints = new Set<Tuple<Node, Node>>();
        public void Clear() {
            _maxLayerOfDrawingGraph.Clear();
            _minLayerOfDrawingGraph.Clear();
            SameLayerConstraints.Clear();
            UpDownConstraints.Clear();
        }
    }
}