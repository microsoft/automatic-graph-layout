// Geometry graph.
export interface IPoint {
    x: number;
    y: number;
}

export class GPoint implements IPoint {
    x: number;
    y: number;
    constructor(p: any)
    constructor(p: IPoint) {
        this.x = p.x === undefined ? 0 : p.x;
        this.y = p.y === undefined ? 0 : p.y;
    }
    static origin = new GPoint({ x: 0, y: 0 });
    add(other: IPoint) {
        return new GPoint({ x: this.x + other.x, y: this.y + other.y });
    }
    sub(other: IPoint) {
        return new GPoint({ x: this.x - other.x, y: this.y - other.y });
    }
    div(op: number) {
        return new GPoint({ x: this.x / op, y: this.y / op });
    }
    mul(op: number) {
        return new GPoint({ x: this.x * op, y: this.y * op });
    }
}

export interface IRect {
    x: number;
    y: number;
    width: number;
    height: number;
}

export class GRect implements IRect {
    x: number;
    y: number;
    width: number;
    height: number;
    constructor(r: any)
    constructor(r: IRect) {
        this.x = r.x === undefined ? 0 : r.x;
        this.y = r.y === undefined ? 0 : r.y;
        this.width = r.width === undefined ? 0 : r.width;
        this.height = r.height === undefined ? 0 : r.height;
    }
    static zero = new GRect({ x: 0, y: 0, width: 0, height: 0 });
    getTopLeft(): GPoint {
        return new GPoint({ x: this.x, y: this.y });
    }
    getBottomRight(): GPoint {
        return new GPoint({ x: this.getRight(), y: this.getBottom() });
    }
    getBottom(): number {
        return this.y + this.height;
    }
    getRight(): number {
        return this.x + this.width;
    }
    getCenter(): GPoint {
        return new GPoint({ x: this.x + this.width / 2, y: this.y + this.height / 2 });
    }
    extend(other: GRect) {
        if (other == null)
            return this;
        return new GRect({
            x: Math.min(this.x, other.x),
            y: Math.min(this.y, other.y),
            width: Math.max(this.getRight(), other.getRight()) - Math.min(this.x, other.x),
            height: Math.max(this.getBottom(), other.getBottom()) - Math.min(this.y, other.y)
        });
    }
    extendP(point: GPoint) {
        return this.extend(new GRect({ x: point.x, y: point.y, width: 0, height: 0 }));
    }
}

export interface ICurve {
    type: string;
    getCenter(): GPoint;
    getBoundingBox(): GRect;
}

export class GCurve implements ICurve {
    type: string;
    constructor(type: string) {
        if (type === undefined)
            throw new Error("Undefined curve type");
        this.type = type;
    }
    getCenter(): GPoint {
        return GPoint.origin
    }
    getBoundingBox(): GRect {
        return GRect.zero;
    }
    static ofCurve(curve: ICurve): GCurve {
        if (curve == null || curve === undefined)
            return null;
        var ret: GCurve;
        if (curve.type == "Ellipse")
            ret = new GEllipse(<IEllipse><any>curve);
        else if (curve.type == "Line")
            ret = new GLine(<ILine><any>curve);
        else if (curve.type == "Bezier")
            ret = new GBezier(<IBezier><any>curve);
        else if (curve.type == "Polyline")
            ret = new GPolyline(<IPolyline><any>curve);
        else if (curve.type == "SegmentedCurve")
            ret = new GSegmentedCurve(<ISegmentedCurve><any>curve);
        else if (curve.type == "RoundedRect")
            ret = new GRoundedRect(<IRoundedRect><any>curve);
        return ret;
    }
}

export interface IEllipse {
    center: IPoint;
    axisA: IPoint;
    axisB: IPoint;
    parStart: number;
    parEnd: number;
}

