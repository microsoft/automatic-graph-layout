import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphContainer = document.getElementById("graphContainer");
var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);

var showButton = <HTMLButtonElement>document.getElementById("showButton");
var hideButton = <HTMLButtonElement>document.getElementById("hideButton");
var renderButton = <HTMLButtonElement>document.getElementById("renderButton");

function showButtonClicked() {
    graphContainer.style.display = "block";
}

function hideButtonClicked() {
    graphContainer.style.display = "none";
}

function renderButtonClicked() {
    graphControl.graph = new G.GGraph;
    graphControl.graph.addNode(new G.GNode({ id: "node1", label: "Node 1" }));
    graphControl.graph.addNode(new G.GNode({ id: "node2", label: "Node 2" }));
    graphControl.graph.addNode(new G.GNode({ id: "node3", label: "Node 3" }));
    graphControl.graph.addEdge(new G.GEdge({ id: "edge12", source: "node1", target: "node2" }));
    graphControl.graph.addEdge(new G.GEdge({ id: "edge13", source: "node1", target: "node3" }));
    graphControl.graph.addEdge(new G.GEdge({ id: "edge23", source: "node2", target: "node3" }));
    graphControl.graph.createNodeBoundariesFromSVG();
    graphControl.graph.layoutCallback = () => graphControl.drawGraph();
    graphControl.graph.beginLayoutGraph();
}

showButton.onclick = showButtonClicked;
hideButton.onclick = hideButtonClicked;
renderButton.onclick = renderButtonClicked;