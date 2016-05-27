/// <amd-dependency path="idd"/>
import G = require('./ggraph');
import SVGGraph = require('./svggraph');
declare var InteractiveDataDisplay;

/** Renderer that targets an SVG plot inside IDD. Note that the MSAGL coordinate system has inverted Y-axis compared to IDD. */
class IDDSVGGraph extends SVGGraph {
    /** This is the declaration of the IDD plot for MSAGL. I got this from Sergey and subsequently modified it to have it handle some corner cases.
    This should no longer be necessary after we have SVGPlot as part of IDD. */
    private static msaglPlot = function (graph: IDDSVGGraph, jqDiv, master) {
        this.base = InteractiveDataDisplay.Plot;
        this.base(jqDiv, master);
        var that = this;

        var _svgCnt = undefined;
        var _svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");

        Object.defineProperty(this, "svg", {
            get: function () {
                return _svg;
            },
        });

        this.computeLocalBounds = function (step, computedBounds) {
            if (graph.graph == null)
                return undefined;
            return { x: graph.graph.boundingBox.x, y: (-graph.graph.boundingBox.y - graph.graph.boundingBox.height), width: graph.graph.boundingBox.width, height: graph.graph.boundingBox.height };
        }

        this.arrange = function (finalRect) {
            InteractiveDataDisplay.CanvasPlot.prototype.arrange.call(this, finalRect);

            if (_svgCnt === undefined) {
                _svgCnt = $("<div></div>").css("overflow", "hidden").appendTo(that.host)[0];
                //_svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
                _svg.setAttribute("preserveAspectRatio", "xMinYMin slice"); //xMinYMin
                _svgCnt.appendChild(_svg);
            }

            _svg.setAttribute("width", finalRect.width);
            _svg.setAttribute("height", finalRect.height);
            _svgCnt.setAttribute("width", finalRect.width);
            _svgCnt.setAttribute("height", finalRect.height);

            if (_svg !== undefined) {
                var plotRect = that.visibleRect;

                if (!isNaN(plotRect.y) && !isNaN(plotRect.height)) {
                    var zoom = finalRect.width / plotRect.width;
                    _svg.setAttribute("viewBox", plotRect.x + " " + (-plotRect.y - plotRect.height) + " " + plotRect.width + " " + plotRect.height);
                }
            }
        };
    };

    chart: any;
    gplot: any;

    /** Constructs an IDD SVG renderer and binds it to the provided HTML element.
     * @param container The container for the graph.
     * @param graph A GGraph to render. This is optional; you can set this later. */
    constructor(container: HTMLElement, graph?: G.GGraph) {
        super(container, graph);
        var that = this;

        // Ensure that the container has a unique ID. Note that I've tried using jQuery's uniqueId function here, and it does not work reliably. It can
        // assign an ID that is already in use.
        var chartID = container.id;
        var c = 1;
        if (chartID == "" || document.getElementById(chartID) != container)
            while (chartID == "" || document.getElementById(chartID) != null)
                chartID = "id_" + c++;
        container.setAttribute("id", chartID);

        // Create a container div for the plot. Maybe this is unnecessary and I can use the user-provided container directly. I should try.
        var plotContainerID = chartID + '-container';
        var plotID = chartID + '-plot';
        container.setAttribute('data-idd-plot', 'plot');
        // Check whether the container already exists. Maybe this is unnecessary and I can assume the container never exists.
        var containerDiv = document.getElementById(plotContainerID);
        if (containerDiv == undefined) {
            containerDiv = document.createElement('div');
            containerDiv.setAttribute('id', plotContainerID);
            containerDiv.style.width = '100%';
            containerDiv.style.height = '100%';
            containerDiv.style.minWidth = '100px';
            containerDiv.style.minHeight = '100px';
            containerDiv.setAttribute('data-idd-plot', plotID);
            container.appendChild(containerDiv);
        }

        // This looks like it can cause problems if there's more than one IDDSVGGraph on the page. But it should go away after we switch to IDD SVGPlot.
        IDDSVGGraph.msaglPlot.prototype = new InteractiveDataDisplay.Plot;
        InteractiveDataDisplay.register(plotID, function (jqDiv, master) { return new IDDSVGGraph.msaglPlot(that, jqDiv, master); });
        this.chart = InteractiveDataDisplay.asPlot(chartID);
        this.chart.aspectRatio = 1;

        var gestureSource = InteractiveDataDisplay.Gestures.getGesturesStream($("#" + chartID));
        this.chart.navigation.gestureSource = gestureSource;
        this.gplot = this.chart.get(plotContainerID);

        this.containerRect = container.getBoundingClientRect();
        if ((<any>container).msagl_check_size_interval != null)
            clearInterval((<any>container).msagl_check_size_interval);
        (<any>container).msagl_check_size_interval = setInterval(() => that.checkSizeChanged(), 300);
    }

    private containerRect: ClientRect;
    private checkSizeChanged() {
        var rect = this.container.getBoundingClientRect();
        for (var i in rect)
            if (rect[i] != this.containerRect[i]) {
                InteractiveDataDisplay.updateLayouts($(this.container));
                break;
            }
        this.containerRect = rect;
    }

    /** Returns a GRect with the portion of the graph that's currently being displayed. */
    getViewBox(): G.GRect {
        var vb: string = this.gplot.svg.getAttribute("viewBox");
        if (vb == null || vb == "")
            return null;
        var tokens = vb.split(' ');
        var x = parseFloat(tokens[0]);
        var y = parseFloat(tokens[1]);
        var width = parseFloat(tokens[2]);
        var height = parseFloat(tokens[3]);

        return new G.GRect({ x: x, y: y, width: width, height: height });
    }

    /** Sets the portion of the graph to be displayed. */
    setViewBox(box: G.IRect) {
        var x = box.x;
        var y = box.y;
        var width = box.width;
        var height = box.height;
        this.chart.navigation.setVisibleRect({ x: x, y: -y - height, width: width, height: height }, false);
    }

    /** Clears the previous elements from the SVG and populates it with elements from the current graph. */
    private redrawGraph(): boolean {
        this.svg = this.gplot.svg;
        if (this.svg === undefined)
            return false;
        while (this.svg.childNodes.length > 0)
            this.svg.removeChild(this.svg.childNodes[0]);
        super.populateGraph();
        if (this.graph == null)
            return false;
        return true;
    }

    /** Renders the graph. Note that this is an override of SVGGraph's drawGraph (which would attempt to set the SVG viewBox directly, while here
    I should do it through IDD). */
    drawGraph(): void {
        // Populate the graph.
        if (!this.redrawGraph())
            return;
        // Set a reasonable initial position for the graph view window.
        var bbox: G.GRect = this.graph.boundingBox;
        var offsetX = bbox.x;
        var offsetY = bbox.y;
        var cwidth = parseFloat(this.svg.getAttribute('width'));
        var cheight = parseFloat(this.svg.getAttribute('height'));
        var scaleX = cwidth / bbox.width;
        var scaleY = cheight / bbox.height;
        var scale = Math.min(scaleX, scaleY);
        this.chart.navigation.setVisibleRect({ x: offsetX, y: -offsetY - (isNaN(scale) ? bbox.height : (cheight / scale)), width: bbox.width, height: bbox.height }, false);
    }
}

export = IDDSVGGraph