export class GEllipse extends GCurve implements IEllipse {
    center: GPoint;
    axisA: GPoint;
    axisB: GPoint;
    parStart: number;
    parEnd: number;
    constructor(ellipse: any)
    constructor(ellipse: IEllipse) {
        super("Ellipse");
        this.center = ellipse.center === undefined ? GPoint.origin : new GPoint(ellipse.center);
        this.axisA = ellipse.axisA === undefined ? GPoint.origin : new GPoint(ellipse.axisA);
        this.axisB = ellipse.axisB === undefined ? GPoint.origin : new GPoint(ellipse.axisB);
        this.parStart = ellipse.parStart === undefined ? 0 : ellipse.parStart;
        this.parEnd = ellipse.parStart === undefined ? Math.PI * 2 : ellipse.parEnd;
    }
    getCenter(): GPoint {
        return this.center;
    }
    getBoundingBox(): GRect {
        var width = 2 * Math.max(Math.abs(this.axisA.x), Math.abs(this.axisB.x));
        var height = 2 * Math.max(Math.abs(this.axisA.y), Math.abs(this.axisB.y));
        var p = this.center.sub({ x: width / 2, y: height / 2 });
        return new GRect({ x: p.x, y: p.y, width: width, height: height });
    }
    static make(width: number, height: number): GEllipse {
        return new GEllipse({ center: GPoint.origin, axisA: new GPoint({ x: width / 2, y: 0 }), axisB: new GPoint({ x: 0, y: height / 2 }), parStart: 0, parEnd: Math.PI * 2 });
    }
}

export interface ILine {
    start: IPoint;
    end: IPoint;
}

export class GLine extends GCurve implements ILine {
    start: GPoint;
    end: GPoint;
    constructor(line: any)
    constructor(line: ILine) {
        super("Line");
        this.start = line.start === undefined ? GPoint.origin : new GPoint(line.start);
        this.end = line.end === undefined ? GPoint.origin : new GPoint(line.end);
    }
    getCenter(): GPoint {
        return this.start.add(this.end).div(2);
    }
    getBoundingBox(): GRect {
        var ret = new GRect({ x: this.start.x, y: this.start.y, width: 0, height: 0 });
        ret = ret.extendP(this.end);
        return ret;
    }
}

export interface IPolyline {
    start: IPoint;
    points: IPoint[];
    closed: boolean;
}

export class GPolyline extends GCurve implements ICurve {
    start: GPoint;
    points: GPoint[];
    closed: boolean;
    constructor(polyline: any)
    constructor(polyline: IPolyline) {
        super("Polyline");
        this.start = polyline.start === undefined ? GPoint.origin : new GPoint(polyline.start);
        this.points = [];
        for (var i = 0; i < polyline.points.length; i++)
            this.points.push(new GPoint(polyline.points[i]));
        this.closed = polyline.closed === undefined ? false : polyline.closed;
    }
    getCenter(): GPoint {
        var ret: GPoint = this.start;
        for (var i = 0; i < this.points.length; i++)
            ret = ret.add(this.points[i]);
        ret = ret.div(1 + this.points.length);
        return ret;
    }
    getBoundingBox(): GRect {
        var ret = new GRect({ x: this.points[0].x, y: this.points[0].y, height: 0, width: 0 });
        for (var i = 1; i < this.points.length; i++)
            ret = ret.extendP(this.points[i]);
        return ret;
    }
}

export interface IRoundedRect {
    bounds: IRect;
    radiusX: number;
    radiusY: number;
}

