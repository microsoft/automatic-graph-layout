/// <amd-dependancy path="ggraph"/>
/// <amd-dependancy path="svggraph"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
// Renderer that targets an SVG element inside HTML.
var HTMLSVGGraph = (function (_super) {
    __extends(HTMLSVGGraph, _super);
    function HTMLSVGGraph(containerID, graph) {
        _super.call(this, containerID, graph);
        this.grid = false;
        var self = this;
    }
    HTMLSVGGraph.prototype.drawGraph = function () {
        if (this.svg !== undefined)
            this.container.removeChild(this.svg);
        this.svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        _super.prototype.drawGraph.call(this);
        this.container.appendChild(this.svg);
    };
    return HTMLSVGGraph;
})(SVGGraph);
//# sourceMappingURL=htmlsvggraph.js.map