import G = require('./ggraph');

declare var $;

// Abstract class that renders to SVG.
export class SVGGraph {
    graph: G.GGraph;
    container: HTMLElement;
    svg: Element;
    grid: boolean = false;

    constructor(containerID: string, graph?: G.GGraph) {
        this.container = document.getElementById(containerID);
        this.graph = graph === undefined ? null : graph;
    }

    private pathEllipse(ellipse: G.GEllipse, continuous: boolean): string {
        var center = ellipse.center;
        // Note that MSAGL's representation of ellipses can handle axes that are not horizontal or vertical - but at the moment I can't.
        var yAxis = (ellipse.axisB.y == 0) ? ellipse.axisA.y : ellipse.axisB.y;
        var xAxis = (ellipse.axisA.x == 0) ? ellipse.axisB.x : ellipse.axisA.x;
        var ratio = yAxis / xAxis;
        var parStart = ellipse.parStart;
        var parEnd = ellipse.parEnd;
        if (ratio > 0) {
            parStart += Math.PI;
            parEnd += Math.PI;
        }
        var path: string = "";
        // The SVG path command is unable to draw a complete ellipse (or an ellipse that is very close to complete), so I need to treat it as a special case.
        var isFullEllipse = Math.abs(Math.abs(parEnd - parStart) - 2 * Math.PI) < 0.01;
        if (isFullEllipse) {
            var firstHalf = new G.GEllipse(ellipse);
            var secondHalf = new G.GEllipse(ellipse);
            firstHalf.parEnd = (ellipse.parStart + ellipse.parEnd) / 2;
            secondHalf.parStart = (ellipse.parStart + ellipse.parEnd) / 2;
            path += this.pathEllipse(firstHalf, continuous);
            path += this.pathEllipse(secondHalf, true);
        }
        else {
            var startX = center.x + xAxis * Math.cos(parStart);
            var startY = center.y + yAxis * Math.sin(parStart);
            var endX = center.x + xAxis * Math.cos(parEnd);
            var endY = center.y + yAxis * Math.sin(parEnd);
            var largeArc = (parEnd - parStart) > Math.PI;
            var sweepFlag = parEnd > parStart;
            path += (continuous ? " L" : " M") + startX + " " + startY;
            path += " A" + xAxis + " " + yAxis;
            path += " 0"; // x-axis-rotation
            path += largeArc ? " 1" : " 0";
            path += sweepFlag ? " 1" : " 0";
            path += " " + endX + " " + endY;
        }
        return path;
    }

    private pathLine(line: G.GLine, continuous: boolean): string {
        var start = line.start;
        var end = line.end;
        var path = (continuous ? " L" : " M") + start.x + " " + start.y;
        path += " L" + end.x + " " + end.y;
        return path;
    }

    private pathBezier(bezier: G.GBezier, continuous: boolean): string {
        var start = bezier.start;
        var p1 = bezier.p1;
        var p2 = bezier.p2;
        var p3 = bezier.p3;
        var path = (continuous ? " L" : " M") + start.x + " " + start.y;
        path += " C" + p1.x + " " + p1.y + " " + p2.x + " " + p2.y + " " + p3.x + " " + p3.y;
        return path;
    }

    private pathSegmentedCurve(curve: G.GSegmentedCurve, continuous: boolean): string {
        var path = "";
        for (var i = 0; i < curve.segments.length; i++)
            path += this.pathCurve(curve.segments[i], continuous || i > 0);
        return path;
    }

    private pathPolyline(polyline: G.GPolyline, continuous: boolean): string {
        var start: G.GPoint = polyline.start;
        var path = " M" + start.x + " " + start.y;
        for (var i = 0; i < polyline.points.length; i++) {
            var point = polyline.points[i];
            path += " L" + point.x + " " + point.y;
        }
        if (polyline.closed)
            path + " F";
        return path;
    }