export class GRoundedRect extends GCurve implements IRoundedRect {
    bounds: GRect;
    radiusX: number;
    radiusY: number;
    constructor(roundedRect: any)
    constructor(roundedRect: IRoundedRect) {
        super("RoundedRect");
        this.bounds = roundedRect.bounds === undefined ? GRect.zero : new GRect(roundedRect.bounds);
        this.radiusX = roundedRect.radiusX === undefined ? 0 : roundedRect.radiusX;
        this.radiusY = roundedRect.radiusY === undefined ? 0 : roundedRect.radiusY;
    }
    getCenter(): GPoint {
        return this.bounds.getCenter();
    }
    getBoundingBox(): GRect {
        return this.bounds;
    }
    getCurve(): GSegmentedCurve {
        var segments: GCurve[] = [];
        var axisA = new GPoint({ x: this.radiusX, y: 0 });
        var axisB = new GPoint({ x: 0, y: this.radiusY });
        var innerBounds = new GRect({ x: this.bounds.x + this.radiusX, y: this.bounds.y + this.radiusY, width: this.bounds.width - this.radiusX * 2, height: this.bounds.height - this.radiusY * 2 });
        segments.push(new GEllipse({ axisA: axisA, axisB: axisB, center: new GPoint({ x: innerBounds.x, y: innerBounds.y }), parStart: 0, parEnd: Math.PI / 2 }));
        segments.push(new GLine({ start: new GPoint({ x: innerBounds.x, y: this.bounds.y }), end: new GPoint({ x: innerBounds.x + innerBounds.width, y: this.bounds.y }) }));
        segments.push(new GEllipse({ axisA: axisA, axisB: axisB, center: new GPoint({ x: innerBounds.x + innerBounds.width, y: innerBounds.y }), parStart: Math.PI / 2, parEnd: Math.PI }));
        segments.push(new GLine({ start: new GPoint({ x: this.bounds.x + this.bounds.width, y: innerBounds.y }), end: new GPoint({ x: this.bounds.x + this.bounds.width, y: innerBounds.y + innerBounds.height }) }));
        segments.push(new GEllipse({ axisA: axisA, axisB: axisB, center: new GPoint({ x: innerBounds.x + innerBounds.width, y: innerBounds.y + innerBounds.height }), parStart: Math.PI, parEnd: Math.PI * 3 / 2 }));
        segments.push(new GLine({ start: new GPoint({ x: innerBounds.x + innerBounds.width, y: this.bounds.y + this.bounds.height }), end: new GPoint({ x: innerBounds.x, y: this.bounds.y + this.bounds.height }) }));
        segments.push(new GEllipse({ axisA: axisA, axisB: axisB, center: new GPoint({ x: innerBounds.x, y: innerBounds.y + innerBounds.height }), parStart: Math.PI * 3 / 2, parEnd: Math.PI * 2 }));
        segments.push(new GLine({ start: new GPoint({ x: this.bounds.x, y: innerBounds.y + innerBounds.height }), end: new GPoint({ x: this.bounds.x, y: innerBounds.y }) }));
        return new GSegmentedCurve({ segments: segments });
    }
}

export interface IBezier {
    start: IPoint;
    p1: IPoint;
    p2: IPoint;
    p3: IPoint;
}

export class GBezier extends GCurve implements ICurve {
    start: GPoint;
    p1: GPoint;
    p2: GPoint;
    p3: GPoint;
    constructor(bezier: any)
    constructor(bezier: IBezier) {
        super("Bezier");
        this.start = bezier.start === undefined ? GPoint.origin : new GPoint(bezier.start);
        this.p1 = bezier.p1 === undefined ? GPoint.origin : new GPoint(bezier.p1);
        this.p2 = bezier.p2 === undefined ? GPoint.origin : new GPoint(bezier.p2);
        this.p3 = bezier.p3 === undefined ? GPoint.origin : new GPoint(bezier.p3);
    }
    getCenter(): GPoint {
        var ret: GPoint = this.start;
        ret = ret.add(this.p1);
        ret = ret.add(this.p2);
        ret = ret.add(this.p3);
        ret = ret.div(4);
        return ret;
    }
    getBoundingBox(): GRect {
        var ret = new GRect({ x: this.start.x, y: this.start.y, width: 0, height: 0 });
        ret = ret.extendP(this.p1);
        ret = ret.extendP(this.p2);
        ret = ret.extendP(this.p3);
        return ret;
    }
}

export interface ISegmentedCurve {
    segments: ICurve[];
}

export class GSegmentedCurve extends GCurve implements ICurve {
    segments: ICurve[];
    constructor(segmentedCurve: any)
    constructor(segmentedCurve: ISegmentedCurve) {
        super("SegmentedCurve");
        this.segments = [];
        for (var i = 0; i < segmentedCurve.segments.length; i++)
            this.segments.push(GCurve.ofCurve(segmentedCurve.segments[i]));
    }
    getCenter(): GPoint {
        var ret: GPoint = GPoint.origin;
        for (var i = 0; i < this.segments.length; i++)
            ret = ret.add(this.segments[i].getCenter());
        ret = ret.div(this.segments.length);
        return ret;
    }
    getBoundingBox(): GRect {
        var ret = this.segments[0].getBoundingBox();
        for (var i = 1; i < this.segments.length; i++)
            ret = ret.extend(this.segments[i].getBoundingBox());
        return ret;
    }
}

