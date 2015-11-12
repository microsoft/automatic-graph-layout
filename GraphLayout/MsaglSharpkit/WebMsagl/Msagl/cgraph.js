/// <amd-dependency path="ggraph"/>
/// <amd-dependency path="contextgraph"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
// Renderer that targets a Canvas.
var CGraph = (function (_super) {
    __extends(CGraph, _super);
    function CGraph(canvasID, graph) {
        _super.call(this);
        this.grid = false;
        this.graph = graph === undefined ? null : graph;
        this.canvas = document.getElementById(canvasID);
    }
    CGraph.prototype.drawGraph = function () {
        var context = this.canvas.getContext("2d");
        context.clearRect(0, 0, this.canvas.width, this.canvas.height);
        if (this.grid)
            this.drawGrid(context);
        context.save();
        var bbox = this.graph.boundingBox;
        var offsetX = -bbox.x;
        var offsetY = -bbox.y;
        context.translate(offsetX + 0.5, offsetY + 0.5); // the half-pixel translation makes for nicer antialiasing.
        this.drawGraphInternal(context, this.graph);
        context.restore();
    };
    return CGraph;
})(ContextGraph);
//# sourceMappingURL=cgraph.js.map