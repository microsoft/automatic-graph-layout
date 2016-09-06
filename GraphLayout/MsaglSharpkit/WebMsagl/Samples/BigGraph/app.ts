import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);

var nodeCountControl = <HTMLInputElement>document.getElementById("nodeCount");
var edgeCountControl = <HTMLInputElement>document.getElementById("edgeCount");
var startButton = <HTMLButtonElement>document.getElementById("startButton");
var stopButton = <HTMLButtonElement>document.getElementById("stopButton");
var working = document.getElementById("working");
var elapsed = document.getElementById("elapsed");

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
    graphControl.graph = new G.GGraph();
    graphControl.graph.settings.aspectRatio = graphView.offsetWidth / graphView.offsetHeight;
    for (var i = 1; i <= nodeCount; i++)
        graphControl.graph.addNode(new G.GNode({ id: "node" + i, label: "Node " + i }));
    for (var i = 1; i <= edgeCount; i++)
        graphControl.graph.addEdge(new G.GEdge({
            id: "edge" + i, source: "node" + Math.floor(Math.random() * nodeCount + 1),
            target: "node" + Math.floor(Math.random() * nodeCount + 1)
        }));

    var startTime = new Date();
    graphControl.graph.createNodeBoundariesForSVGInContainer(graphView);
    graphControl.graph.layoutCallback = () => {
        graphControl.drawGraph();
        setGUIToNotRunning();
        var endTime = new Date();
        var diff = endTime.getTime() - startTime.getTime();
        elapsed.textContent = diff + " msecs";
    }
    graphControl.graph.beginLayoutGraph();
}

function startButtonClicked() {
    var nodeCount = parseInt(nodeCountControl.value);
    var edgeCount = parseInt(edgeCountControl.value);
    setGUIToRunning();
    run(nodeCount, edgeCount);
}

function stopButtonClicked() {
    graphControl.graph.stopLayoutGraph();
    setGUIToNotRunning();
}

setGUIToNotRunning();

startButton.onclick = startButtonClicked;
stopButton.onclick = stopButtonClicked;