export interface IElement {
    tooltip: string;
}

export interface ILabel {
    bounds: IRect;
    content: string;
    fill: string;
}

export class GLabel {
    bounds: IRect;
    content: string;
    fill: string;
    constructor(label: any)
    constructor(label: ILabel) {
        if (typeof (label) == "string")
            this.content = <string><any>label;
        else {
            this.bounds = label.bounds == undefined || label.bounds == GRect.zero ? GRect.zero : new GRect(label.bounds);
            this.content = label.content;
            this.fill = label.fill === undefined ? "Black" : label.fill;
        }
    }
}

export class GShape {
    static GetRect(): GShape {
        var ret = new GShape();
        ret.shape = "rect";
        return ret;
    }
    static GetRoundedRect(radiusX?: number, radiusY?: number): GShape {
        var ret = new GShape();
        ret.shape = "rect";
        ret.radiusX = radiusX === undefined ? 5 : radiusX;
        ret.radiusY = radiusY === undefined ? 5 : radiusY;
        return ret;
    }
    static GetMaxRoundedRect(): GShape {
        var ret = new GShape();
        ret.shape = "rect";
        ret.radiusX = null;
        ret.radiusY = null;
        return ret;
    }
    static RectShape = "rect";
    static FromString(shape: string) {
        if (shape == "rect")
            return GShape.GetRect();
        else if (shape == "roundedrect")
            return GShape.GetRoundedRect();
        else if (shape == "maxroundedrect")
            return GShape.GetMaxRoundedRect();
        return null;
    }
    constructor() {
        this.radiusX = 0;
        this.radiusY = 0;
        this.multi = 0;
    }
    shape: string;
    radiusX: number;
    radiusY: number;
    multi: number;
}

export interface INode extends IElement {
    id: string;
    label: ILabel;
    labelMargin: number;
    shape: GShape;
    boundaryCurve: ICurve;
    thickness: number;
    fill: string;
    stroke: string;
}

export class GNode implements INode {
    id: string;
    tooltip: string;
    label: GLabel;
    labelMargin: number;
    shape: GShape;
    boundaryCurve: GCurve;
    thickness: number;
    fill: string;
    stroke: string;
    constructor(node: any)
    constructor(node: INode) {
        if (node.id === undefined)
            throw new Error("Undefined node id");
        this.id = node.id;
        this.tooltip = node.tooltip === undefined ? null : node.tooltip;
        this.shape = node.shape === undefined ? null : typeof (node.shape) == "string" ? GShape.FromString(<string><any>node.shape) : node.shape;
        this.boundaryCurve = GCurve.ofCurve(node.boundaryCurve);
        this.label = node.label === undefined ? null : node.label == null ? null : typeof (node.label) == "string" ? new GLabel({ content: node.label }) : new GLabel(node.label);
        this.labelMargin = node.labelMargin === undefined ? 5 : node.labelMargin;
        this.thickness = node.thickness == undefined ? 1 : node.thickness;
        this.fill = node.fill === undefined ? "" : node.fill;
        this.stroke = node.stroke === undefined ? "Black" : node.stroke;
    }
    isCluster() {
        return (<GCluster>this).children !== undefined;
    }
}

export interface ICluster extends INode {
    children: INode[];
}

export class GCluster extends GNode implements ICluster {
    children: GNode[];
    constructor(cluster: any)
    constructor(cluster: ICluster) {
        super(cluster);
        this.children = [];
        for (var i = 0; i < cluster.children.length; i++)
            if ((<GCluster>cluster.children[i]).children !== undefined)
                this.children.push(new GCluster(<ICluster>cluster.children[i]));
            else
                this.children.push(new GNode(cluster.children[i]));
    }
}

