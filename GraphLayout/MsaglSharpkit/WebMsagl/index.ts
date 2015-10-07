/// <amd-dependancy path="ggraph"/>
/// <amd-dependancy path="cgraph"/>
/// <amd-dependancy path="iddgraph"/>
/// <amd-dependancy path="htmlsvggraph"/>
/// <amd-dependancy path="iddsvggraph"/>
/// <amd-dependancy path="samples"/>

// Declare the various types of graph rendering layers that I'm going to use. It's important to note that
// I'm using multiple rendering layers for the same geometry graph; the two layers are independant. I am
// using one geometry graph for the Canvas and IDD renderings, and another geometry graph for the SVG and
// IDD SVG renderings. The reason I'm not using just one geometry graph is that Canvas and SVG render text
// a bit differently, which may require labels of different size.
var cgraph: CGraph;
var iddgraph: IDDGraph;
var svggraph: HTMLSVGGraph;
var iddsvggraph: IDDSVGGraph;

// This is the callback function for Canvas graphs. It will be called when the layout for the geometry
// graph that I want to use for Canvas is completed. In this function, I will use the Canvas and IDD
// rendering layers to display the laid out graph on the page. This is as simple as setting the graph
// property of the CGraph or IDDGraph and calling drawGraph(). The rest of the code in this function is
// not relevant to the rendering.
function callbackCGraph(ggraph) {
    var updateCanvas = (<HTMLInputElement>$("#updateCanvas")[0]).checked;
    var updateIDD = (<HTMLInputElement>$("#updateCanvas")[0]).checked;

    $("#messages").append("<br>layout (context) done");

    var json = ggraph.getJSON();
    $("#graphContextJsonTextOut").text(json);

    if (updateCanvas) {
        // Put the laid-out graph in the rendering graph.
        cgraph.graph = ggraph;
        var tstart = new Date().getTime();
        // Render.
        cgraph.drawGraph();
        var tend = new Date().getTime();
        var tdiff = tend - tstart;
        $("#canvasGraphFooter").text("drawGraph took " + tdiff + " msecs");
    }

    if (updateIDD) {
        iddgraph.graph = ggraph;
        var tstart = new Date().getTime();
        iddgraph.drawGraph();
        var tend = new Date().getTime();
        var tdiff = tend - tstart;
        $("#iddGraphFooter").text("drawGraph took " + tdiff + " msecs");
    }
}

// Same as above, for SVG and IDD SVG.
function callbackSVGGraph(ggraph) {
    var updateSVG = (<HTMLInputElement>$("#updateSVG")[0]).checked;
    var updateIDDSVG = (<HTMLInputElement>$("#updateIDDSVG")[0]).checked;

    $("#messages").append("<br>layout (SVG) done");

    var json = ggraph.getJSON();
    $("#graphSVGJsonTextOut").text(json);

    if (updateSVG) {
        svggraph.graph = ggraph;
        var tstart = new Date().getTime();
        svggraph.drawGraph();
        var tend = new Date().getTime();
        var tdiff = tend - tstart;
        $("#svgGraphFooter").text("drawGraph took " + tdiff + " msecs");
    }

    if (updateIDDSVG) {
        iddsvggraph.graph = ggraph;
        var tstart = new Date().getTime();
        iddsvggraph.drawGraph();
        var tend = new Date().getTime();
        var tdiff = tend - tstart;
        $("#iddsvgGraphFooter").text("drawGraph took " + tdiff + " msecs");
    }
}

// This is a custom label rendering function for Canvas renderings. This is only interesting if you want to have
// labels that are not just text. The function gets the rendering context, the center coordinates of the label,
// and the label object. It should return true after rendering the label, or return false if you want to defer to
// the default rendering (i.e. text). Note that if you use custom labels, you must make sure that the label size
// is set before starting the layout; the engine cannot infer the size of a custom label.
// In this case, if the label content is a single asterisk, then I'm drawing a filled circle with a radius of 20.
function customDrawLabelCanvas(context: CanvasRenderingContext2D, label: GLabel): boolean {
    if (label.content != "*")
        return false;
    context.beginPath();
    context.arc(label.bounds.x + label.bounds.width / 2, label.bounds.y + label.bounds.height / 2, 20, 0, Math.PI * 2, true);
    context.closePath();
    context.fill();
    return true;
}

