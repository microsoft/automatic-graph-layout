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

function showWorking(show: boolean) {
    if (show) {
        workingIndicator.style.display = "inherit";
        graphView.style.display = "none";
    }
    else {
        workingIndicator.style.display = "none";
        graphView.style.display = "";
    }
}

function stop() {
    if (graphControl.graph != null && graphControl.graph.working)
        graphControl.graph.stopLayoutGraph();
    showWorking(false);
}

function copySettingsToGraph() {
    graphControl.graph.settings.layout = layeredLayoutCheckBox.checked ? G.GSettings.sugiyamaLayout : G.GSettings.mdsLayout;
    graphControl.graph.settings.transformation = horizontalLayoutCheckBox.checked ? G.GPlaneTransformation.ninetyDegreesTransformation : G.GPlaneTransformation.defaultTransformation;
    if (aspectRatioTextBox.value != null && aspectRatioTextBox.value != "")
        graphControl.graph.settings.aspectRatio = parseFloat(aspectRatioTextBox.value);
    graphControl.graph.settings.routing = edgeRoutingSelect.value;
}

function layoutClicked() {
    stop();
    copySettingsToGraph();
    graphControl.graph.createNodeBoundariesForSVGInContainer(graphView);
    showWorking(true);
    graphControl.graph.beginLayoutGraph();
}

function routeClicked() {
    stop();
    copySettingsToGraph();
    showWorking(true);
    graphControl.graph.beginEdgeRouting();
}

function stopClicked() {
    stop();
}

function loadGraph(json: string) {
    jsonGraph = json;
    graphControl.graph = G.GGraph.ofJSON(jsonGraph);
    graphControl.graph.layoutCallback = () => {
        showWorking(false);
        graphControl.drawGraph();
    };
    graphControl.graph.edgeRoutingCallback = () => {
        showWorking(false);
        graphControl.drawGraph();
    };
    layoutClicked();
}

function loadGraph1() {
    require(["text!Samples/Options/samplegraph1.json"], (sample) => {
        loadGraph(sample);
    });
}

function loadGraph2() {
    require(["text!Samples/Options/samplegraph2.json"], (sample) => {
        loadGraph(sample);
    });
}

document.getElementById("layoutButton").onclick = layoutClicked;
document.getElementById("routeButton").onclick = routeClicked;
document.getElementById("stopButton").onclick = stopClicked;
document.getElementById("loadGraph1Button").onclick = loadGraph1;
document.getElementById("loadGraph2Button").onclick = loadGraph2;

loadGraph1();