import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);
var graph: G.GGraph = null;

var nodeCountControl = <HTMLInputElement>document.getElementById("nodeCount");
var edgeCountControl = <HTMLInputElement>document.getElementById("edgeCount");
var startButton = <HTMLButtonElement>document.getElementById("startButton");
var stopButton = <HTMLButtonElement>document.getElementById("stopButton");
var working = document.getElementById("working");
var elapsed = document.getElementById("elapsed");
var edgeRoutingSelect = <HTMLSelectElement>document.getElementById("edgeRoutingSelect");
var layeredLayoutCheckBox = <HTMLInputElement>document.getElementById("layeredLayoutCheckBox");

function setGUIToRunning() {
    working.style.display = "inline";
    startButton.disabled = true;
    stopButton.disabled = false;
    elapsed.textContent = "";
}

function setGUIToNotRunning() {
    working.style.display = "none";
    startButton.disabled = false;
    stopButton.disabled = true;
}

function run(nodeCount: number, edgeCount: number) {
    graph = new G.GGraph();
    graphControl.setGraph(graph);
    graph.settings.aspectRatio = graphView.offsetWidth / graphView.offsetHeight;
    graph.settings.routing = edgeRoutingSelect.value;
    for (var i = 1; i <= nodeCount; i++)
        graph.addNode(new G.GNode({ id: "node" + i, label: "Node " + i }));
    for (var i = 1; i <= edgeCount; i++)
        graph.addEdge(new G.GEdge({
            id: "edge" + i, source: "node" + Math.floor(Math.random() * nodeCount + 1),
            target: "node" + Math.floor(Math.random() * nodeCount + 1)
        }));
    graph.settings.layout = layeredLayoutCheckBox.checked ? G.GSettings.sugiyamaLayout : G.GSettings.mdsLayout;

    var startTime = new Date();
    graph.createNodeBoundariesForSVGInContainer(graphView);
    graph.layoutCallbacks.add(() => {
        graphControl.drawGraph();
        setGUIToNotRunning();
        var endTime = new Date();
        var diff = endTime.getTime() - startTime.getTime();
        elapsed.textContent = diff + " msecs";
    });
    graph.beginLayoutGraph();
}

function startButtonClicked() {
    var nodeCount = parseInt(nodeCountControl.value);
    var edgeCount = parseInt(edgeCountControl.value);
    setGUIToRunning();
    run(nodeCount, edgeCount);
}

function stopButtonClicked() {
    graph.stopLayoutGraph();
    setGUIToNotRunning();
}

setGUIToNotRunning();

startButton.onclick = startButtonClicked;
stopButton.onclick = stopButtonClicked;