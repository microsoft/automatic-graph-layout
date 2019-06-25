/// <reference path="../../Scripts/typings/requirejs/require.d.ts"/>
import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);
var graph: G.GGraph = null;

function draw() {
    var jsonOutputArea = document.getElementById("jsonOutput");
    var graphText = graph.getJSON();
    jsonOutputArea.textContent = graphText;
    graphControl.drawGraph();
}

function parse() {
    var jsonTextArea = <HTMLTextAreaElement>document.getElementById("jsonInput");
    var jsonText = jsonTextArea.value;
    graph = G.GGraph.ofJSON(jsonText);
    graphControl.setGraph(graph);
    graph.createNodeBoundariesForSVGInContainer(graphView);
    graph.layoutCallbacks.add(draw);
    graph.edgeRoutingCallbacks.add(draw);
}

function loadFromOutput() {
    var jsonOutputArea = <HTMLTextAreaElement>document.getElementById("jsonOutput");
    var jsonText = jsonOutputArea.value;
    graph = G.GGraph.ofJSON(jsonText);
    graphControl.setGraph(graph);
}

function parseJsonClicked() {
    parse();
    graph.beginLayoutGraph();
}

function loadClicked() {
    parse();
    draw();
}

function routeEdgesClicked() {
    graph.beginEdgeRouting();
}

function renderClicked() {
    loadFromOutput();
    graphControl.drawGraph();
}

document.getElementById("parseButton").onclick = parseJsonClicked;
document.getElementById("loadButton").onclick = loadClicked;
document.getElementById("routeButton").onclick = routeEdgesClicked;
document.getElementById("renderButton").onclick = renderClicked;

require(["text!Samples/GraphFromJson/samplegraph.json"], (sample: string) => {
    document.getElementById("jsonInput").textContent = sample;
});