export interface IArrowHead {
    start: IPoint;
    end: IPoint;
    closed: boolean;
    fill: boolean;
}

export class GArrowHead implements IArrowHead {
    start: IPoint;
    end: IPoint;
    closed: boolean;
    fill: boolean;
    constructor(arrowHead: any)
    constructor(arrowHead: IArrowHead) {
        this.start = arrowHead.start === undefined ? GPoint.origin : arrowHead.start;
        this.end = arrowHead.end === undefined ? GPoint.origin : arrowHead.end;
        this.closed = arrowHead.closed === undefined ? false : arrowHead.closed;
        this.fill = arrowHead.fill === undefined ? false : arrowHead.fill;
    }
    static standard: GArrowHead = new GArrowHead({ start: GPoint.origin, end: GPoint.origin, closed: false, fill: false });
    static closed: GArrowHead = new GArrowHead({ start: GPoint.origin, end: GPoint.origin, closed: true, fill: false });
    static filled: GArrowHead = new GArrowHead({ start: GPoint.origin, end: GPoint.origin, closed: true, fill: true });
}

export interface IEdge extends IElement {
    id: string;
    source: string;
    target: string;
    label: ILabel;
    arrowHeadAtTarget: GArrowHead;
    arrowHeadAtSource: GArrowHead;
    thickness: number;
    curve: ICurve;
    stroke: string;
}

export class GEdge implements IEdge {
    id: string;
    tooltip: string;
    source: string;
    target: string;
    label: GLabel;
    arrowHeadAtTarget: GArrowHead;
    arrowHeadAtSource: GArrowHead;
    thickness: number;
    curve: GCurve;
    stroke: string;
    constructor(edge: any)
    constructor(edge: IEdge) {
        if (edge.id === undefined)
            throw new Error("Undefined edge id");
        if (edge.source === undefined)
            throw new Error("Undefined edge source");
        if (edge.target === undefined)
            throw new Error("Undefined edge target");
        this.id = edge.id;
        this.tooltip = edge.tooltip === undefined ? null : edge.tooltip;
        this.source = edge.source;
        this.target = edge.target;
        this.label = edge.label === undefined || edge.label == null ? null : typeof (edge.label) == "string" ? new GLabel({ content: edge.label }) : new GLabel(edge.label);
        this.arrowHeadAtTarget = edge.arrowHeadAtTarget === undefined ? GArrowHead.standard : edge.arrowHeadAtTarget == null ? null : new GArrowHead(edge.arrowHeadAtTarget);
        this.arrowHeadAtSource = edge.arrowHeadAtSource === undefined || edge.arrowHeadAtSource == null ? null : new GArrowHead(edge.arrowHeadAtSource);
        this.thickness = edge.thickness == undefined ? 1 : edge.thickness;
        this.curve = edge.curve === undefined ? null : GCurve.ofCurve(edge.curve);
        this.stroke = edge.stroke === undefined ? "Black" : edge.stroke;
    }
}

export interface IPlaneTransformation {
    m00: number;
    m01: number;
    m02: number;
    m10: number;
    m11: number;
    m12: number;
}

export class GPlaneTransformation implements IPlaneTransformation {
    m00: number;
    m01: number;
    m02: number;
    m10: number;
    m11: number;
    m12: number;
    constructor(transformation: any)
    constructor(transformation: IPlaneTransformation) {
        if ((<any>transformation).rotation !== undefined) {
            var angle = (<any>transformation).rotation;
            var cos = Math.cos(angle);
            var sin = Math.sin(angle);
            this.m00 = cos;
            this.m01 = -sin;
            this.m02 = 0;
            this.m10 = sin;
            this.m11 = cos;
            this.m12 = 0;
        }
        else {
            this.m00 = transformation.m00 === undefined ? -1 : transformation.m00;
            this.m01 = transformation.m01 === undefined ? -1 : transformation.m01;
            this.m02 = transformation.m02 === undefined ? -1 : transformation.m02;
            this.m10 = transformation.m10 === undefined ? -1 : transformation.m10;
            this.m11 = transformation.m11 === undefined ? -1 : transformation.m11;
            this.m12 = transformation.m12 === undefined ? -1 : transformation.m12;
        }
    }
    static defaultTransformation = new GPlaneTransformation({ m00: -1, m01: 0, m02: 0, m10: 0, m11: -1, m12: 0 });
}

