/// <reference path="../../Scripts/typings/requirejs/require.d.ts"/>
import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);

function parseJsonClicked() {
    var jsonTextArea = document.getElementById("jsonInput");
    var jsonText = jsonTextArea.textContent;
    graphControl.graph = G.GGraph.ofJSON(jsonText);
    graphControl.graph.createNodeBoundariesForSVGInContainer(graphView);
    graphControl.graph.layoutCallback = () => {
        var jsonOutputArea = document.getElementById("jsonOutput");
        var graphText = graphControl.graph.getJSON();
        jsonOutputArea.textContent = graphText;
        graphControl.drawGraph();
    };
    graphControl.graph.beginLayoutGraph();
}

document.getElementById("parseButton").onclick = parseJsonClicked;

require(["text!Samples/GraphFromJson/samplegraph.json"], (sample) => {
    document.getElementById("jsonInput").textContent = sample;
});