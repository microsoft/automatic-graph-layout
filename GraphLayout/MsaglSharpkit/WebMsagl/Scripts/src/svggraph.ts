///<reference path="../../Scripts/typings/FileSaver/FileSaver.d.ts"/>
///<amd-dependency path="filesaver"/>
import G = require('./ggraph');

/** This class, and its concrete subclasses, correlate a geometry object with the SVG object that's currently rendering it.
Note that while the geometry object persists for the duration of the graph, the SVG object can be replaced. */
abstract class RenderElement {
    constructor(group: SVGGElement) {
        this.group = group;
    }
    public group: SVGGElement;
    public abstract getGeometryElement(): G.IElement;
}

class RenderNode extends RenderElement {
    constructor(node: G.GNode, group: SVGGElement) {
        super(group);
        this.node = node;
    }
    public node: G.GNode;
    public getGeometryElement() { return this.node; }
}

class RenderEdge extends RenderElement {
    constructor(edge: G.GEdge, group: SVGGElement) {
        super(group);
        this.edge = edge;
    }
    public edge: G.GEdge;
    public getGeometryElement() { return this.edge; }
}

class RenderEdgeLabel extends RenderElement {
    constructor(edge: G.GEdge, group: SVGGElement) {
        super(group);
        this.edge = edge;
    }
    public edge: G.GEdge;
    public getGeometryElement() { return this.edge.label; }
}

/** Renderer that targets SVG. */
class SVGGraph {
    protected graph: G.GGraph;
    container: HTMLElement;
    svg: SVGSVGElement;
    grid: boolean = false;
    public allowEditing = true;

    private static SVGNS: string = "http://www.w3.org/2000/svg";

    constructor(container: HTMLElement, graph?: G.GGraph) {
        this.container = container;
        this.container.style.position = "relative";
        this.graph = graph === undefined ? null : graph;

        var workingText = document.createTextNode("LAYOUT IN PROGRESS");
        var workingSpan = document.createElement("span");
        workingSpan.setAttribute("style", "position: absolute; top: 50%; width: 100%; text-align: center; z-index: 10");
        workingSpan.style.visibility = "hidden";
        workingSpan.appendChild(workingText);
        this.workingSpan = workingSpan;
        this.container.appendChild(this.workingSpan);
        this.hookUpMouseEvents();
    }

    private edgeRoutingCallback: ((edges: string[]) => void) = null;
    private layoutStartedCallback: () => void = null;
    private workStoppedCallback: () => void = null;

    private workingSpan: HTMLSpanElement;

    public getGraph(): G.GGraph { return this.graph; }
    public setGraph(graph: G.GGraph) {
        if (this.graph != null) {
            this.graph.edgeRoutingCallbacks.remove(this.edgeRoutingCallback);
            this.graph.layoutStartedCallbacks.remove(this.layoutStartedCallback);
            this.graph.workStoppedCallbacks.remove(this.workStoppedCallback);
        }

        this.graph = graph;

        var that = this;
        this.edgeRoutingCallback = edges => {
            if (edges != null)
                for (var e in edges)
                    that.redrawElement(that.renderEdges[edges[e]]);
        };
        this.graph.edgeRoutingCallbacks.add(this.edgeRoutingCallback);
        this.layoutStartedCallback = () => {
            if (this.graph.nodes.length > 0)
                that.workingSpan.style.visibility = "visible";
        };
        this.graph.layoutStartedCallbacks.add(this.layoutStartedCallback);
        this.workStoppedCallback = () => {
            that.workingSpan.style.visibility = "hidden";
        };
        this.graph.workStoppedCallbacks.add(this.workStoppedCallback);
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
        if (curve == null)
            return "";
        if (curve.curvetype === "SegmentedCurve")
            return this.pathSegmentedCurve(<G.GSegmentedCurve>curve, continuous);
        else if (curve.curvetype === "Polyline")
            return this.pathPolyline(<G.GPolyline>curve, continuous);
        else if (curve.curvetype === "Bezier")
            return this.pathBezier(<G.GBezier>curve, continuous);
        else if (curve.curvetype === "Line")
            return this.pathLine(<G.GLine>curve, continuous);
        else if (curve.curvetype === "Ellipse")
            return this.pathEllipse(<G.GEllipse>curve, continuous);
        else if (curve.curvetype === "RoundedRect")
            return this.pathRoundedRect(<G.GRoundedRect>curve, continuous);
        else
            throw "unknown curve type: " + curve.curvetype;
    }

