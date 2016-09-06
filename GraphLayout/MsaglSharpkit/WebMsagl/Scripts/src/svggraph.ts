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
    graph: G.GGraph;
    container: HTMLElement;
    svg: SVGSVGElement;
    grid: boolean = false;

    private static SVGNS: string = "http://www.w3.org/2000/svg";

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
            text.setAttribute("style", "fill: " + (label.fill == "" ? "black" : label.fill));
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
        var cluster = <G.GCluster>node;
        if (cluster.children !== undefined)
            for (var i = 0; i < cluster.children.length; i++)
                this.drawNode(parent, cluster.children[i]);

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
        var path = document.createElementNS(SVGGraph.SVGNS, "path");
        path.setAttribute("d", pathString);
        if (arrowHead.dash != null)
            style += "; stroke-dasharray: " + arrowHead.dash;
        path.setAttribute("style", style);
        parent.appendChild(path);
    }

    private drawEdge(parent: Element, edge: G.GEdge): void {
        var g = <SVGGElement>document.createElementNS(SVGGraph.SVGNS, "g");
        var edgeCopy = edge;
        var that = this;
        g.onclick = function () { that.onEdgeClick(edgeCopy); };
        var curve: G.GCurve = edge.curve;
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

    public style: string;

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

        var that = this;
        // Note: the SVG element does not have onmouseleave, and onmouseout is useless because it fires on moving to children.
        this.container.onmousemove = function (e) { return that.onMouseMove(e); };
        this.container.onmouseleave = function (e) { return that.onMouseOut(e); };
        this.container.onmousedown = function (e) { return that.onMouseDown(e); };
        this.container.onmouseup = function (e) { return that.onMouseUp(e); };

        this.populateGraph();
    }

    redrawElement(el: RenderElement) {
        if (el instanceof RenderNode) {
            var renderNode = <RenderNode>el;
            this.svg.removeChild(renderNode.group);
            this.drawNode(this.svg, renderNode.node);
        }
        else if (el instanceof RenderEdge) {
            var renderEdge = <RenderEdge>el;
            this.svg.removeChild(renderEdge.group);
            var renderLabel = this.renderEdgeLabels[renderEdge.edge.id];
            if (renderLabel != null)
                this.svg.removeChild(renderLabel.group);
            this.drawEdge(this.svg, renderEdge.edge);
        }
        else if (el instanceof RenderEdgeLabel) {
            var renderEdgeLabel = <RenderEdgeLabel>el;
            this.svg.removeChild(renderEdgeLabel.group);
            this.drawLabel(this.svg, renderEdgeLabel.edge.label, renderEdgeLabel.edge);
        }
    }

    /** This callback gets invoked when the user clicks on a node. */
    public onNodeClick: (n: G.GNode) => void = function (n) { };
    /** This callback gets invoked when the user clicks on an edge. */
    public onEdgeClick: (e: G.GEdge) => void = function (e) { };

    private mousePoint: G.GPoint = null;
    private elementUnderMouseCursor: RenderElement = null;

    /** Returns the current mouse coordinates, in graph space. If null, the mouse is outside the graph. */
    public getMousePoint() { return this.mousePoint; };

    /** Returns the graph element that is currently under the mouse cursor. This can be a node, an edge, or an edge label. Note
    that node labels are just considered part of the node. If null, the mouse is over blank space, or not over the graph. */
    public getObjectUnderMouseCursor() {
        return this.elementUnderMouseCursor == null ? null : this.elementUnderMouseCursor.getGeometryElement();
    };

    /** Converts a point from a MouseEvent into graph space coordinates. */
    public getGraphPoint(e) {
        var clientPoint = this.svg.createSVGPoint();
        clientPoint.x = e.clientX;
        clientPoint.y = e.clientY;
        var matrix = this.svg.getScreenCTM().inverse();
        var graphPoint = clientPoint.matrixTransform(matrix);
        return new G.GPoint({ x: graphPoint.x, y: graphPoint.y });
    };

    // Mouse event handlers.

    private onMouseMove(e) {
        this.mousePoint = this.getGraphPoint(e);
        this.doDrag();
    };
    private onMouseOut(e) {
        this.mousePoint = null;
        this.elementUnderMouseCursor = null;
        this.endDrag();
    };
    private onMouseDown(e) {
        this.mouseDownPoint = new G.GPoint(this.getGraphPoint(e));
        this.beginDrag();
    };
    private onMouseUp(e) {
        this.endDrag();
    };
    private onNodeMouseOver(n, e) {
        this.elementUnderMouseCursor = n;
    };
    private onNodeMouseOut(n, e) {
        this.elementUnderMouseCursor = null;
    };
    private onEdgeMouseOver(ed, e) {
        this.elementUnderMouseCursor = ed;
    };
    private onEdgeMouseOut(ed, e) {
        this.elementUnderMouseCursor = null;
    };
    private onEdgeLabelMouseOver(l, e) {
        this.elementUnderMouseCursor = l;
    };
    private onEdgeLabelMouseOut(l, e) {
        this.elementUnderMouseCursor = null;
    };

    private dragElement: RenderElement;
    private mouseDownPoint: G.GPoint;

    /** Returns the object that is currently being dragged (or null if nothing is being dragged). */
    public getDragObject() { return this.dragElement == null ? null : this.dragElement.getGeometryElement(); };

    /** Begins a drag operation on the object that is currently under the mouse cursor, if it is a draggable object. */
    private beginDrag() {
        if (this.elementUnderMouseCursor == null)
            return;
        var geometryElement = this.elementUnderMouseCursor.getGeometryElement();
        this.graph.startMoveElement(geometryElement);
        this.dragElement = this.elementUnderMouseCursor;
    };

    /** Updates the position of the object that is currently being dragged, according to the current mouse position. */
    private doDrag() {
        if (this.dragElement == null)
            return;
        var delta = this.mousePoint.sub(this.mouseDownPoint);
        this.graph.moveElements(delta);
        this.redrawElement(this.dragElement);
    };

    /** Ends the current drag operation, if any. After calling this, further mouse movements will not move any object. */
    private endDrag() {
        var that = this;
        this.graph.edgeRoutingCallback = edges => {
            if (edges != null)
                for (var e in edges)
                    that.redrawElement(that.renderEdges[edges[e]]);
        };
        this.graph.endMoveElements();
        this.dragElement = null;
    };
}

export = SVGGraph