import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");
var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);
graphControl.allowEditing = false;
var graph: G.GGraph = null;

function generateGraph() {
    graph = new G.GGraph();
    graphControl.setGraph(graph);
    graph.addNode(new G.GCluster({
        id: "clusterA", label: "Cluster A", children: [
            new G.GNode({ id: "node1", label: "Node 1" }),
            new G.GNode({ id: "node2", label: "Node 2" }),
            new G.GCluster({
                id: "clusterB", label: "Cluster B", children: [
                    new G.GNode({ id: "node3", label: "Node 3" }),
                    new G.GNode({ id: "node4", label: "Node 4" }),
                ]
            })
        ]
    }));
    graph.addEdge(new G.GEdge({ id: "edge12", source: "node1", target: "node2" }));
    graph.addEdge(new G.GEdge({ id: "edge2B", source: "node2", target: "clusterB" }));
    graph.addEdge(new G.GEdge({ id: "edge34", source: "node3", target: "node4" }));
    graph.addEdge(new G.GEdge({ id: "edge13", source: "node1", target: "node3" }));
    graph.createNodeBoundariesForSVGInContainer(graphView);

    graph.layoutCallbacks.add(() => graphControl.drawGraph());
    graph.beginLayoutGraph();
}

generateGraph();