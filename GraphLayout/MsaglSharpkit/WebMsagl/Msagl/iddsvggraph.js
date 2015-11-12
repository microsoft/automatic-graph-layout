var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var IDDSVGGraph = (function (_super) {
    __extends(IDDSVGGraph, _super);
    function IDDSVGGraph(chartID, graph) {
        var that = this;
        var plotContainerID = chartID + '-container';
        var plotID = chartID + '-plot';
        var container = document.getElementById(chartID);
        container.setAttribute('data-idd-plot', 'plot');
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
        _super.call(this, chartID + '-container', graph);
        IDDSVGGraph.msaglPlot.prototype = new InteractiveDataDisplay.Plot;
        InteractiveDataDisplay.register(plotID, function (jqDiv, master) {
            return new IDDSVGGraph.msaglPlot(that, jqDiv, master);
        });
        this.chart = InteractiveDataDisplay.asPlot(chartID);
        this.chart.aspectRatio = 1;
        var gestureSource = InteractiveDataDisplay.Gestures.getGesturesStream($("#" + chartID));
        this.chart.navigation.gestureSource = gestureSource;
        this.gplot = this.chart.get(plotContainerID);
        //$(container).resize(function (e) { that.onSVGResized(e); });
    }
    IDDSVGGraph.prototype.onSVGResized = function (e) {
        alert(e);
    };
    IDDSVGGraph.prototype.getViewBox = function () {
        var vb = this.gplot.svg.getAttribute("viewBox");
        if (vb == null || vb == "")
            return null;
        var tokens = vb.split(' ');
        var x = parseFloat(tokens[0]);
        var y = parseFloat(tokens[1]);
        var width = parseFloat(tokens[2]);
        var height = parseFloat(tokens[3]);
        return new GRect({ x: x, y: y, width: width, height: height });
    };
    IDDSVGGraph.prototype.setViewBox = function (box) {
        var x = box.x;
        var y = box.y;
        var width = box.width;
        var height = box.height;
        this.chart.navigation.setVisibleRect({ x: x, y: -y - height, width: width, height: height }, false);
    };
    IDDSVGGraph.prototype.redrawGraph = function () {
        this.svg = this.gplot.svg;
        if (this.svg === undefined)
            return false;
        while (this.svg.childNodes.length > 0)
            this.svg.removeChild(this.svg.childNodes[0]);
        _super.prototype.populateGraph.call(this);
        if (this.graph == null)
            return false;
        return true;
    };
    IDDSVGGraph.prototype.drawGraph = function () {
        if (!this.redrawGraph())
            return;
        var bbox = this.graph.boundingBox;
        var offsetX = bbox.x;
        var offsetY = bbox.y;
        var cwidth = parseFloat(this.svg.getAttribute('width'));
        var cheight = parseFloat(this.svg.getAttribute('height'));
        var scaleX = Math.max(0.5, Math.min(2.0, cwidth / bbox.width));
        var scaleY = Math.max(0.5, Math.min(2.0, cheight / bbox.height));
        var scale = Math.min(scaleX, scaleY);
        if (isNaN(scale))
            scale = 1;
        var width = bbox.width * scale;
        var height = bbox.height * scale;
        this.chart.navigation.setVisibleRect({ x: offsetX, y: -offsetY - bbox.height, width: bbox.width, height: bbox.height }, false);
        this.chart.navigation.setVisibleRect({ x: offsetX, y: -offsetY - (cheight / scale), width: cwidth / scale, height: cheight / scale }, false);
        var viewBox = "" + offsetX + " " + offsetY + " " + width + " " + height;
        this.svg.setAttribute("viewBox", viewBox);
    };
    IDDSVGGraph.msaglPlot = function (graph, jqDiv, master) {
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
        };
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
    return IDDSVGGraph;
})(SVGGraph);
//# sourceMappingURL=iddsvggraph.js.map