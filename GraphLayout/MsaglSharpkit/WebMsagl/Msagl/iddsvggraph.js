var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", './svggraph'], function (require, exports, SVGG) {
    var IDDSVGGraph = (function (_super) {
        __extends(IDDSVGGraph, _super);
        function IDDSVGGraph(chartID, graph) {
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
                return new IDDSVGGraph.msaglPlot(jqDiv, master);
            });
            this.chart = InteractiveDataDisplay.asPlot(chartID);
            this.chart.aspectRatio = 1;
            var gestureSource = InteractiveDataDisplay.Gestures.getGesturesStream($("#" + chartID));
            this.chart.navigation.gestureSource = gestureSource;
            this.gplot = this.chart.get(plotContainerID);
        }
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
            //var scale = Math.min(this.container.offsetWidth / bbox.width, this.container.offsetHeight / bbox.height);
            var scale = Math.max(parseFloat(this.svg.getAttribute('width')) / bbox.width, parseFloat(this.svg.getAttribute('height')) / bbox.height);
            scale = Math.max(0.5, scale);
            scale = Math.min(2.0, scale);
            var width = bbox.width * scale;
            var height = bbox.height * scale;
            if (this.chart.host.width() <= 1 || this.chart.host.height() <= 1) {
                // Handle case when the host is not currently displayed (e.g. in a tab).
                this.chart.host.width(width);
                this.chart.host.height(height);
            }
            this.chart.navigation.setVisibleRect({ x: offsetX, y: -height - offsetY, width: width, height: height }, false);
            var viewBox = "" + offsetX + " " + offsetY + " " + width + " " + height;
            this.svg.setAttribute("viewBox", viewBox);
        };
        IDDSVGGraph.msaglPlot = function (jqDiv, master) {
            this.base = InteractiveDataDisplay.Plot;
            this.base(jqDiv, master);
            var that = this;
            var _svgCnt = undefined;
            var _svg = undefined;
            Object.defineProperty(this, "svg", {
                get: function () {
                    return _svg;
                },
            });
            this.arrange = function (finalRect) {
                InteractiveDataDisplay.CanvasPlot.prototype.arrange.call(this, finalRect);
                if (_svgCnt === undefined) {
                    _svgCnt = $("<div></div>").css("overflow", "hidden").appendTo(that.host)[0];
                    _svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
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
                        _svg.setAttribute("viewBox", plotRect.x + " " + (-plotRect.y - plotRect.height) + " " + plotRect.width + " " + plotRect.height);
                    }
                }
            };
        };
        return IDDSVGGraph;
    })(SVGG.SVGGraph);
    exports.IDDSVGGraph = IDDSVGGraph;
});
//# sourceMappingURL=iddsvggraph.js.map