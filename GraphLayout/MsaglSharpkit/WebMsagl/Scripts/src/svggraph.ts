///<reference path="../../typings/FileSaver/FileSaver.d.ts"/>
///<amd-dependency path="filesaver"/>
import G = require('./ggraph');

/** Renderer that targets SVG. */
class SVGGraph {
    graph: G.GGraph;
    container: HTMLElement;
    svg: Element;
    grid: boolean = false;

    constructor(container: HTMLElement, graph?: G.GGraph) {
        this.container = container;
        this.graph = graph === undefined ? null : graph;
    }

    public getSVGString(): string {
        if (this.svg == null)
            return null;
        var currentViewBox = this.svg.getAttribute("viewBox");
        var currentPreserve = this.svg.getAttribute("preserveAspectRatio");
        var bbox: G.GRect = this.graph.boundingBox;
        var offsetX = bbox.x;
        var offsetY = bbox.y;
        var maxViewBox: string = "" + offsetX + " " + offsetY + " " + bbox.width + " " + bbox.height;
        this.svg.setAttribute("viewBox", maxViewBox);
        this.svg.removeAttribute("preserveAspectRatio");
        var ret: string = (new XMLSerializer()).serializeToString(this.svg);
        this.svg.setAttribute("viewBox", currentViewBox);
        this.svg.setAttribute("preserveAspectRatio", currentPreserve);
        return ret;
    }

    public saveAsSVG(fileName?: string) {
        fileName = fileName || "graph.svg";
        var svgString = this.getSVGString();
        var blob = new Blob([svgString], { type: "image/svg+xml" });
        saveAs(blob, fileName);
    }

    private pathEllipse(ellipse: G.GEllipse, continuous: boolean): string {
        // Note that MSAGL's representation of ellipses can handle axes that are not horizontal or vertical - but at the moment I can't.
        var center = ellipse.center;
        // Grab the horizontal and vertical axes. These could be either A or B.
        var yAxis = (ellipse.axisB.y == 0) ? ellipse.axisA.y : ellipse.axisB.y;
        var xAxis = (ellipse.axisA.x == 0) ? ellipse.axisB.x : ellipse.axisA.x;
        // Grab their absolute values.
        yAxis = Math.abs(yAxis);
        xAxis = Math.abs(xAxis);
        // Degenerate case: do nothing. Note that it still works if I just proceed from here, but it's a waste of time.
        if (yAxis == 0 || xAxis == 0)
            return "";

        // Grab flags that describe the direction of the arc and axes. I'm going to use these to rotate and flip my way back to the
        // normal case.
        var counterClockwise = ellipse.axisA.x * ellipse.axisB.y - ellipse.axisB.x * ellipse.axisA.y > 0;
        var aHorz = ellipse.axisA.x != 0;
        var aPos = ellipse.axisA.x > 0 || ellipse.axisA.y > 0;

        var parStart = ellipse.parStart;
        var parEnd = ellipse.parEnd;
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
            // Rotate/flip the angle so that I can get back to the normal case (i.e. A horizontal positive, B vertical positive).
            var rots = aHorz ? aPos ? 0 : 2 : (aPos == counterClockwise) ? 1 : 3;
            parStart += Math.PI * rots / 2;
            parEnd += Math.PI * rots / 2;
            if (!counterClockwise) {
                parStart = -parStart;
                parEnd = -parEnd;
            }

            // Proceed as in the normal case.
            var startX = center.x + xAxis * Math.cos(parStart);
            var startY = center.y + yAxis * Math.sin(parStart);
            var endX = center.x + xAxis * Math.cos(parEnd);
            var endY = center.y + yAxis * Math.sin(parEnd);
            var largeArc = Math.abs(parEnd - parStart) > Math.PI;
            var sweepFlag = counterClockwise;
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
        var path = continuous ? "" : (" M" + start.x + " " + start.y);
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
            path += this.pathCurve(curve.segments[i], continuous || path != "");
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

    /** Set this to draw custom labels. Return true to suppress default label rendering, or false to render as default.
    @param svg The SVG container for the graph.
    @param parent The SVG element that contains the label.
    @param label The label.
    @param owner The element to which this label belongs. */
    customDrawLabel: (svg: Element, parent: Element, label: G.GLabel, owner: G.IElement) => boolean = null;

    private drawLabel(parent: Element, label: G.GLabel, owner: G.IElement): void {
        if (this.customDrawLabel != null && this.customDrawLabel(this.svg, parent, label, owner))
            return;
        var text = document.createElementNS("http://www.w3.org/2000/svg", "text");
        text.setAttribute("x", label.bounds.x.toString());
        text.setAttribute("y", (label.bounds.y + label.bounds.height).toString());
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
        var pathString = this.pathCurve(curve, false) + "Z";
        var pathStyle = "stroke: " + node.stroke + "; fill: " + (node.fill == "" ? "none" : node.fill) + "; stroke-width: " + node.thickness + "; stroke-linejoin: miter; stroke-miterlimit: 2.0";
        if (node.shape != null && node.shape.multi > 0) {
            var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
            path.setAttribute("d", pathString);
            path.setAttribute("transform", "translate(5,5)");
            path.setAttribute("style", pathStyle);
            g.appendChild(path);
        }
        var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("d", pathString);
        path.setAttribute("style", pathStyle);
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
                circle.setAttribute("x", (x * 100).toString());
                circle.setAttribute("y", (y * 100).toString());
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

    /** This callback gets invoked when the user clicks on a node. */
    public onNodeClick: (n: G.GNode) => void = function (n) { };
    /** This callback gets invoked when the user clicks on an edge. */
    public onEdgeClick: (e: G.GEdge) => void = function (e) { };
}

export = SVGGraph