    /** Set this to draw custom labels. Return true to suppress default label rendering, or false to render as default. Note
    that in order to draw a custom label, the node or edge needs to have a label to begin with. The easiest way is to create it
    with a label equal to "". If the element does not have any label at all, this function will not get invoked.
    @param svg The SVG container for the graph.
    @param parent The SVG element that contains the label.
    @param label The label.
    @param owner The element to which this label belongs. */
    customDrawLabel: (svg: Element, parent: Element, label: G.GLabel, owner: G.IElement) => boolean = null;

    private drawLabel(parent: Element, label: G.GLabel, owner: G.IElement): void {
        var g = <SVGGElement>document.createElementNS(SVGGraph.SVGNS, "g");
        if (this.customDrawLabel == null || !this.customDrawLabel(this.svg, parent, label, owner)) {
            var text = document.createElementNS(SVGGraph.SVGNS, "text");
            text.setAttribute("x", label.bounds.x.toString());
            text.setAttribute("y", (label.bounds.y + label.bounds.height).toString());
            text.textContent = label.content;
            text.setAttribute("style", "fill: " + (label.fill == "" ? "black" : label.fill + "; text-anchor: start"));
            g.appendChild(text);
        }
        parent.appendChild(g);
        // If this is an edge label, I need to construct an appropriate RenderEdgeLabel object.
        if (owner instanceof G.GEdge) {
            var edge = owner;
            if (this.renderEdgeLabels[edge.id] == null)
                this.renderEdgeLabels[edge.id] = new RenderEdgeLabel(edge, g);
            var renderLabel = this.renderEdgeLabels[edge.id];
            this.renderEdgeLabels[edge.id].group = g;
            var that = this;
            g.onmouseover = function (e) { that.onEdgeLabelMouseOver(renderLabel, e); };
            g.onmouseout = function (e) { that.onEdgeLabelMouseOut(renderLabel, e); };
        }
    }

    private drawNode(parent: Element, node: G.GNode): void {
        var g = <SVGGElement>document.createElementNS(SVGGraph.SVGNS, "g");
        var nodeCopy = node;
        var that = this;
        g.onclick = function () { that.onNodeClick(nodeCopy); };
        var curve: G.GCurve = node.boundaryCurve;
        var pathString = this.pathCurve(curve, false) + "Z";
        var pathStyle = "stroke: " + node.stroke + "; fill: " + (node.fill == "" ? "none" : node.fill) + "; stroke-width: " + node.thickness + "; stroke-linejoin: miter; stroke-miterlimit: 2.0";
        if (node.shape != null && node.shape.multi > 0) {
            var path = document.createElementNS(SVGGraph.SVGNS, "path");
            path.setAttribute("d", pathString);
            path.setAttribute("transform", "translate(5,5)");
            path.setAttribute("style", pathStyle);
            g.appendChild(path);
        }
        var path = document.createElementNS(SVGGraph.SVGNS, "path");
        path.setAttribute("d", pathString);
        path.setAttribute("style", pathStyle);
        g.appendChild(path);
        if (node.label !== null)
            this.drawLabel(g, node.label, node);
        if (node.tooltip != null) {
            var title = document.createElementNS(SVGGraph.SVGNS, "title");
            title.textContent = node.tooltip;
            g.appendChild(title);
        }
        parent.appendChild(g);

        // Construct the appropriate RenderNode object.
        if (this.renderNodes[node.id] == null)
            this.renderNodes[node.id] = new RenderNode(node, g);
        this.renderNodes[node.id].group = g;
        var renderNode = this.renderNodes[node.id];
        g.onclick = function () { that.onNodeClick(renderNode.node); };
        g.onmouseover = function (e) { that.onNodeMouseOver(renderNode, e); };
        g.onmouseout = function (e) { that.onNodeMouseOut(renderNode, e); };

        var cluster = <G.GCluster>node;
        if (cluster.children !== undefined)
            for (var i = 0; i < cluster.children.length; i++)
                this.drawNode(parent, cluster.children[i]);
    }

