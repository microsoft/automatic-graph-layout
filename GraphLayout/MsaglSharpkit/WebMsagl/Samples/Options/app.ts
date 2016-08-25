/// <reference path="../../Scripts/typings/requirejs/require.d.ts"/>
import G = require("../../Scripts/src/ggraph");
import IDDSVGGraph = require("../../Scripts/src/iddsvggraph");

var graphView = document.getElementById("graphView");
var graphControl = new IDDSVGGraph(graphView);

var layeredLayoutCheckBox = <HTMLInputElement>document.getElementById("layeredLayoutCheckBox");
var horizontalLayoutCheckBox = <HTMLInputElement>document.getElementById("horizontalLayoutCheckBox");
var aspectRatioTextBox = <HTMLInputElement>document.getElementById("aspectRatioTextBox");
var edgeRoutingSelect = <HTMLSelectElement>document.getElementById("edgeRoutingSelect");
var workingIndicator = <HTMLDivElement>document.getElementById("workingIndicator");

var jsonGraph = "";

function stop() {
    if (graphControl.graph != null)
        graphControl.graph.stopLayoutGraph();
    workingIndicator.style.display = "none";
    graphView.style.display = "";
}

function layoutClicked() {
    stop();

    graphControl.graph = G.GGraph.ofJSON(jsonGraph);

    graphControl.graph.settings.layout = layeredLayoutCheckBox.checked ? G.GSettings.sugiyamaLayout : G.GSettings.mdsLayout;
    graphControl.graph.settings.transformation = horizontalLayoutCheckBox.checked ? G.GPlaneTransformation.ninetyDegreesTransformation : G.GPlaneTransformation.defaultTransformation;
    if (aspectRatioTextBox.value != null && aspectRatioTextBox.value != "")
        graphControl.graph.settings.aspectRatio = parseFloat(aspectRatioTextBox.value);
    graphControl.graph.settings.routing = edgeRoutingSelect.value;

    graphControl.graph.createNodeBoundariesForSVGInContainer(graphView);

    workingIndicator.style.display = "inherit";
    graphView.style.display = "none";
    graphControl.graph.beginLayoutGraph(() => {
        workingIndicator.style.display = "none";
        graphView.style.display = "";
        graphControl.drawGraph();
    });
}

function stopClicked() {
    stop();
}

function loadGraph1() {
    require(["text!Samples/Options/samplegraph1.json"], (sample) => {
        jsonGraph = sample;
        layoutClicked();
    });
}

function loadGraph2() {
    require(["text!Samples/Options/samplegraph2.json"], (sample) => {
        jsonGraph = sample;
        layoutClicked();
    });
}

document.getElementById("layoutButton").onclick = layoutClicked;
document.getElementById("stopButton").onclick = stopClicked;
document.getElementById("loadGraph1Button").onclick = loadGraph1;
document.getElementById("loadGraph2Button").onclick = loadGraph2;

loadGraph1();