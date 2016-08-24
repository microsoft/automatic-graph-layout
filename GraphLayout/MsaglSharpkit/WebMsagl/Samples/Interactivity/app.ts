import G = require("../../Scripts/src/ggraph");
import SVGGraph = require("../../Scripts/src/svggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new SVGGraph(graphView);

var statusText = document.getElementById("status");
var mousePointText = document.getElementById("mousePoint");
var objectUnderCursorText = document.getElementById("objectUnderCursor");
var dragObjectText = document.getElementById("dragObject");

function appendStatus(text) {
    var p = document.createElement("p");
    p.appendChild(document.createTextNode(text));
    statusText.appendChild(p);
}

function makeInitialGraph() {
    graphControl.graph = new G.GGraph();
    graphControl.onNodeClick = (n => appendStatus("Clicked " + n.id));
    graphControl.onEdgeClick = (e => appendStatus("Clicked " + e.id));
    graphControl.graph.settings.aspectRatio = graphView.offsetWidth / graphView.offsetHeight;

    graphControl.graph.addNode(new G.GNode({ id: "node1", label: "Node 1", fill: "white" }));
    graphControl.graph.addNode(new G.GNode({ id: "node2", label: "Node 2", fill: "white" }));
    graphControl.graph.addNode(new G.GNode({ id: "node3", label: "Node 3", fill: "white" }));

    graphControl.graph.addEdge(new G.GEdge({ id: "edge13", label: "Edge 1-3", source: "node1", target: "node3" }));
    graphControl.graph.addEdge(new G.GEdge({ id: "edge23", label: "Edge 2-3", source: "node2", target: "node3" }));

    graphControl.graph.createNodeBoundariesForSVGInContainer(graphView);
    graphControl.graph.beginLayoutGraph(() => { graphControl.drawGraph(); updateTexts(); setInterval(updateTexts, 100); });
}

makeInitialGraph();

function updateTexts() {
    mousePointText.textContent = JSON.stringify(graphControl.getMousePoint());
    objectUnderCursorText.textContent = JSON.stringify(graphControl.getObjectUnderMouseCursor());
    dragObjectText.textContent = JSON.stringify(graphControl.getDragObject());
}