    private drawArrow(parent: Element, arrowHead: G.GArrowHead, style: string): void {
        // start is the base of the arrowhead
        var start = arrowHead.start;
        // end is the point where the arrowhead touches the target
        var end = arrowHead.end;
        if (start == null || end == null)
            return;
        // dir is the vector from start to end
        var dir = new G.GPoint({ x: start.x - end.x, y: start.y - end.y });
        // offset (x and y) is the vector from the start to the side
        var offsetX = -dir.y * Math.tan(25 * 0.5 * (Math.PI / 180));
        var offsetY = dir.x * Math.tan(25 * 0.5 * (Math.PI / 180));
        var pathString = "";
        if (arrowHead.style == "tee") {
            pathString += " M" + (start.x + offsetX) + " " + (start.y + offsetY);
            pathString += " L" + (start.x - offsetX) + " " + (start.y - offsetY);
        }
        else if (arrowHead.style == "diamond") {
            pathString += " M" + (start.x) + " " + (start.y);
            pathString += " L" + (start.x - (offsetX + dir.x / 2)) + " " + (start.y - (offsetY + dir.y / 2));
            pathString += " L" + (end.x) + " " + (end.y);
            pathString += " L" + (start.x - (-offsetX + dir.x / 2)) + " " + (start.y - (-offsetY + dir.y / 2));
            pathString += " Z";
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
        var path = document.createElementNS(SVGGraph.SVGNS, "path");
        path.setAttribute("d", pathString);
        if (arrowHead.dash != null)
            style += "; stroke-dasharray: " + arrowHead.dash;
        path.setAttribute("style", style);
        parent.appendChild(path);
    }

    private drawEdge(parent: Element, edge: G.GEdge): void {
        var curve: G.GCurve = edge.curve;
        if (curve == null) {
            console.log("MSAGL warning: did not receive a curve for edge " + edge.id);
            return;
        }
        var g = <SVGGElement>document.createElementNS(SVGGraph.SVGNS, "g");
        var edgeCopy = edge;
        var that = this;
        g.onclick = function () { that.onEdgeClick(edgeCopy); };
        var pathString = this.pathCurve(curve, false);
        var path = document.createElementNS(SVGGraph.SVGNS, "path");
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
            this.drawLabel(this.svg, edge.label, edge);
        if (edge.tooltip != null) {
            var title = document.createElementNS(SVGGraph.SVGNS, "title");
            title.textContent = edge.tooltip;
            g.appendChild(title);
        }
        parent.appendChild(g);

        // Construct the appropriate RenderEdge object.
        if (this.renderEdges[edge.id] == null)
            this.renderEdges[edge.id] = new RenderEdge(edge, g);
        var renderEdge = this.renderEdges[edge.id];
        renderEdge.group = g;
        g.onclick = function () { that.onEdgeClick(renderEdge.edge); };
        g.onmouseover = function (e) { that.onEdgeMouseOver(renderEdge, e); };
        g.onmouseout = function (e) { that.onEdgeMouseOut(renderEdge, e); };
    }

    drawGrid(parent: Element): void {
        for (var x = 0; x < 10; x++)
            for (var y = 0; y < 10; y++) {
                var circle = document.createElementNS(SVGGraph.SVGNS, "circle");
                circle.setAttribute("r", "1");
                circle.setAttribute("x", (x * 100).toString());
                circle.setAttribute("y", (y * 100).toString());
                circle.setAttribute("style", "fill: black; stroke: black; stroke-width: 1");
                parent.appendChild(circle);
            }
    }

    public style: string = "text { stroke: black; fill: black; stroke-width: 0; font-size: 15px; font-family: Verdana, Arial, sans-serif }";

    private renderNodes: { [id: string]: RenderNode };
    private renderEdges: { [id: string]: RenderEdge };
    private renderEdgeLabels: { [id: string]: RenderEdgeLabel };

    populateGraph(): void {
        if (this.style != null) {
            var style = document.createElementNS(SVGGraph.SVGNS, "style");
            var styleText = document.createTextNode(this.style);
            style.appendChild(styleText);
            this.svg.appendChild(style);
        }

        this.renderNodes = {};
        this.renderEdges = {};
        this.renderEdgeLabels = {};
        for (var i = 0; i < this.graph.nodes.length; i++)
            this.drawNode(this.svg, this.graph.nodes[i]);
        for (var i = 0; i < this.graph.edges.length; i++)
            this.drawEdge(this.svg, this.graph.edges[i]);
    }

    drawGraph(): void {
        while (this.svg != null && this.svg.childElementCount > 0)
            this.svg.removeChild(this.svg.firstChild);
        if (this.grid)
            this.drawGrid(this.svg);
        if (this.graph == null)
            return;
        if (this.svg == null) {
            this.svg = <SVGSVGElement>document.createElementNS(SVGGraph.SVGNS, "svg");
            this.container.appendChild(this.svg);
        }

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

    /** Registers several mouse events on the container, to handle editing. */
    protected hookUpMouseEvents() {
        var that = this;
        // Note: the SVG element does not have onmouseleave, and onmouseout is useless because it fires on moving to children.
        this.container.onmousemove = function (e) { that.onMouseMove(e); };
        this.container.onmouseleave = function (e) { that.onMouseOut(e); };
        this.container.onmousedown = function (e) { that.onMouseDown(e); };
        this.container.onmouseup = function (e) { that.onMouseUp(e); };
        this.container.ondblclick = function (e) { that.onMouseDblClick(e); };
    }

    /** Returns true if the SVG node contains the specified group as a child. I need this because SVG elements do not seem
    to have a method to test if they contain a specific child, at least in IE. */
    private containsGroup(g: SVGGElement): boolean {
        if (this.svg.contains != null)
            return this.svg.contains(g);
        for (var i = 0; i < this.svg.childNodes.length; i++)
            if (this.svg.childNodes[i] == g)
                return true;
        return false;
    }

    /** Redraws a single graph element. Used for editing. This is done by removing the element's group, and making a new
    one. */
    private redrawElement(el: RenderElement) {
        if (el instanceof RenderNode) {
            var renderNode = <RenderNode>el;
            if (this.containsGroup(renderNode.group))
                this.svg.removeChild(renderNode.group);
            this.drawNode(this.svg, renderNode.node);
        }
        else if (el instanceof RenderEdge) {
            var renderEdge = <RenderEdge>el;
            if (this.containsGroup(renderEdge.group))
                this.svg.removeChild(renderEdge.group);
            // In the case of edges, I also need to redraw the label.
            var renderLabel = this.renderEdgeLabels[renderEdge.edge.id];
            if (renderLabel != null)
                this.svg.removeChild(renderLabel.group);
            this.drawEdge(this.svg, renderEdge.edge);
            // Also, if it is being edited, I'll need to redraw the control points.
            if (this.edgeEditEdge == renderEdge)
                this.drawPolylineCircles();
        }
        else if (el instanceof RenderEdgeLabel) {
            var renderEdgeLabel = <RenderEdgeLabel>el;
            if (this.containsGroup(renderEdgeLabel.group))
                this.svg.removeChild(renderEdgeLabel.group);
            this.drawLabel(this.svg, renderEdgeLabel.edge.label, renderEdgeLabel.edge);
        }
    }

    /** This callback gets invoked when the user clicks on a node. */
    public onNodeClick: (n: G.GNode) => void = function (n) { };
    /** This callback gets invoked when the user clicks on an edge. */
    public onEdgeClick: (e: G.GEdge) => void = function (e) { };

    /** The point where the mouse cursor is at the moment, in graph space. */
    private mousePoint: G.GPoint = null;
    /** The graph element that's currently under the mouse cursor, if any. */
    private elementUnderMouseCursor: RenderElement = null;

    /** Returns the current mouse coordinates, in graph space. If null, the mouse is outside the graph. */
    public getMousePoint() { return this.mousePoint; };

    /** Returns the graph element that is currently under the mouse cursor. This can be a node, an edge, or an edge label. Note
    that node labels are just considered part of the node. If null, the mouse is over blank space, or not over the graph. */
    public getObjectUnderMouseCursor() {
        return this.elementUnderMouseCursor == null ? null : this.elementUnderMouseCursor.getGeometryElement();
    };

    /** Converts a point from a MouseEvent into graph space coordinates. */
    public getGraphPoint(e: MouseEvent) {
        // I'm using the SVG transformation facilities, to make sure all transformations are
        // accounted for. First, make a SVG point with the mouse coordinates...
        var clientPoint: SVGPoint = this.svg.createSVGPoint();
        clientPoint.x = e.clientX;
        clientPoint.y = e.clientY;
        // Then, reverse the current transformation matrix...
        var matrix = this.svg.getScreenCTM().inverse();
        // Then, apply the reversed matrix to the point, obtaining the new point in graph space.
        var graphPoint = clientPoint.matrixTransform(matrix);
        return new G.GPoint({ x: graphPoint.x, y: graphPoint.y });
    };

    // Mouse event handlers.

    protected onMouseMove(e: MouseEvent) {
        if (this.svg == null)
            return;
        // Update the mouse point.
        this.mousePoint = this.getGraphPoint(e);
        // Do dragging, if needed.
        this.doDrag();
    };
    protected onMouseOut(e: MouseEvent) {
        if (this.svg == null)
            return;
        // Clear the mouse data.
        this.mousePoint = null;
        this.elementUnderMouseCursor = null;
        // End dragging, if needed.
        this.endDrag();
    };
    protected onMouseDown(e: MouseEvent) {
        if (this.svg == null)
            return;
        // Store the point where the mouse went down.
        this.mouseDownPoint = new G.GPoint(this.getGraphPoint(e));
        // Begin dragging, if needed.
        if (this.allowEditing)
            this.beginDrag();
    };
    protected onMouseUp(e: MouseEvent) {
        if (this.svg == null)
            return;
        // End dragging, if needed.
        this.endDrag();
    };
    protected onMouseDblClick(e: MouseEvent) {
        if (this.svg == null)
            return;
        // If an edge is being edited, interpret the double click as an edge corner event. It may be
        // an insertion or a deletion.
        if (this.edgeEditEdge != null)
            this.edgeControlPointEvent(this.getGraphPoint(e));
    }
    private onNodeMouseOver(n: RenderNode, e: MouseEvent) {
        if (this.svg == null)
            return;
        // Update the object under mouse cursor.
        this.elementUnderMouseCursor = n;
    };
    private onNodeMouseOut(n: RenderNode, e: MouseEvent) {
        if (this.svg == null)
            return;
        // Clear the object under mouse cursor.
        this.elementUnderMouseCursor = null;
    };
    private onEdgeMouseOver(ed: RenderEdge, e: MouseEvent) {
        if (this.svg == null)
            return;
        // Update the object under mouse cursor.
        this.elementUnderMouseCursor = ed;
        // If needed, begin editing the edge.
        if (this.allowEditing)
            this.enterEdgeEditMode(ed);
    };
    private onEdgeMouseOut(ed: RenderEdge, e: MouseEvent) {
        if (this.svg == null)
            return;
        // Start the timeout to exit edge edit mode.
        this.beginExitEdgeEditMode();
        // Clear the object under mouse cursor.
        this.elementUnderMouseCursor = null;
    };
    private onEdgeLabelMouseOver(l: RenderEdgeLabel, e: MouseEvent) {
        if (this.svg == null)
            return;
        // Update the object under mouse cursor.
        this.elementUnderMouseCursor = l;
    };
    private onEdgeLabelMouseOut(l: RenderEdgeLabel, e: MouseEvent) {
        if (this.svg == null)
            return;
        // Clear the object under mouse cursor.
        this.elementUnderMouseCursor = null;
    };

    /** The element currently being dragged. */
    private dragElement: RenderElement;
    /** The point where the mouse button went down. Used to establish a delta while dragging. */
    private mouseDownPoint: G.GPoint;

    /** Returns the object that is currently being dragged (or null if nothing is being dragged). */
    public getDragObject() { return this.dragElement == null ? null : this.dragElement.getGeometryElement(); };

    /** Begins a drag operation on the object that is currently under the mouse cursor, if it is a draggable object. */
    private beginDrag() {
        if (this.elementUnderMouseCursor == null)
            return;
        // Get the geometry object being dragged.
        var geometryElement = this.elementUnderMouseCursor.getGeometryElement();
        // Start a geometry move operation.
        this.graph.startMoveElement(geometryElement, this.mouseDownPoint);
        // Store the drag element.
        this.dragElement = this.elementUnderMouseCursor;
    };

    /** Updates the position of the object that is currently being dragged, according to the current mouse position. */
    private doDrag() {
        if (this.dragElement == null)
            return;
        // Compute the delta.
        var delta = this.mousePoint.sub(this.mouseDownPoint);
        // Perform the geometry move operation.
        this.graph.moveElements(delta);
        // Redraw the affected element.
        this.redrawElement(this.dragElement);
    };

    /** Ends the current drag operation, if any. After calling this, further mouse movements will not move any object. */
    private endDrag() {
        // End the geometry move operation.
        this.graph.endMoveElements();
        // Clear the drag element.
        this.dragElement = null;
    };

    /** The ID of the Timeout that's currently waiting to exit edge edit mode. */
    private edgeEditModeTimeout: number;
    /** The edge that's currently being edited. */
    private edgeEditEdge: RenderEdge;

    protected isEditingEdge(): boolean {
        return this.edgeEditEdge != null;
    }

    /** Draws the control points for the edge that is currently being edited. */
    private drawPolylineCircles() {
        if (this.edgeEditEdge == null)
            return;
        var group = this.edgeEditEdge.group;
        var points = this.graph.getPolyline(this.edgeEditEdge.edge.id);
        // I want to move existing circles in preference to deleting and recreating them. This avoids needless mouseout/mouseover events
        // as circles disappear and appear right under the cursor. I'll start by getting all of the circles that are currently present.
        // Note that I am assuming that all Circle elements in the edge group are control point renderings; this should be a safe
        // assumption. If there are circles as part of a custom labels, they will be in a subgroup.
        var existingCircles: any[] = [];
        for (var i = 0; i < group.childNodes.length; i++)
            if (group.childNodes[i].nodeName == "circle")
                existingCircles.push(group.childNodes[i]);
        for (var i = 0; i < points.length; i++) {
            var point = points[i];
            var c = i < existingCircles.length ? existingCircles[i] : <SVGCircleElement>document.createElementNS(SVGGraph.SVGNS, "circle");
            c.setAttribute("r", G.GGraph.EdgeEditCircleRadius.toString());
            c.setAttribute("cx", point.x.toString());
            c.setAttribute("cy", point.y.toString());
            // The fill needs to be explicitly set to transparent. If it is null, the circle will not catch mouse events properly.
            c.setAttribute("style", "stroke: #5555FF; stroke-width: 1px; fill: transparent");
            // If control points have actually been added, they need to be added to the edge group.
            if (i >= existingCircles.length)
                group.insertBefore(c, group.childNodes[0]);
        }
        // If control points have actually been removed, they need to be removed from the edge group.
        for (var i = points.length; i < existingCircles.length; i++)
            group.removeChild(existingCircles[i]);
    }

    /** Removes the control point circles from the edge that's currently being edited. */
    private clearPolylineCircles() {
        if (this.edgeEditEdge == null)
            return;
        // First, make a list of these circles.
        var circles: any[] = [];
        var group = this.edgeEditEdge.group;
        for (var i = 0; i < group.childNodes.length; i++)
            if (group.childNodes[i].nodeName == "circle")
                circles.push(group.childNodes[i]);
        // Then remove them.
        for (var i = 0; i < circles.length; i++)
            group.removeChild(circles[i]);
    }

    /** Starts editing an edge. */
    private enterEdgeEditMode(ed: RenderEdge) {
        if (this.edgeEditEdge == ed) {
            // I am already editing this edge. I just need to clear the timeout, if any.
            clearTimeout(this.edgeEditModeTimeout);
            this.edgeEditModeTimeout = 0;
        }
        // If the user is attempting to start editing another edge, I'll stop right here. They need to wait
        // for the current edge to exit edit mode.
        if (this.edgeEditEdge != null && this.edgeEditEdge != ed)
            return;
        // Mark this as the edge being edited.
        this.edgeEditEdge = ed;
        // Show the control point circles.
        this.drawPolylineCircles();
    }

    /** Exit edge editing mode immediately. */
    private exitEdgeEditMode() {
        var ed = this.edgeEditEdge;
        if (ed == null)
            return;
        // Clear the timeout, it's no longer needed.
        clearTimeout(this.edgeEditModeTimeout);
        // Get rid of the circles.
        this.clearPolylineCircles();
        // Reset the edge editing data.
        this.edgeEditModeTimeout = 0;
        this.edgeEditEdge = null;
    }

    /** This is the timeout (in msecs) during which the user can move the mouse away from the edge and still be
    editing the edge. */
    private static ExitEdgeModeTimeout = 2000;
    /** Sets a timeout to exit edge edit mode. */
    private beginExitEdgeEditMode() {
        var that = this;
        // TODO: what if there already is a timeout at this point? Need to test.
        this.edgeEditModeTimeout = setTimeout(() => that.exitEdgeEditMode(), SVGGraph.ExitEdgeModeTimeout);
    }

    /** Handles an attempt to insert/remove an edge control point. */
    private edgeControlPointEvent(point: G.GPoint) {
        // First, check if the click was right on a control point. 
        var clickedPoint = this.graph.getControlPointAt(this.edgeEditEdge.edge, point);
        if (clickedPoint != null) {
            this.graph.delEdgeControlPoint(this.edgeEditEdge.edge.id, clickedPoint);
            // Note that at this point the mouse will usually be outside the edge, but no other mouse event will fire
            // unless the user moves the mouse. So I need to behave as if it had moved outside the edge: clear the
            // elementUnderMouseCursor, and start the edge editing timeout. Note that, technically, it is possible for
            // the mouse cursor to still be resting on the edge; however, I think this is the best compromise that
            // can be done without having to use hit testing. The SVG spec has hit testing, but it does not work in
            // Mozilla, and at the moment I do not want to reimplement hit testing.
            this.elementUnderMouseCursor = null;
            this.beginExitEdgeEditMode();
        }
        else {
            // The click was not inside any control point. Make a new control point.
            this.graph.addEdgeControlPoint(this.edgeEditEdge.edge.id, point);
            // Note that at this point the mouse will certainly be inside a control point, which means it will
            // technically be on the edge. So I should clear the timeout; otherwise, it will still be ticking unless
            // the user moves the mouse.
            clearTimeout(this.edgeEditModeTimeout);
            this.edgeEditModeTimeout = 0;
            // Set the object under the mouse cursor to be the edge currently being edited. This because it is null
            // at this point (the user has clicked outside the edge), and it will incorrectly remain null even after
            // adding the control point, unless the user moves the mouse.
            this.elementUnderMouseCursor = this.edgeEditEdge;
        }
    }
}

export = SVGGraph