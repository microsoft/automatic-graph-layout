import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);

var status = document.getElementById("status");

function appendStatus(text) {
    var p = document.createElement("p");
    p.appendChild(document.createTextNode(text));
    status.appendChild(p);
}

function makeInitialGraph() {
    graphControl.graph = new G.GGraph();
    graphControl.onNodeClick = (n => appendStatus("Clicked " + n.id));
    graphControl.onEdgeClick = (e => appendStatus("Clicked " + e.id));
    graphControl.graph.settings.aspectRatio = graphView.offsetWidth / graphView.offsetHeight;

    graphControl.graph.addNode(new G.GNode({ id: "node1", label: "Node 1", fill: "white" }));
    graphControl.graph.addNode(new G.GNode({ id: "node2", label: "Node 2", fill: "white" }));
    graphControl.graph.addNode(new G.GNode({ id: "node3", label: "Node 3", fill: "white" }));

    graphControl.graph.addEdge(new G.GEdge({ id: "edge13", source: "node1", target: "node3" }));
    graphControl.graph.addEdge(new G.GEdge({ id: "edge23", source: "node2", target: "node3" }));

    graphControl.graph.createNodeBoundariesForSVGInContainer(graphView);
    graphControl.graph.beginLayoutGraph(() => { graphControl.drawGraph(); appendStatus("Ready"); });
}

makeInitialGraph();