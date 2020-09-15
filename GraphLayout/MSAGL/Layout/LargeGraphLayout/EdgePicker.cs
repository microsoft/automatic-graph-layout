using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    internal class EdgePicker {
        readonly LgData lgData;
        readonly IZoomLevelCalculator nodeZoomLevelCalculator;
        readonly int _nodeCountOnLevel;
        internal static void SetEdgeInfosZoomLevelsAndIcreaseRanks(LgData lgData,
                                                    IZoomLevelCalculator nodeZoomLevelCalculator,
                                                    int nodeCountOnLevel) {
            var edgePicker = new EdgePicker(lgData, nodeZoomLevelCalculator, nodeCountOnLevel);
            edgePicker.Run();
        }

        void Run() {
            if (_nodeCountOnLevel == lgData.GeometryNodesToLgNodeInfos.Count) {
                foreach (var e in lgData.GeometryEdgesToLgEdgeInfos.Keys)
                    UpdateEdgeInfoZoomLevel(e);
                return;
            }

            FillShortRoutes();
        }


        readonly int zoomLevel;
     
        EdgePicker(LgData lgData, IZoomLevelCalculator nodeZoomLevelCalculator,
                   int nodeCountOnLevel) {
            this.lgData = lgData;
            this.nodeZoomLevelCalculator = nodeZoomLevelCalculator;
            this._nodeCountOnLevel = nodeCountOnLevel;
            zoomLevel = (int) nodeZoomLevelCalculator.SortedLgNodeInfos[nodeCountOnLevel - 1].ZoomLevel;
        }

        void FillShortRoutes() {
            //creating one edge long routes between the nodes of the level
             for (int i = 0; i < _nodeCountOnLevel; i++)
                FillShortRoutesOfNode(nodeZoomLevelCalculator.SortedLgNodeInfos[i].GeometryNode);

        }

        void FillShortRoutesOfNode(Node node) {
            foreach (var e in node.OutEdges)
                TryPickingEdge(e);
        }

        void TryPickingEdge(Edge edge) {
            if (lgData.GeometryNodesToLgNodeInfos[edge.Target].ZoomLevel <= zoomLevel)
                //connected to a node from the same level
                UpdateEdgeInfoZoomLevel(edge);
        }

        void UpdateEdgeInfoZoomLevel(Edge edge) {
            var sourceNodeInfo = lgData.GeometryNodesToLgNodeInfos[edge.Source];
            var targetNodeInfo = lgData.GeometryNodesToLgNodeInfos[edge.Target];
            UpdateEdgeInfoZoomLevel(edge, sourceNodeInfo.Rank + targetNodeInfo.Rank);
        }

        void UpdateEdgeInfoZoomLevel(Edge edge, double edgeRank) {
            var edgeInfo = lgData.GeometryEdgesToLgEdgeInfos[edge];
            TryToDecreaseZoomLevel(edgeInfo);
            TryToIncreaseRank(edgeInfo, edgeRank);
        }

        static void TryToIncreaseRank(LgEdgeInfo edgeInfo, double edgeRank) {
            if (edgeInfo.Rank < edgeRank)
                edgeInfo.Rank = edgeRank;
        }

        void TryToDecreaseZoomLevel(LgEdgeInfo edgeInfo) {
            if (edgeInfo.ZoomLevel > zoomLevel)
                edgeInfo.ZoomLevel = zoomLevel;
        }
    }
}