// Same as above, for SVG. In this case, I'm using an SVG picture as the node content.
function customDrawLabelSVG(svg: any, parent: Element, label: GLabel): boolean {
    if (label.content != "*")
        return false;
    var translate = "translate(" + (label.bounds.x) + "," + (label.bounds.y) + ")";
    var g = document.createElementNS("http://www.w3.org/2000/svg", "g");
    g.setAttribute("transform", translate);

    var txt = customSVG;
    var content: Document = new DOMParser().parseFromString(txt, 'image/svg+xml');

    function copyTree(source: any, dest: Element) {
        for (var i = 0; i < source.childNodes.length; i++) {
            var child = <Element>source.childNodes[i];
            if (child.tagName === undefined) {
                var tel = document.createTextNode(child.textContent);
                dest.appendChild(tel);
            }
            else {
                var el = document.createElementNS("http://www.w3.org/2000/svg", child.tagName);
                for (var j = 0; j < child.attributes.length; j++)
                    el.setAttribute(child.attributes[j].name, child.attributes[j].value);
                copyTree(child, el);
                dest.appendChild(el);
            }
        }
    }
    copyTree(content, g);

    parent.appendChild(g);
    return true;
}

export function init() {
    $("#messages").text("");

    // Create and setup the various rendering graphs. Note that if you only have text labels, you don't
    // need to set the customDrawLabel property.
    cgraph = new CGraph('graphCanvas');
    cgraph.customDrawLabel = customDrawLabelCanvas;
    iddgraph = new IDDGraph('iddchart');
    iddgraph.customDrawLabel = customDrawLabelCanvas;
    svggraph = new HTMLSVGGraph('graphSvg');
    svggraph.customDrawLabel = customDrawLabelSVG;
    iddsvggraph = new IDDSVGGraph('iddsvgchart');
    iddsvggraph.customDrawLabel = customDrawLabelSVG;

    // Load the "four nodes" example in the text box.
    onLoadFourNodes();

    // Launch an update of the graphs.
    performUpdate();

    updateDOMText();
}

// This function will retrieve the JSON-specified graph from the text box, turn it into a geometry graph,
// and start the layout. This boils down to calling GGraph.ofJSON, then one of the
// GGraph.createNodeBoundaries functions, and then GGraph.beginLayoutGraph.
function performUpdate() {
    var updateCanvas = (<HTMLInputElement>$("#updateCanvas")[0]).checked;
    var updateIDD = (<HTMLInputElement>$("#updateIDD")[0]).checked;
    var updateSVG = (<HTMLInputElement>$("#updateSVG")[0]).checked;
    var updateIDDSVG = (<HTMLInputElement>$("#updateIDDSVG")[0]).checked;

    var json = $("#graphJsonTextIn").text();

    if (updateCanvas || updateIDD) {
        // Get a GGraph from the JSON string.
        var ggraphContext = GGraph.ofJSON(json);
        // Call createNodeBoundariesFromContext. This will make sure that every node has a proper boundary
        // curve. If any node does not have a boundary curve, layout will fail. Note that if you are
        // explicitly setting the boundary curve for every single node, you do not need to call this
        // function at all.
        ggraphContext.createNodeBoundariesFromContext();

        // Initiate the layout. The layout is an asynchronous operation, so you must provide a callback.
        // This function will return immediately, and the callback will be invoked at some later point.
        // When the callback is invoked, the graph will have been laid out (which means that all of the
        // nodes will have a valid position, and all of the edges will have a valid curve).
        ggraphContext.beginLayoutGraph(function () { callbackCGraph(ggraphContext) });
        $("#messages").append("<br>layout (context) started");
    }

    // This block does the same as the above, for SVG.
    if (updateSVG || updateIDDSVG) {
        var ggraphSVG = GGraph.ofJSON(json);
        ggraphSVG.createNodeBoundariesFromSVG();

        ggraphSVG.beginLayoutGraph(function () { callbackSVGGraph(ggraphSVG) });
        $("#messages").append("<br>layout (SVG) started");
    }
}

export function onLoadProgrammatically() {
    $("#graphJsonTextIn").text(txt_programmatically);
}

export function onLoadFourNodes() {
    $("#graphJsonTextIn").text(txt_fourNodes);
}

export function onLoadManyNodes() {
    var nodes = $("#randomGraphNodes").val();
    var edges = $("#randomGraphEdges").val();
    var custom = (<HTMLInputElement>$("#randomGraphCustom")[0]).checked;
    var txt = txt_manyNodes(nodes, edges, custom ? 0.2 : 0.0);
    $("#graphJsonTextIn").text(txt);
}

export function onUpdateClicked() {
    performUpdate();
}

function updateDOMText() {
    $("#domText").text("");
    var txt = document.documentElement.innerHTML;
    $("#domText").text(txt);
}

var g = null;
var fc = null;

export function onUpdateDOMTextClicked() {
    updateDOMText();
}