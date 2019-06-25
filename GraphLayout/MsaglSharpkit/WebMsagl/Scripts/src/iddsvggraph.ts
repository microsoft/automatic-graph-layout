/// <amd-dependency path="idd"/>
import G = require('./ggraph');
import SVGGraph = require('./svggraph');
var InteractiveDataDisplay = require('idd');

/** Renderer that targets an SVG plot inside IDD. Note that the MSAGL coordinate system has inverted Y-axis compared to IDD. */
class IDDSVGGraph extends SVGGraph {
    /** This is the declaration of the IDD plot for MSAGL. I got this from Sergey and subsequently modified it to have it handle some corner cases.
    This should no longer be necessary after we have SVGPlot as part of IDD. */
    private static msaglPlot: any = function (this: any, graph: IDDSVGGraph, jqDiv: any, master: any) {
        this.base = InteractiveDataDisplay.Plot;
        this.base(jqDiv, master);
        var that = this;

        var _svgCnt: HTMLElement = undefined;
        var _svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");

        Object.defineProperty(this, "svg", {
            get: function () {
                return _svg;
            },
        });

        this.computeLocalBounds = function (step: any, computedBounds: any) {
            if (graph.graph == null)
                return undefined;
            return { x: graph.graph.boundingBox.x, y: (-graph.graph.boundingBox.y - graph.graph.boundingBox.height), width: graph.graph.boundingBox.width, height: graph.graph.boundingBox.height };
        }

        this.arrange = function (this: any, finalRect: any) {
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
                    if (plotRect.width > 0 && plotRect.height > 0)
                        _svg.setAttribute("viewBox", plotRect.x + " " + (-plotRect.y - plotRect.height) + " " + plotRect.width + " " + plotRect.height);
                }
            }
        };
    };

    chart: any;
    gplot: any;

    /** The scale wich is considered to have zoom level equal to 1.0 */
    private referenceScale: number = 1.0;

    /** Is called each time the zoom level is changed by the IDD navigation */
    public zoomLevelChangeCallback: (level: number) => void = function (level) { };

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
        InteractiveDataDisplay.register(plotID, function (jqDiv: any, master: any) { return new IDDSVGGraph.msaglPlot(that, jqDiv, master); });
        this.chart = InteractiveDataDisplay.asPlot(chartID);
        this.chart.aspectRatio = 1;

        var gestureSource = InteractiveDataDisplay.Gestures.getGesturesStream($("#" + chartID));
        this.chart.navigation.gestureSource = gestureSource;
        this.gplot = this.chart.get(plotContainerID);

        // passing the event from IDD to the user, prforming data conversion
        this.chart.master.host.on('widthScaleChanged', (event: any, data: any) => {
            var newWidthScale = data['widthScale'];
            var zoomLevel = newWidthScale / that.referenceScale;
            that.zoomLevelChangeCallback(zoomLevel);
        });

        this.containerRect = container.getBoundingClientRect();
        if ((<any>container).msagl_check_size_interval != null)
            clearInterval((<any>container).msagl_check_size_interval);
        (<any>container).msagl_check_size_interval = setInterval(() => that.checkSizeChanged(), 300);
    }

    private containerRect: ClientRect;
    private checkSizeChanged() {
        var rect = this.container.getBoundingClientRect();
        for (var i in rect)
            if ((<any>rect)[i] != (<any>this.containerRect)[i]) {
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

    /** The IDD gesture source. Used to disable and restore IDD mouse handling. */
    private gestureSource: any = undefined;
    /** Disables IDD mouse handling, and stores its required reference for later use. */
    private disableIDDMouseHandling() {
        this.gestureSource = this.chart.navigation.gestureSource;
        this.chart.navigation.gestureSource = undefined;
    }
    /** Restores IDD mouse handling, if it was disabled. */
    private restoreIDDMouseHandling() {
        if (this.gestureSource != null)
            this.chart.navigation.gestureSource = this.gestureSource;
    }

    /**
     *  Recalibrates the zoom level. After this call the current view of the graph will be considered as zoom level 1.0     
     */
    public resetZoomLevel() {
        this.referenceScale = this.chart.navigation.widthScale;
    }

    /**
     * Returns the current zoom level value.
     */
    public getZoomLevel() {
        return this.chart.navigation.widthScale / this.referenceScale;
    }

    /**
     * Changes the zoom level of the graph. Value of 1.0 indicates 100%; 1.5 - 150%; 0.1 - 10% of zoom
     * @param zoomLevel new level to set
     */
    public setZoomLevel(zoomLevel: number) {
        this.chart.navigation.widthScale = this.referenceScale * zoomLevel;
    }

    /** Prepares to take charge of mouse handling when the user is about to start editing the graph. */
    hookUpMouseEvents() {
        // Invoke the super. It will hook up mouse events.
        super.hookUpMouseEvents();
        // Prepare to disable IDD handling when editing.
        var that = this;
        this.container.onmousedown = function (e) {
            // If we're in edit mode and there is an object under the mouse cursor, disable IDD handling.
            if (that.allowEditing && that.getObjectUnderMouseCursor() != null)
                that.disableIDDMouseHandling();
            // Pass the event to the super.
            that.onMouseDown(e);
        };
        this.container.onmouseup = function (e) {
            // Pass the event to the super.
            that.onMouseUp(e);
            // If we're in edit mode, make sure that IDD regains mouse control.
            if (that.allowEditing)
                that.restoreIDDMouseHandling();
        }
    }

    protected onMouseDblClick(e: MouseEvent) {
        if (this.svg != null && !this.isEditingEdge())
            this.setViewBox(this.graph.boundingBox);
        else
            super.onMouseDblClick(e);
    }
}

export = IDDSVGGraph