export class ISettings {
    transformation: IPlaneTransformation;
    routing: string;
}

export class GSettings {
    transformation: GPlaneTransformation;
    routing: string;
    constructor(settings: any)
    constructor(settings: ISettings) {
        this.transformation = settings.transformation === undefined ? GPlaneTransformation.defaultTransformation : settings.transformation;
        this.routing = settings.routing === undefined ? GSettings.sugiyamaSplinesRouting : settings.routing;
    }
    static sugiyamaSplinesRouting = "sugiyamasplines";
    static rectilinearRouting = "rectilinear";
}

export class IGraph {
    nodes: GNode[];
    edges: GEdge[];
    boundingBox: IRect;
    settings: ISettings;
}

class GNodeInternal {
    node: GNode;
    outEdges: string[];
    inEdges: string[];
    selfEdges: string[];
}

export class GGraph implements IGraph {
    private nodesMap: Object; // id -> GNodeInternal
    private edgesMap: Object; // id -> GEdge
    nodes: GNode[];
    edges: GEdge[];
    boundingBox: GRect;
    settings: GSettings;

    constructor() {
        this.nodesMap = new Object();
        this.edgesMap = new Object();
        this.nodes = [];
        this.edges = [];
        this.boundingBox = GRect.zero;
        this.settings = new GSettings({ transformation: { m00: -1, m01: 0, m02: 0, m10: 0, m11: -1, m12: 0 } });
    }

    addNode(node: GNode): void {
        this.nodesMap[node.id] = <GNodeInternal>{ node: node, outEdges: [], inEdges: [], selfEdges: [] };
        this.nodes.push(node);
    }

    getNode(id: string): GNode {
        var nodeInternal = <GNodeInternal>this.nodesMap[id];
        return nodeInternal == null ? null : nodeInternal.node;
    }

    getInEdges(id: string): string[] {
        var nodeInternal = <GNodeInternal>this.nodesMap[id];
        return nodeInternal == null ? null : nodeInternal.inEdges;
    }

    getOutEdges(id: string): string[] {
        var nodeInternal = <GNodeInternal>this.nodesMap[id];
        return nodeInternal == null ? null : nodeInternal.outEdges;
    }

    getSelfEdges(id: string): string[] {
        var nodeInternal = <GNodeInternal>this.nodesMap[id];
        return nodeInternal == null ? null : nodeInternal.selfEdges;
    }

    addEdge(edge: GEdge): void {
        if (this.nodesMap[edge.source] == null)
            throw new Error("Undefined node " + edge.source);
        if (this.nodesMap[edge.target] == null)
            throw new Error("Undefined node " + edge.target);
        this.edgesMap[edge.id] = edge;
        this.edges.push(edge);
        if (edge.source == edge.target)
            (<GNodeInternal>this.nodesMap[edge.source]).selfEdges.push(edge.id);
        else {
            (<GNodeInternal>this.nodesMap[edge.source]).outEdges.push(edge.id);
            (<GNodeInternal>this.nodesMap[edge.target]).inEdges.push(edge.id);
        }
    }

    getEdge(id: string): GEdge {
        return this.edgesMap[id];
    }

    getJSON(): string {
        var igraph: IGraph = { nodes: this.nodes, edges: this.edges, boundingBox: this.boundingBox, settings: this.settings };
        var ret: string = JSON.stringify(igraph);
        return ret;
    }