    private pathRoundedRect(roundedRect: G.GRoundedRect, continuous: boolean): string {
        var curve = roundedRect.getCurve();
        return this.pathSegmentedCurve(curve, continuous);
    }

    private pathCurve(curve: G.GCurve, continuous: boolean): string {
        if (curve.type === "SegmentedCurve")
            return this.pathSegmentedCurve(<G.GSegmentedCurve>curve, continuous);
        else if (curve.type === "Polyline")
            return this.pathPolyline(<G.GPolyline>curve, continuous);
        else if (curve.type === "Bezier")
            return this.pathBezier(<G.GBezier>curve, continuous);
        else if (curve.type === "Line")
            return this.pathLine(<G.GLine>curve, continuous);
        else if (curve.type === "Ellipse")
            return this.pathEllipse(<G.GEllipse>curve, continuous);
        else if (curve.type === "RoundedRect")
            return this.pathRoundedRect(<G.GRoundedRect>curve, continuous);
    }

    // Return true to suppress default label rendering.
    customDrawLabel: (svg: Element, parent: Element, label: G.GLabel, owner: G.IElement) => boolean = null;

    private drawLabel(parent: Element, label: G.GLabel, owner: G.IElement): void {
        if (this.customDrawLabel != null && this.customDrawLabel(this.svg, parent, label, owner))
            return;
        var text = document.createElementNS("http://www.w3.org/2000/svg", "text");
        text.setAttribute("x", label.bounds.x.toString());
        text.setAttribute("y",(label.bounds.y + label.bounds.height).toString());
        text.textContent = label.content;
        text.setAttribute("style", "fill: " + (label.fill == "" ? "black" : label.fill));
        parent.appendChild(text);
    }

    private drawNode(parent: Element, node: G.GNode): void {
        var cluster = <G.GCluster>node;
        if (cluster.children !== undefined)
            for (var i = 0; i < cluster.children.length; i++)
                this.drawNode(parent, cluster.children[i]);

        var g = <SVGGElement>document.createElementNS("http://www.w3.org/2000/svg", "g");
        var nodeCopy = node;
        var thisCopy = this;
        g.onclick = function () { thisCopy.onNodeClick(nodeCopy); };
        var curve: G.GCurve = node.boundaryCurve;
        var pathString = this.pathCurve(curve, false);
        if (node.shape != null && node.shape.multi > 0) {
            var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
            path.setAttribute("d", pathString);
            path.setAttribute("transform", "translate(5,5)");
            path.setAttribute("style", "stroke: " + node.stroke + "; fill: " + (node.fill == "" ? "none" : node.fill) + "; stroke-width: " + node.thickness);
            g.appendChild(path);
        }
        var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("d", pathString);
        path.setAttribute("style", "stroke: " + node.stroke + "; fill: " + (node.fill == "" ? "none" : node.fill) + "; stroke-width: " + node.thickness);
        g.appendChild(path);
        if (node.label !== null)
            this.drawLabel(g, node.label, node);
        if (node.tooltip != null) {
            var title = document.createElementNS("http://www.w3.org/2000/svg", "title");
            title.textContent = node.tooltip;
            g.appendChild(title);
        }
        parent.appendChild(g);
    }

    private drawArrow(parent: Element, arrowHead: G.GArrowHead, style: string): void {
        var start = arrowHead.start;
        var end = arrowHead.end;
        var dir = new G.GPoint({ x: start.x - end.x, y: start.y - end.y });
        var offsetX = -dir.y * Math.tan(25 * 0.5 * (Math.PI / 180));
        var offsetY = dir.x * Math.tan(25 * 0.5 * (Math.PI / 180));
        var pathString = "";
        if (arrowHead.style == "tee") {
            pathString += " M" + (start.x + offsetX) + " " + (start.y + offsetY);
            pathString += " L" + (start.x - offsetX) + " " + (start.y - offsetY);
        }
        else {
            pathString += " M" + (start.x + offsetX) + " " + (start.y + offsetY);
            pathString += " L" + end.x + " " + end.y;
            pathString += " L" + (start.x - offsetX) + " " + (start.y - offsetY);
            if (arrowHead.closed)
                pathString += " Z";
            else {
                pathString += " M" + start.x + " " + start.y;
                pathString += " L" + end.x + " " + end.y;
            }
        }
        var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("d", pathString);
        if (arrowHead.dash != null)
            style += "; stroke-dasharray: " + arrowHead.dash;
        path.setAttribute("style", style);
        parent.appendChild(path);
    }

