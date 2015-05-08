using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Node = Microsoft.Msagl.Drawing.Node;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    internal class LgStringFinder {
        readonly GraphmapsViewer _graphViewer;
        readonly Func<Node, LgNodeInfo> _drawingNodeToLgNodeInfo;
        int _offsetInHits = -1;
        string _searchString = "";
        readonly List<LgNodeInfo> _hits = new List<LgNodeInfo>();
        Graph _graph;

        internal LgStringFinder(GraphmapsViewer graphViewer, Func<Drawing.Node, LgNodeInfo> drawingNodeToLgNodeInfo) {
            _graphViewer = graphViewer;
            _drawingNodeToLgNodeInfo = drawingNodeToLgNodeInfo;
        }

        internal LgNodeInfo Find(string text) {
            text = text.ToLower();
            if (_graphViewer.Graph != _graph) {
                _graph = _graphViewer.Graph;                
                _searchString = text;
                FillHits();
            }
            else if (text != _searchString) {
                _searchString = text;
                FillHits();
            }
            if (_hits.Count == 0)
                return null;
            _offsetInHits++;
            if (_offsetInHits == _hits.Count)
                _offsetInHits = 0;
            return _hits[_offsetInHits];
        }

        void FillHits() {
            _hits.Clear();
            _offsetInHits = -1;
            foreach (var node in _graph.Nodes)  
                if (node.LabelText.ToLower().Contains(_searchString))
                    _hits.Add(_drawingNodeToLgNodeInfo(node));
        }
    }
}