    static ofJSON(json: string): GGraph {
        var igraph: IGraph = JSON.parse(json);
        if (igraph.edges === undefined)
            igraph.edges = [];
        var ret = new GGraph();
        ret.boundingBox = new GRect(igraph.boundingBox === undefined ? GRect.zero : igraph.boundingBox);
        ret.settings = new GSettings(igraph.settings === undefined ? {} : igraph.settings);
        for (var i = 0; i < igraph.nodes.length; i++) {
            var inode: INode = igraph.nodes[i];
            if ((<ICluster>inode).children !== undefined) {
                var gcluster = new GCluster(<ICluster>inode);
                ret.addNode(gcluster);
            }
            else {
                var gnode = new GNode(inode);
                ret.addNode(gnode);
            }
        }
        for (var i = 0; i < igraph.edges.length; i++) {
            var iedge = igraph.edges[i];
            var gedge = new GEdge(iedge);
            ret.addEdge(gedge);
        }

        return ret;
    }

    private createNodeBoundariesRec(node: GNode, sizer?: (label: GLabel, owner: IElement) => IPoint) {
        if (node.boundaryCurve == null) {
            if (node.label != null && node.label.bounds == GRect.zero && sizer !== undefined) {
                var labelSize = sizer(node.label, node);
                node.label.bounds = new GRect({ x: 0, y: 0, width: labelSize.x, height: labelSize.y });
            }
            var labelWidth = node.label == null ? 0 : node.label.bounds.width;
            var labelHeight = node.label == null ? 0 : node.label.bounds.height;
            labelWidth += 2 * node.labelMargin;
            labelHeight += 2 * node.labelMargin;
            var boundary: GCurve;
            if (node.shape != null && node.shape.shape == GShape.RectShape) {
                var radiusX = node.shape.radiusX;
                var radiusY = node.shape.radiusY;
                if (radiusX == null && radiusY == null) {
                    var k = Math.min(labelWidth, labelHeight);
                    radiusX = radiusY = k / 2;
                }
                boundary = new GRoundedRect({
                    bounds: new GRect({ x: 0, y: 0, width: labelWidth, height: labelHeight }), radiusX: radiusX, radiusY: radiusY
                });
            }
            else
                boundary = GEllipse.make(labelWidth * Math.sqrt(2), labelHeight * Math.sqrt(2));
            node.boundaryCurve = boundary;
        }

        var cluster = <GCluster>node;
        if (cluster.children !== undefined)
            for (var i = 0; i < cluster.children.length; i++)
                this.createNodeBoundariesRec(cluster.children[i], sizer);
    }

    // Creates the node boundaries for nodes that don't have one. If the node has a label, it will first
    // compute the label's size based on the provided size function, and then create an appropriate boundary.
    createNodeBoundaries(sizer?: (label: GLabel, owner: IElement) => IPoint) {
        for (var i = 0; i < this.nodes.length; i++)
            this.createNodeBoundariesRec(this.nodes[i], sizer);

        // Assign size to edge labels too.
        if (sizer !== undefined) {
            for (var i = 0; i < this.edges.length; i++) {
                var edge = this.edges[i];
                if (edge.label != null && edge.label.bounds == GRect.zero) {
                    var labelSize = sizer(edge.label, edge);
                    edge.label.bounds = new GRect({ width: labelSize.x, height: labelSize.y });
                }
            }
        }
    }

    static contextSizer(context: CanvasRenderingContext2D, label: GLabel): IPoint {
        return { x: context.measureText(label.content).width, y: parseInt(context.font) };
    }

    createNodeBoundariesFromContext(context?: CanvasRenderingContext2D) {
        var selfMadeContext = (context === undefined);
        if (selfMadeContext) {
            var canvas = document.createElement('canvas');
            document.body.appendChild(canvas);
            context = canvas.getContext("2d");
        }

        this.createNodeBoundaries(function (label) { return GGraph.contextSizer(context, label); });

        if (selfMadeContext)
            document.body.removeChild(canvas);
    }

    static divSizer(div: HTMLDivElement, label: GLabel): IPoint {
        div.innerText = label.content;
        return { x: div.clientWidth, y: div.clientHeight };
    }

    createNodeBoundariesFromDiv(div?: HTMLDivElement) {
        var selfMadeDiv = (div === undefined);
        if (selfMadeDiv) {
            div = document.createElement('div');
            div.setAttribute('style', 'float:left');
            document.body.appendChild(div);
        }
        this.createNodeBoundaries(function (label) { return GGraph.divSizer(div, label); });
        if (selfMadeDiv)
            document.body.removeChild(div);
    }