    private drawEdge(parent: Element, edge: G.GEdge): void {
        var g = <SVGGElement>document.createElementNS("http://www.w3.org/2000/svg", "g");
        var edgeCopy = edge;
        var thisCopy = this;
        g.onclick = function () { thisCopy.onEdgeClick(edgeCopy); };
        var curve: G.GCurve = edge.curve;
        var pathString = this.pathCurve(curve, false);
        var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("d", pathString);
        var style = "stroke: " + edge.stroke + "; stroke-width: " + edge.thickness + "; fill: none"
        if (edge.dash != null)
            style += "; stroke-dasharray: " + edge.dash;
        path.setAttribute("style", style);
        g.appendChild(path);
        if (edge.arrowHeadAtTarget != null)
            this.drawArrow(g, edge.arrowHeadAtTarget, "stroke: " + edge.stroke + "; stroke-width: " + edge.thickness + "; fill: " + (edge.arrowHeadAtTarget.fill ? edge.stroke : "none"));
        if (edge.arrowHeadAtSource != null)
            this.drawArrow(g, edge.arrowHeadAtSource, "stroke: " + edge.stroke + "; stroke-width: " + edge.thickness + "; fill: " + (edge.arrowHeadAtSource.fill ? edge.stroke : "none"));
        if (edge.label != null)
            this.drawLabel(g, edge.label, edge);
        if (edge.tooltip != null) {
            var title = document.createElementNS("http://www.w3.org/2000/svg", "title");
            title.textContent = edge.tooltip;
            g.appendChild(title);
        }
        parent.appendChild(g);
    }

    drawGrid(parent: Element): void {
        for (var x = 0; x < 10; x++)
            for (var y = 0; y < 10; y++) {
                var circle = document.createElementNS("http://www.w3.org/2000/svg", "circle");
                circle.setAttribute("r", "1");
                circle.setAttribute("x",(x * 100).toString());
                circle.setAttribute("y",(y * 100).toString());
                circle.setAttribute("style", "fill: black; stroke: black; stroke-width: 1");
                parent.appendChild(circle);
            }
    }

    public style: string;

    populateGraph(): void {
        if (this.style != null) {
            var style = document.createElementNS("http://www.w3.org/2000/svg", "style");
            var styleText = document.createTextNode(this.style);
            style.appendChild(styleText);
            this.svg.appendChild(style);
        }

        for (var i = 0; i < this.graph.nodes.length; i++)
            this.drawNode(this.svg, this.graph.nodes[i]);
        for (var i = 0; i < this.graph.edges.length; i++)
            this.drawEdge(this.svg, this.graph.edges[i]);
    }

    drawGraph(): void {
        if (this.grid)
            this.drawGrid(this.svg);
        if (this.graph == null)
            return;

        var bbox: G.GRect = this.graph.boundingBox;
        var offsetX = bbox.x;
        var offsetY = bbox.y;
        var width = this.container.offsetWidth;
        var height = this.container.offsetHeight;
        //this.svg.setAttribute("style", "width: " + width + "px; height: " + height + "px");
        //this.svg.setAttribute("style", "width: 100%; height: 100%");
        var viewBox: string = "" + offsetX + " " + offsetY + " " + bbox.width + " " + bbox.height;
        this.svg.setAttribute("viewBox", viewBox);

        this.populateGraph();
    }

    public onNodeClick: (n: G.GNode) => void = function (n) { };
    public onEdgeClick: (e: G.GEdge) => void = function (e) { };
}