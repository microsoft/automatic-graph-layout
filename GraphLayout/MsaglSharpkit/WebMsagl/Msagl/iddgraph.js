var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", './contextgraph'], function (require, exports, CoG) {
    /// <amd-dependency path="idd"/>
    var InteractiveDataDisplay = require('idd');
    // Renderer that targets a Canvas inside IDD.
    var IDDGraph = (function (_super) {
        __extends(IDDGraph, _super);
        function IDDGraph(chartID, graph) {
            _super.call(this);
            this.grid = false;
            var plotContainerID = chartID + '-container';
            var plotID = chartID + '-plot';
            var container = document.getElementById(chartID);
            container.setAttribute('data-idd-plot', 'plot');
            var containerDiv = document.createElement('div');
            containerDiv.setAttribute('id', plotContainerID);
            containerDiv.setAttribute('data-idd-plot', plotID);
            container.appendChild(containerDiv);
            IDDGraph.msaglPlot.prototype = new InteractiveDataDisplay.CanvasPlot;
            this.graph = graph === undefined ? null : graph;
            InteractiveDataDisplay.register(plotID, function (jqDiv, master) {
                return new IDDGraph.msaglPlot(jqDiv, master);
            });
            var chart = InteractiveDataDisplay.asPlot(chartID);
            var gestureSource = InteractiveDataDisplay.Gestures.getGesturesStream($("#" + chartID));
            chart.navigation.gestureSource = gestureSource;
            container.ondblclick = function (ev) {
                chart.fitToView();
            };
            this.gplot = chart.get(plotContainerID);
        }
        IDDGraph.prototype.drawGraph = function () {
            this.gplot.setGraph(this);
            this.gplot.invalidateLocalBounds();
            this.gplot.requestNextFrameOrUpdate();
        };
        IDDGraph.prototype.drawGraphFromPlot = function (context) {
            if (this.grid)
                this.drawGrid(context);
            this.drawGraphInternal(context, this.graph);
        };
        IDDGraph.msaglPlot = function (jqDiv, master) {
            this.base = InteractiveDataDisplay.CanvasPlot;
            this.base(jqDiv, master);
            this.aspectRatio = 1.0;
            var _graph;
            this.renderCore = function (plotRect, screenSize) {
                if (_graph && plotRect.width > 0 && plotRect.height > 0) {
                    var bbox = _graph.graph.boundingBox;
                    var context = this.getContext(true);
                    context.save();
                    var t = this.coordinateTransform;
                    var offset = t.getOffset();
                    var scale = t.getScale();
                    context.translate(offset.x, offset.y);
                    context.scale(scale.x, scale.y);
                    context.translate(-bbox.x, -bbox.height - bbox.y);
                    _graph.drawGraphFromPlot(context);
                    context.restore();
                }
            };
            this.setGraph = function (g) {
                _graph = g;
            };
            this.computeLocalBounds = function (step, computedBounds) {
                return _graph ? { x: 0, y: 0, width: _graph.graph.boundingBox.width, height: _graph.graph.boundingBox.height } : { x: 0, y: 0, width: 1, height: 1 };
            };
        };
        return IDDGraph;
    })(CoG.ContextGraph);
    exports.IDDGraph = IDDGraph;
});
//# sourceMappingURL=iddgraph.js.map