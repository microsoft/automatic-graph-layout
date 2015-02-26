define(["require", "exports", './ggraph'], function (require, exports, G) {
    // Abstract class for a renderer that targets a CanvasRenderingContext2D.
    var ContextGraph = (function () {
        function ContextGraph() {
            // Return true to suppress default label rendering.
            this.customDrawLabel = null;
        }
        ContextGraph.prototype.drawEllipse = function (context, ellipse, continuous) {
            var center = ellipse.center;
            var yAxis = ellipse.axisB.y;
            if (yAxis == 0)
                yAxis = ellipse.axisA.y;
            var xAxis = ellipse.axisA.x;
            if (xAxis == 0)
                xAxis = ellipse.axisB.x;
            var ratio = yAxis / xAxis;
            var parStart = ellipse.parStart;
            var parEnd = ellipse.parEnd;
            if (ratio > 0) {
                parStart += Math.PI;
                parEnd += Math.PI;
            }
            context.scale(1, ratio);
            context.arc(center.x, center.y / ratio, Math.abs(xAxis), parStart, parEnd);
            context.scale(1, 1 / ratio);
        };
        ContextGraph.prototype.drawLine = function (context, line, continuous) {
            var start = line.start;
            var end = line.end;
            if (continuous)
                context.lineTo(start.x, start.y);
            else
                context.moveTo(start.x, start.y);
            context.lineTo(end.x, end.y);
        };
        ContextGraph.prototype.drawBezier = function (context, bezier, continuous) {
            var start = bezier.start;
            var p1 = bezier.p1;
            var p2 = bezier.p2;
            var p3 = bezier.p3;
            if (continuous)
                context.lineTo(start.x, start.y);
            else
                context.moveTo(start.x, start.y);
            context.bezierCurveTo(p1.x, p1.y, p2.x, p2.y, p3.x, p3.y);
        };
        ContextGraph.prototype.drawSegmentedCurve = function (context, curve, continuous) {
            for (var i = 0; i < curve.segments.length; i++)
                this.drawCurve(context, curve.segments[i], continuous || i > 0);
        };
        ContextGraph.prototype.drawPolyline = function (context, polyline, continuous) {
            var start = polyline.start;
            if (continuous)
                context.lineTo(start.x, start.y);
            else
                context.moveTo(start.x, start.y);
            for (var i = 0; i < polyline.points.length; i++) {
                var point = polyline.points[i];
                context.lineTo(point.x, point.y);
            }
            if (polyline.closed)
                context.closePath();
        };
        ContextGraph.prototype.drawRoundedRect = function (context, roundedRect, continuous) {
            var curve = roundedRect.getCurve();
            this.drawSegmentedCurve(context, curve, continuous);
        };
        ContextGraph.prototype.drawCurve = function (context, curve, continuous) {
            if (curve.type === "SegmentedCurve")
                this.drawSegmentedCurve(context, curve, continuous);
            else if (curve.type === "Polyline")
                this.drawPolyline(context, curve, continuous);
            else if (curve.type === "Bezier")
                this.drawBezier(context, curve, continuous);
            else if (curve.type === "Line")
                this.drawLine(context, curve, continuous);
            else if (curve.type === "Ellipse")
                this.drawEllipse(context, curve, continuous);
            else if (curve.type === "RoundedRect")
                this.drawRoundedRect(context, curve, continuous);
        };
        ContextGraph.prototype.drawLabel = function (context, label) {
            if (this.customDrawLabel != null && this.customDrawLabel(context, label))
                return;
            if (label.fill != "")
                context.fillStyle = label.fill;
            context.fillText(label.content, label.bounds.x, label.bounds.y + label.bounds.height);
        };
        ContextGraph.prototype.drawNode = function (context, node) {
            var cluster = node;
            if (cluster.children !== undefined)
                for (var i = 0; i < cluster.children.length; i++)
                    this.drawNode(context, cluster.children[i]);
            context.save();
            context.beginPath();
            var curve = node.boundaryCurve;
            this.drawCurve(context, curve, false);
            if (node.stroke != "")
                context.strokeStyle = node.stroke;
            context.stroke();
            if (node.fill != "") {
                context.fillStyle = node.fill;
                context.fill();
            }
            if (node.label !== null)
                this.drawLabel(context, node.label);
            context.restore();
        };
        ContextGraph.prototype.drawArrow = function (context, arrowHead) {
            context.save();
            var start = arrowHead.start;
            var end = arrowHead.end;
            var dir = new G.GPoint({ x: start.x - end.x, y: start.y - end.y });
            var offsetX = -dir.y * Math.tan(25 * 0.5 * (Math.PI / 180));
            var offsetY = dir.x * Math.tan(25 * 0.5 * (Math.PI / 180));
            context.beginPath();
            context.moveTo(start.x + offsetX, start.y + offsetY);
            context.lineTo(end.x, end.y);
            context.lineTo(start.x - offsetX, start.y - offsetY);
            if (arrowHead.closed)
                context.closePath();
            else {
                context.moveTo(start.x, start.y);
                context.lineTo(end.x, end.y);
            }
            context.lineJoin = 'bevel';
            context.stroke();
            if (arrowHead.fill)
                context.fill();
            context.restore();
        };
        ContextGraph.prototype.drawEdge = function (context, edge) {
            context.save();
            context.beginPath();
            var curve = edge.curve;
            this.drawCurve(context, curve, false);
            if (edge.stroke != "")
                context.strokeStyle = edge.stroke;
            context.stroke();
            if (edge.arrowHeadAtTarget != null)
                this.drawArrow(context, edge.arrowHeadAtTarget);
            if (edge.arrowHeadAtSource != null)
                this.drawArrow(context, edge.arrowHeadAtSource);
            if (edge.label != null)
                this.drawLabel(context, edge.label);
            context.restore();
        };
        ContextGraph.prototype.drawGrid = function (context) {
            for (var x = 0; x < 10; x++)
                for (var y = 0; y < 10; y++) {
                    context.beginPath();
                    context.arc(x * 100, y * 100, 1, 0, 2 * Math.PI);
                    context.stroke();
                }
            ;
        };
        ContextGraph.prototype.drawGraphInternal = function (context, graph) {
            for (var i = 0; i < graph.nodes.length; i++)
                this.drawNode(context, graph.nodes[i]);
            for (var i = 0; i < graph.edges.length; i++)
                this.drawEdge(context, graph.edges[i]);
        };
        return ContextGraph;
    })();
    exports.ContextGraph = ContextGraph;
});
//# sourceMappingURL=contextgraph.js.map