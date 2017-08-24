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
    var jsonTextArea = document.getElementById("jsonInput");
    var jsonText = jsonTextArea.textContent;
    graph = G.GGraph.ofJSON(jsonText);
    graphControl.setGraph(graph);
    graph.createNodeBoundariesForSVGInContainer(graphView);
    graph.layoutCallbacks.add(draw);
    graph.edgeRoutingCallbacks.add(draw);
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
    parse();
    graph.beginEdgeRouting();
}

document.getElementById("parseButton").onclick = parseJsonClicked;
document.getElementById("loadButton").onclick = loadClicked;
document.getElementById("routeButton").onclick = routeEdgesClicked;

require(["text!Samples/GraphFromJson/samplegraph.json"], (sample: string) => {
    document.getElementById("jsonInput").textContent = sample;
});