import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);
graphControl.allowEditing = false;
var graph: G.GGraph = null;
var radiusControl = <HTMLInputElement>document.getElementById("radius");
var pointsControl = <HTMLInputElement>document.getElementById("points");
var runButton = <HTMLButtonElement>document.getElementById("runButton");
var saveButton = <HTMLButtonElement>document.getElementById("saveButton");

var radius = 0;
var points = 0;

function customDrawLabel(svg: Element, parent: Element, label: G.GLabel, owner: G.IElement) {
    if (label.content != "*")
        return false;
    if (points % 2 == 1) {
        var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        var d = "";
        for (var i = 0; i < points; i++) {
            var idx = i * 2 % points;
            var x = Math.cos(idx * Math.PI * 2 / points) * (radius - 1);
            var y = Math.sin(idx * Math.PI * 2 / points) * (radius - 1);
            d += i == 0 ? "M" : "L";
            d += x;
            d += "," + y;
        }
        d += "Z";
        path.setAttribute("d", d);
        var x = label.bounds.x;
        var y = label.bounds.y;
        path.setAttribute("transform", "translate(" + x + "," + y + ")");
        path.setAttribute("style", "stroke: black; stroke-linejoin: bevel; fill: blue; stroke-width: 3");
        parent.appendChild(path);
    }
    else {
        var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        var d = "";
        for (var k = 0; k <= 1; k++) {
            for (var i = 0; i < points / 2; i++) {
                var idx = i * 2 + k;
                var x = Math.cos(idx * Math.PI * 2 / points) * (radius - 1);
                var y = Math.sin(idx * Math.PI * 2 / points) * (radius - 1);
                d += i == 0 ? "M" : "L";
                d += x;
                d += "," + y;
            }
            d += "Z";
        }
        path.setAttribute("d", d);
        var x = label.bounds.x;
        var y = label.bounds.y;
        path.setAttribute("transform", "translate(" + x + "," + y + ")");
        path.setAttribute("style", "stroke: black; stroke-linejoin: bevel; fill: blue; stroke-width: 3");
        parent.appendChild(path);
    }
    return true;
}

function generateGraph() {
    graph = new G.GGraph();
    graphControl.setGraph(graph);
    graph.addNode(new G.GNode({ id: "node1", label: "Node 1" }));
    graph.addNode(new G.GNode({ id: "node2", label: "Node 2" }));
    graph.addNode(new G.GNode({ id: "node3", label: "Node 3" }));
    graph.addNode(new G.GNode({ id: "node4", label: "Node 4" }));
    graph.addNode(new G.GNode({ id: "nodeC", label: "*", boundaryCurve: G.GEllipse.make(radius * 2, radius * 2) }));
    graph.addEdge(new G.GEdge({ id: "edge1C", source: "node1", target: "nodeC" }));
    graph.addEdge(new G.GEdge({ id: "edge2C", source: "node2", target: "nodeC" }));
    graph.addEdge(new G.GEdge({ id: "edgeC3", source: "nodeC", target: "node3" }));
    graph.addEdge(new G.GEdge({ id: "edgeC4", source: "nodeC", target: "node4" }));
    graphControl.customDrawLabel = customDrawLabel;
    graph.createNodeBoundariesForSVGInContainer(graphView);

    graph.layoutCallbacks.add(() => graphControl.drawGraph());
    graph.beginLayoutGraph();
}

function startButtonClicked() {
    radius = parseInt(radiusControl.value);
    points = parseInt(pointsControl.value);
    generateGraph();
}

function saveButtonClicked() {
    graphControl.saveAsSVG();
}

runButton.onclick = startButtonClicked;
saveButton.onclick = saveButtonClicked;

startButtonClicked();