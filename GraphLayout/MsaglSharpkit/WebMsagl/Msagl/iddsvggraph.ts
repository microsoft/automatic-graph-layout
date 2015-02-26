import G = require('./ggraph');
import SVGG = require('./svggraph');

// Renderer that targets SVG inside IDD.
declare var InteractiveDataDisplay;

export class IDDSVGGraph extends SVGG.SVGGraph {
    gplot: any;

    private static msaglPlot = function (jqDiv, master) {
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
                _svg.setAttribute("preserveAspectRatio", "xMinYMin meet");
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

    chart: any;

    constructor(chartID: string, graph?: G.GGraph) {
        var plotContainerID = chartID + '-container';
        var plotID = chartID + '-plot';
        var container = document.getElementById(chartID);
        container.setAttribute('data-idd-plot', 'plot');
        var containerDiv = document.getElementById(plotContainerID);
        if (containerDiv == undefined) {
            containerDiv = document.createElement('div');
            containerDiv.setAttribute('id', plotContainerID);
            //containerDiv.setAttribute('style', 'width: ' + container.offsetWidth + 'px; height: ' + container.offsetHeight + 'px');
            containerDiv.setAttribute('style', 'width: 100%; height: 100%;');
            containerDiv.setAttribute('data-idd-plot', plotID);
            container.appendChild(containerDiv);
        }

        super(chartID + '-container', graph);

        IDDSVGGraph.msaglPlot.prototype = new InteractiveDataDisplay.Plot;
        InteractiveDataDisplay.register(plotID, function (jqDiv, master) { return new IDDSVGGraph.msaglPlot(jqDiv, master); });
        this.chart = InteractiveDataDisplay.asPlot(chartID);
        var gestureSource = InteractiveDataDisplay.Gestures.getGesturesStream($("#" + chartID));
        this.chart.navigation.gestureSource = gestureSource;
        this.gplot = this.chart.get(plotContainerID);
    }

    drawGraph(): void {
        this.svg = this.gplot.svg;
        while (this.svg.childNodes.length > 0)
            this.svg.removeChild(this.svg.childNodes[0]);
        super.drawGraph();

        var bbox: G.GRect = this.graph.boundingBox;
        var offsetX = bbox.x;
        var offsetY = bbox.y;
        var scale = Math.min(this.container.offsetWidth / bbox.width, this.container.offsetHeight / bbox.height);
        var width = bbox.width * scale;
        var height = bbox.height * scale;
        width = Math.max(bbox.width, bbox.height);
        height = Math.max(bbox.width, bbox.height);

        this.chart.navigation.setVisibleRect({ x: offsetX, y: -bbox.height - offsetY, width: bbox.width, height: bbox.height }, false);
    }
}