    static SVGSizer(svg: Element, label: GLabel): IPoint {
        var element = <any>document.createElementNS('http://www.w3.org/2000/svg', 'text');
        element.setAttribute('fill', 'black');
        var textNode = document.createTextNode(label.content);
        element.appendChild(textNode);
        svg.appendChild(element);
        var bbox = element.getBBox();
        var ret = { x: bbox.width, y: bbox.height };
        svg.removeChild(element);
        ret.y -= 6; // Hack: offset miscalculated height.
        if (label.content.length == 1)
            ret.x = ret.y; // Hack: make single-letter nodes round.
        return ret;
    }

    createNodeBoundariesFromSVG(svg?: Element, style?: CSSStyleDeclaration) {
        var selfMadeSvg = (svg === undefined);
        if (selfMadeSvg) {
            svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            if (style !== undefined) {
                (<any>svg).style.font = style.font;
                (<any>svg).style.fontFamily = style.fontFamily;
                (<any>svg).style.fontFeatureSettings = style.fontFeatureSettings;
                (<any>svg).style.fontSize = style.fontSize;
                (<any>svg).style.fontSizeAdjust = style.fontSizeAdjust;
                (<any>svg).style.fontStretch = style.fontStretch;
                (<any>svg).style.fontStyle = style.fontStyle;
                (<any>svg).style.fontVariant = style.fontVariant;
                (<any>svg).style.fontWeight = style.fontWeight;
            }
            document.body.appendChild(svg);
        }
        this.createNodeBoundaries(function (label) { return GGraph.SVGSizer(svg, label); });
        if (selfMadeSvg)
            document.body.removeChild(svg);
    }

    static SVGInContainerSizer(container: HTMLElement, svg: Element, label: GLabel) {
        var element = <any>document.createElementNS('http://www.w3.org/2000/svg', 'text');
        element.setAttribute('fill', 'black');
        var textNode = document.createTextNode(label.content);
        element.appendChild(textNode);
        svg.appendChild(element);
        var bbox = element.getBBox();
        var ret = { x: bbox.width, y: bbox.height };
        svg.removeChild(element);
        return ret;
    }

    createNodeBoundariesForSVGInContainer(container: HTMLElement) {
        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        container.appendChild(svg);
        this.createNodeBoundaries(function (label) { return GGraph.SVGInContainerSizer(container, svg, label); });
        container.removeChild(svg);
    }

    beginLayoutGraph(callback: () => void = null): void {
        var self = this;
        var workerCallback = function (gstr) {
            var gs: GGraph = GGraph.ofJSON(gstr.data);
            self.boundingBox = new GRect({
                x: gs.boundingBox.x - 10, y: gs.boundingBox.y - 10, width: gs.boundingBox.width + 20, height: gs.boundingBox.height + 20
            });
            for (var i = 0; i < gs.nodes.length; i++) {
                var workerNode = gs.nodes[i];
                var myNode = self.getNode(workerNode.id);
                myNode.boundaryCurve = workerNode.boundaryCurve;
                if (myNode.label != null)
                    myNode.label.bounds = workerNode.label.bounds;
            }
            for (var i = 0; i < gs.edges.length; i++) {
                var workerEdge = gs.edges[i];
                var myEdge = self.getEdge(workerEdge.id);
                myEdge.curve = workerEdge.curve;
                if (myEdge.label != null)
                    myEdge.label.bounds = workerEdge.label.bounds;
                if (myEdge.arrowHeadAtSource != null)
                    myEdge.arrowHeadAtSource = workerEdge.arrowHeadAtSource;
                if (myEdge.arrowHeadAtTarget != null)
                    myEdge.arrowHeadAtTarget = workerEdge.arrowHeadAtTarget;
            }
            if (callback != null)
                callback();
        }

        var serialisedGraph = this.getJSON();
        var worker = new Worker('/MSAGL/workerBoot.js');
        worker.addEventListener('message', workerCallback);
        worker.postMessage(serialisedGraph);
    }
}