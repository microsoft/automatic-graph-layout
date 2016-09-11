/// <reference path="../../Scripts/typings/requirejs/require.d.ts"/>
import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);
var graph: G.GGraph = null;

function parseJsonClicked() {
    var jsonTextArea = document.getElementById("jsonInput");
    var jsonText = jsonTextArea.textContent;
    graph = G.GGraph.ofJSON(jsonText);
    graphControl.setGraph(graph);
    graph.createNodeBoundariesForSVGInContainer(graphView);
    graph.layoutCallbacks.add(() => {
        var jsonOutputArea = document.getElementById("jsonOutput");
        var graphText = graph.getJSON();
        jsonOutputArea.textContent = graphText;
        graphControl.drawGraph();
    });
    graph.beginLayoutGraph();
}

document.getElementById("parseButton").onclick = parseJsonClicked;

require(["text!Samples/GraphFromJson/samplegraph.json"], (sample) => {
    document.getElementById("jsonInput").textContent = sample;
});