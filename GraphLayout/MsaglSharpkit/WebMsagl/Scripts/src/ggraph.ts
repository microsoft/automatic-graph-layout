///<reference path="../../Scripts/typings/requirejs/require.d.ts"/>
import M = require('./messages');

/* This file contains classes that can be used to describe a geometry graph. The classes in this file, especially GGraph, are
the main way you use MSAGL_JS. Generally speaking, you construct one of these classes by passing to the constructor a JS object
with the relevant data, based on the declared interfaces. The objects themselves implement the same interfaces, so they can be
copied easily by passing them to a constructor. This is important because these objects need to be shipped across web worker
boundaries, and this approach makes serializing and deserializing them very easy. */

/** An IPoint describes coordinates in 2D space. */
export interface IPoint {
    x: number;
    y: number;
}

/** A GPoint represents coordinates in 2D space. */
export class GPoint implements IPoint {
    x: number;
    y: number;
    constructor(p: any)
    constructor(p: IPoint) {
        this.x = p.x === undefined ? 0 : p.x;
        this.y = p.y === undefined ? 0 : p.y;
    }
    static origin = new GPoint({ x: 0, y: 0 });
    equals(other: IPoint): boolean {
        return this.x == other.x && this.y == other.y;
    }
    /** Vector sum. */
    add(other: IPoint): GPoint {
        return new GPoint({ x: this.x + other.x, y: this.y + other.y });
    }
    /** Vector subtraction. */
    sub(other: IPoint): GPoint {
        return new GPoint({ x: this.x - other.x, y: this.y - other.y });
    }
    /** Scalar division. */
    div(op: number): GPoint {
        return new GPoint({ x: this.x / op, y: this.y / op });
    }
    /** Scalar multiplication. */
    mul(op: number): GPoint {
        return new GPoint({ x: this.x * op, y: this.y * op });
    }
    /** Vector multiplication. */
    vmul(other: IPoint): number {
        return this.x * other.x + this.y * other.y;
    }
    /** Distance squared. */
    dist2(other: IPoint): number {
        var d = this.sub(other);
        return d.x * d.x + d.y * d.y;
    }
    closestParameter(start: GPoint, end: GPoint): number {
        var bc = end.sub(start);
        var ba = this.sub(start);
        var c1 = bc.vmul(ba);
        if (c1 <= 0.1)
            return 0;
        var c2 = bc.vmul(bc);
        if (c2 <= c1 + 0.1)
            return 1;
        return c1 / c2;
    }
    /** If the area is negative then C lies to the right of the line [cornerA, cornerB] or, in other words, the triangle (A, B, C) is oriented clockwise. If it is positive then C lies to the left of the line [A,B], and the triangle (A, B, C) is oriented counter-clockwise. If zero, A, B and C are colinear. */
    public static signedDoubledTriangleArea(cornerA: GPoint, cornerB: GPoint, cornerC: GPoint): number {
        return (cornerB.x - cornerA.x) * (cornerC.y - cornerA.y) - (cornerC.x - cornerA.x) * (cornerB.y - cornerA.y);
    }
}

/** An IRect describes a rectangular region in 2D space. */
export interface IRect {
    x: number;
    y: number;
    width: number;
    height: number;
    getCenter(): GPoint;
    setCenter(p: GPoint): void;
}

/** A GRect represents a rectangular region in 2D space. */
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
    setCenter(p: GPoint) {
        var delta = p.sub(this.getCenter());
        this.x += delta.x;
        this.y += delta.y;
    }
    /** Combines this GRect with another GRect, returning the smallest GRect that contains both of them. */
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
    /** Returns the smalles GRect that contains both this GRect and the given GPoint. */
    extendP(point: GPoint) {
        return this.extend(new GRect({ x: point.x, y: point.y, width: 0, height: 0 }));
    }
}

/** An ICurve describes a curve. This is actually abstract. */
export interface ICurve {
    /** A string that tells what concrete type of curve this is. */
    curvetype: string;
    getCenter(): GPoint;
    getStart(): GPoint;
    getEnd(): GPoint;
    getBoundingBox(): GRect;
    setCenter(p: GPoint): void;
}

/** A GCurve describes a curve. */
export abstract class GCurve implements ICurve {
    /** A string that tells what concrete type of curve this is. */
    curvetype: string;
    constructor(curvetype: string) {
        if (curvetype === undefined)
            throw new Error("Undefined curve type");
        this.curvetype = curvetype;
    }
    abstract getCenter(): GPoint;
    abstract setCenter(p: GPoint): void;
    abstract getStart(): GPoint;
    abstract getEnd(): GPoint;
    abstract getBoundingBox(): GRect;
    /** Constructs a concrete curve, based on the ICurve passed. This behaves similarly to the constructors of other types, but because this also needs to decide on a type, I cannot use the constructor directly. */
    static ofCurve(curve: ICurve): GCurve {
        if (curve == null || curve === undefined)
            return null;
        var ret: GCurve;
        if (curve.curvetype == "Ellipse")
            ret = new GEllipse(<IEllipse><any>curve);
        else if (curve.curvetype == "Line")
            ret = new GLine(<ILine><any>curve);
        else if (curve.curvetype == "Bezier")
            ret = new GBezier(<IBezier><any>curve);
        else if (curve.curvetype == "Polyline")
            ret = new GPolyline(<IPolyline><any>curve);
        else if (curve.curvetype == "SegmentedCurve")
            ret = new GSegmentedCurve(<ISegmentedCurve><any>curve);
        else if (curve.curvetype == "RoundedRect")
            ret = new GRoundedRect(<IRoundedRect><any>curve);
        return ret;
    }
}

/** An ICurve that describes an ellipse. */
export interface IEllipse extends ICurve {
    center: IPoint;
    axisA: IPoint;
    axisB: IPoint;
    parStart: number;
    parEnd: number;
}

/** A GCurve that represents an ellipse. Note that the data format would support ellipses with axes that are not either vertical or horizontal, but in practice the MSAGL engine doesn't deal with those. So axisA and axisB should be vertical or horizontal vectors. Also, you can use parStart and parEnd to define portions of an ellipse. */
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
    getStart(): GPoint {
        return this.center.add(this.axisA.mul(Math.cos(this.parStart))).add(this.axisB.mul(Math.sin(this.parStart)));
    }
    getEnd(): GPoint {
        return this.center.add(this.axisA.mul(Math.cos(this.parEnd))).add(this.axisB.mul(Math.sin(this.parEnd)));
    }
    setCenter(p: GPoint) {
        this.center = new GPoint(p);
    }
    getBoundingBox(): GRect {
        var width = 2 * Math.max(Math.abs(this.axisA.x), Math.abs(this.axisB.x));
        var height = 2 * Math.max(Math.abs(this.axisA.y), Math.abs(this.axisB.y));
        var p = this.center.sub({ x: width / 2, y: height / 2 });
        return new GRect({ x: p.x, y: p.y, width: width, height: height });
    }
    /** A helper method that makes a complete ellipse with the given width and height. */
    static make(width: number, height: number): GEllipse {
        return new GEllipse({ center: GPoint.origin, axisA: new GPoint({ x: width / 2, y: 0 }), axisB: new GPoint({ x: 0, y: height / 2 }), parStart: 0, parEnd: Math.PI * 2 });
    }
}

/** An ILine describes an ICurve that is a segment. */
export interface ILine {
    start: IPoint;
    end: IPoint;
}

/** A GLine represents a GCurve that is a segment. */
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
    getStart(): GPoint {
        return this.start;
    }
    getEnd(): GPoint {
        return this.end;
    }
    setCenter(p: GPoint) {
        var delta = p.sub(this.getCenter());
        this.start = this.start.add(delta);
        this.end = this.end.add(delta);
    }
    getBoundingBox(): GRect {
        var ret = new GRect({ x: this.start.x, y: this.start.y, width: 0, height: 0 });
        ret = ret.extendP(this.end);
        return ret;
    }
}

/** An IPolyline describes an ICurve that is a sequence of contiguous segments. */
export interface IPolyline {
    start: IPoint;
    points: IPoint[];
    closed: boolean;
}

/** A GPolyline represents a GCurve that is a sequence of contiguous segments. */
export class GPolyline extends GCurve implements IPolyline {
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
    getStart(): GPoint {
        return this.start;
    }
    getEnd(): GPoint {
        return this.points[this.points.length - 1];
    }
    setCenter(p: GPoint) {
        var delta = p.sub(this.getCenter());
        for (var i = 0; i < this.points.length; i++)
            this.points[i] = this.points[i].add(delta);
    }
    getBoundingBox(): GRect {
        var ret = new GRect({ x: this.points[0].x, y: this.points[0].y, height: 0, width: 0 });
        for (var i = 1; i < this.points.length; i++)
            ret = ret.extendP(this.points[i]);
        return ret;
    }
}

/** An IRoundedRect describes an ICurve that is a rectangle that may have rounded corners. */
export interface IRoundedRect {
    bounds: IRect;
    radiusX: number;
    radiusY: number;
}

/** A GRoundedRect represents a GCurve that is a rectangle that may have rounded corners. Technically, this is just a handy helper... the same shape can be represented with a composition of simpler objects. */
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
    getStart(): GPoint {
        throw new Error("getStart not supported by RoundedRect");
    }
    getEnd(): GPoint {
        throw new Error("getEnd not supported by RoundedRect");
    }
    setCenter(p: GPoint) {
        this.bounds.setCenter(p);
    }
    getBoundingBox(): GRect {
        return this.bounds;
    }
    /** Converts this to a GSegmentedCurve (a composition of simpler objects). */
    getCurve(): GSegmentedCurve {
        var segments: GCurve[] = [];
        var axisA = new GPoint({ x: this.radiusX, y: 0 });
        var axisB = new GPoint({ x: 0, y: this.radiusY });
        var innerBounds = new GRect({ x: this.bounds.x + this.radiusX, y: this.bounds.y + this.radiusY, width: this.bounds.width - this.radiusX * 2, height: this.bounds.height - this.radiusY * 2 });
        segments.push(new GEllipse({ axisA: axisA, axisB: axisB, center: new GPoint({ x: innerBounds.x, y: innerBounds.y }), parStart: Math.PI, parEnd: Math.PI * 3 / 2 }));
        segments.push(new GLine({ start: new GPoint({ x: innerBounds.x, y: this.bounds.y }), end: new GPoint({ x: innerBounds.x + innerBounds.width, y: this.bounds.y }) }));
        segments.push(new GEllipse({ axisA: axisA, axisB: axisB, center: new GPoint({ x: innerBounds.x + innerBounds.width, y: innerBounds.y }), parStart: Math.PI * 3 / 2, parEnd: 2 * Math.PI }));
        segments.push(new GLine({ start: new GPoint({ x: this.bounds.x + this.bounds.width, y: innerBounds.y }), end: new GPoint({ x: this.bounds.x + this.bounds.width, y: innerBounds.y + innerBounds.height }) }));
        segments.push(new GEllipse({ axisA: axisA, axisB: axisB, center: new GPoint({ x: innerBounds.x + innerBounds.width, y: innerBounds.y + innerBounds.height }), parStart: 0, parEnd: Math.PI / 2 }));
        segments.push(new GLine({ start: new GPoint({ x: innerBounds.x + innerBounds.width, y: this.bounds.y + this.bounds.height }), end: new GPoint({ x: innerBounds.x, y: this.bounds.y + this.bounds.height }) }));
        segments.push(new GEllipse({ axisA: axisA, axisB: axisB, center: new GPoint({ x: innerBounds.x, y: innerBounds.y + innerBounds.height }), parStart: Math.PI / 2, parEnd: Math.PI }));
        segments.push(new GLine({ start: new GPoint({ x: this.bounds.x, y: innerBounds.y + innerBounds.height }), end: new GPoint({ x: this.bounds.x, y: innerBounds.y }) }));
        return new GSegmentedCurve({ segments: segments });
    }
}

/** An IBezier is an ICurve that describes a Bezier segment. */
export interface IBezier {
    start: IPoint;
    p1: IPoint;
    p2: IPoint;
    p3: IPoint;
}

/** A GBezier is a GCurve that represents a Bezier segment. */
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
    getStart(): GPoint {
        return this.start;
    }
    getEnd(): GPoint {
        return this.p3;
    }
    setCenter(p: GPoint) {
        var delta = p.sub(this.getCenter());
        this.start = this.start.add(delta);
        this.p1 = this.p1.add(delta);
        this.p2 = this.p2.add(delta);
        this.p3 = this.p3.add(delta);
    }
    getBoundingBox(): GRect {
        var ret = new GRect({ x: this.start.x, y: this.start.y, width: 0, height: 0 });
        ret = ret.extendP(this.p1);
        ret = ret.extendP(this.p2);
        ret = ret.extendP(this.p3);
        return ret;
    }
}

/** An ISegmentedCurve is an ICurve that's actually a sequence of simpler ICurves. */
export interface ISegmentedCurve {
    segments: ICurve[];
}

/** A GSegmentedCurve is a GCurve that's actually a sequence of simpler GCurves. */
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
    getStart(): GPoint {
        return this.segments[0].getStart();
    }
    getEnd(): GPoint {
        return this.segments[this.segments.length - 1].getEnd();
    }
    setCenter(p: GPoint) {
        throw new Error("setCenter not supported by SegmentedCurve");
    }
    getBoundingBox(): GRect {
        var ret = this.segments[0].getBoundingBox();
        for (var i = 1; i < this.segments.length; i++)
            ret = ret.extend(this.segments[i].getBoundingBox());
        return ret;
    }
}

/** An IElement is a graph element (i.e. a node or edge). This interface describes properties that are shared by all elements. */
export interface IElement {
    tooltip: string;
}

/** An ILabel describes a label. */
export interface ILabel extends IElement {
    bounds: IRect;
    content: string;
    fill: string;
}

/** A GLabel represents a label. */
export class GLabel implements ILabel {
    bounds: IRect;
    content: string;
    fill: string;
    tooltip: string;
    /** For this constructor, you can also pass a string. This will create a label with that string as its text. */
    constructor(label: any)
    constructor(label: ILabel) {
        if (typeof (label) == "string")
            this.content = <string><any>label;
        else {
            this.bounds = label.bounds == undefined || label.bounds == GRect.zero ? GRect.zero : new GRect(label.bounds);
            this.tooltip = label.tooltip === undefined ? null : label.tooltip;
            this.content = label.content;
            this.fill = label.fill === undefined ? "Black" : label.fill;
        }
    }
}

/** A GShape is the shape of a node's boundary, in an abstract sense (as opposed to a GCurve, which is a concrete curve). You can think of this as a description of how a GCurve should eventually be built to correctly encircle a node's label. */
export class GShape {
    /** Helper that gives you a rectangular shape. */
    static GetRect(): GShape {
        var ret = new GShape();
        ret.shape = "rect";
        return ret;
    }
    /** Helper that gives you a rectangular shape with rounded corners. */
    static GetRoundedRect(radiusX?: number, radiusY?: number): GShape {
        var ret = new GShape();
        ret.shape = "rect";
        ret.radiusX = radiusX === undefined ? 5 : radiusX;
        ret.radiusY = radiusY === undefined ? 5 : radiusY;
        return ret;
    }
    /** Helper that gives you a rectangular shape with rounded corners, with radii that are based on the label size. */
    static GetMaxRoundedRect(): GShape {
        var ret = new GShape();
        ret.shape = "rect";
        ret.radiusX = null;
        ret.radiusY = null;
        return ret;
    }
    static RectShape = "rect";
    /** Helper that gives you a shape from some special strings (rect, roundedrect and maxroundedrect). */
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
    /** This number indicates that the node boundary curve should be replicated and shifted to provide a folder-like appearance. */
    multi: number;
}

/** An INode describes a graph node. */
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

/** A GNode represents a graph node. */
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
        this.fill = node.fill === undefined ? "White" : node.fill;
        this.stroke = node.stroke === undefined ? "Black" : node.stroke;
    }
    /** Type check: returns true if this is actually a cluster. */
    isCluster() {
        return (<any>this).children !== undefined;
    }
}

export interface IClusterMargin {
    bottom: number;
    top: number;
    left: number;
    right: number;
    minWidth: number;
    minHeight: number;
}

export class GClusterMargin implements IClusterMargin {
    bottom: number;
    top: number;
    left: number;
    right: number;
    minWidth: number;
    minHeight: number;
    constructor(clusterMargin: any)
    constructor(clusterMargin: IClusterMargin) {
        this.bottom = clusterMargin.bottom == null ? 0 : clusterMargin.bottom;
        this.top = clusterMargin.top == null ? 0 : clusterMargin.top;
        this.left = clusterMargin.left == null ? 0 : clusterMargin.left;
        this.right = clusterMargin.right == null ? 0 : clusterMargin.right;
        this.minWidth = clusterMargin.minWidth == null ? 0 : clusterMargin.minWidth;
        this.minHeight = clusterMargin.minHeight == null ? 0 : clusterMargin.minHeight;
    }
}

/** An ICluster is an INode that's actually a cluster. */
export interface ICluster extends INode {
    children: INode[];
    margin: IClusterMargin;
}

/** A GCluster is a GNode that's actually a cluster. Note that if you add children via the constructor, you'll lose custom fields. If your nodes have custom fields, use CGluster.addChild instead. */
export class GCluster extends GNode implements ICluster {
    children: GNode[];
    margin: IClusterMargin;
    constructor(cluster: any)
    constructor(cluster: ICluster) {
        super(cluster);
        this.margin = new GClusterMargin(cluster.margin == null ? {} : cluster.margin);
        this.children = [];
        if (cluster.children != null)
            for (var i = 0; i < cluster.children.length; i++)
                if ((<GCluster>cluster.children[i]).children !== undefined)
                    this.children.push(new GCluster(<ICluster>cluster.children[i]));
                else
                    this.children.push(new GNode(cluster.children[i]));
    }

    addChild(n: GNode) {
        this.children.push(n);
    }
}

/** An IArrowHead describes an edge's arrowhead. */
export interface IArrowHead {
    start: IPoint;
    end: IPoint;
    closed: boolean;
    fill: boolean;
    dash: string;
    style: string;
}

/** A GArrowHead represents an edge's arrowhead. */
export class GArrowHead implements IArrowHead {
    start: IPoint;
    end: IPoint;
    closed: boolean;
    fill: boolean;
    dash: string;
    style: string; // standard|tee|diamond
    constructor(arrowHead: any)
    constructor(arrowHead: IArrowHead) {
        this.start = arrowHead.start == undefined ? null : arrowHead.start;
        this.end = arrowHead.end == undefined ? null : arrowHead.end;
        this.closed = arrowHead.closed == undefined ? false : arrowHead.closed;
        this.fill = arrowHead.fill == undefined ? false : arrowHead.fill;
        this.dash = arrowHead.dash == undefined ? null : arrowHead.dash;
        this.style = arrowHead.style == undefined ? "standard" : arrowHead.style;
    }
    /** Standard arrowhead (empty, open). */
    static standard: GArrowHead = new GArrowHead({});
    /** Closed arrowhead. */
    static closed: GArrowHead = new GArrowHead({ closed: true });
    /** Filled arrowhead. */
    static filled: GArrowHead = new GArrowHead({ closed: true, fill: true });
    /** Tee-shaped arrowhead. The closed and fill flags have no effect on this arrowhead. */
    static tee: GArrowHead = new GArrowHead({ style: "tee" });
    /** Diamond-shaped arrowhead (empty). The closed flag has no effect on this arrowhead. */
    static diamond: GArrowHead = new GArrowHead({ style: "diamond" });
    /** Diamond-shaped arrowhead (filled). The closed flag has no effect on this arrowhead. */
    static diamondFilled: GArrowHead = new GArrowHead({ style: "diamond", fill: true });
}

/** An IEdge describes a graph edge. */
export interface IEdge extends IElement {
    id: string;
    source: string;
    target: string;
    label: ILabel;
    arrowHeadAtTarget: GArrowHead;
    arrowHeadAtSource: GArrowHead;
    thickness: number;
    dash: string;
    curve: ICurve;
    stroke: string;
}

/** A GEdge represents a graph edge. */
export class GEdge implements IEdge {
    id: string;
    tooltip: string;
    source: string;
    target: string;
    label: GLabel;
    arrowHeadAtTarget: GArrowHead;
    arrowHeadAtSource: GArrowHead;
    thickness: number;
    dash: string;
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
        this.dash = edge.dash == undefined ? null : edge.dash;
        this.curve = edge.curve === undefined ? null : GCurve.ofCurve(edge.curve);
        this.stroke = edge.stroke === undefined ? "Black" : edge.stroke;
    }
}

/** An IPlaneTransformation describes a setting for applying a transformation to the graph. */
export interface IPlaneTransformation {
    m00: number;
    m01: number;
    m02: number;
    m10: number;
    m11: number;
    m12: number;
}

/** A GPlaneTransformation represents a setting for applying a transformation to the graph. */
export class GPlaneTransformation implements IPlaneTransformation {
    m00: number;
    m01: number;
    m02: number;
    m10: number;
    m11: number;
    m12: number;
    /** Note that you can also pass an object that has a field named "rotation" with a numerical value, to create a transformation that corresponds to a rotation of that value (in radians). */
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
    /** Helper: the default transformation, which orientates the graph top-to-bottom. */
    static defaultTransformation = new GPlaneTransformation({ m00: -1, m01: 0, m02: 0, m10: 0, m11: -1, m12: 0 });
    /** Helper: a transformation that orientates the graph left-to-right. */
    static ninetyDegreesTransformation = new GPlaneTransformation({ m00: 0, m01: -1, m02: 0, m10: 1, m11: 0, m12: 0 });
}

/** An IUpDownConstraint describes a setting to put an Up-Down constraint on a graph layout. */
export interface IUpDownConstraint {
    upNode: string;
    downNode: string;
}

/** An GUpDownConstraint represents a setting to put an Up-Down constraint on a graph layout. */
export class GUpDownConstraint {
    upNode: string;
    downNode: string;
    constructor(upDownConstraint: any)
    constructor(upDownConstraint: IUpDownConstraint) {
        this.upNode = upDownConstraint.upNode;
        this.downNode = upDownConstraint.downNode;
    }
}

/** ISettings describes a graph's layout settings. */
export interface ISettings {
    /** The layout should be one of the static fields of GSettings. */
    layout: string;
    transformation: IPlaneTransformation;
    routing: string;
    aspectRatio: number;
    upDownConstraints: IUpDownConstraint[];
    /** Specific to MDS layout. */
    iterationsWithMajorization: number;
}

/** GSettings represents a graph's layout settings. */
export class GSettings implements ISettings {
    /** The layout should be one of the static fields of GSettings. */
    layout: string;
    transformation: GPlaneTransformation;
    routing: string;
    aspectRatio: number;
    upDownConstraints: GUpDownConstraint[];
    iterationsWithMajorization: number;
    constructor(settings: any)
    constructor(settings: ISettings) {
        this.layout = settings.layout === undefined ? GSettings.sugiyamaLayout : settings.layout;
        this.transformation = settings.transformation === undefined ? GPlaneTransformation.defaultTransformation : settings.transformation;
        this.routing = settings.routing === undefined ? GSettings.sugiyamaSplinesRouting : settings.routing;
        this.aspectRatio = settings.aspectRatio === undefined ? 0.0 : settings.aspectRatio;
        this.upDownConstraints = [];
        if (settings.upDownConstraints !== undefined) {
            for (var i = 0; i < settings.upDownConstraints.length; i++) {
                var upDownConstraint = new GUpDownConstraint(settings.upDownConstraints[i]);
                this.upDownConstraints.push(upDownConstraint);
            }
        }
        this.iterationsWithMajorization = settings.iterationsWithMajorization == undefined ? 30 : settings.iterationsWithMajorization;
    }

    static sugiyamaLayout = "sugiyama";
    static mdsLayout = "mds";

    static splinesRouting = "splines";
    static splinesBundlingRouting = "splinesbundling";
    static straightLineRouting = "straightline";
    static sugiyamaSplinesRouting = "sugiyamasplines";
    static rectilinearRouting = "rectilinear";
    static rectilinearToCenterRouting = "rectilineartocenter";
}

/** An IGraph describes a graph, plus its layout settings. */
export interface IGraph {
    nodes: GNode[];
    edges: GEdge[];
    boundingBox: IRect;
    settings: ISettings;
}

/** This is a helper for internal use, which decorates nodes with their edges. */
class GNodeInternal {
    node: GNode;
    outEdges: string[];
    inEdges: string[];
    selfEdges: string[];
}

/** This is a helper for internal use, which decorates edges with their polylines. */
class GEdgeInternal {
    edge: GEdge;
    polyline: GPoint[];
}

/** A MoveElementToken stores the information needed to perform dragging operations on a geometry element. */
class MoveElementToken {
}

/** This token stores the information needed to move a node: a reference to the node, and the original coordinates of the node. */
class MoveNodeToken extends MoveElementToken {
    public node: GNode;
    public originalBoundsCenter: GPoint;
}

/** This token stores the information needed to move an edge label: a reference to the label, and the original coordinates of the label. */
class MoveEdgeLabelToken extends MoveElementToken {
    public label: GLabel;
    public originalLabelCenter: GPoint;
}

/** This token stores the information needed to move an edge control point: a reference to the edge, the original polyline, and the point on the original polyline that's being edited. */
class MoveEdgeToken extends MoveElementToken {
    public edge: GEdge;
    public originalPolyline: GPoint[];
    public originalPoint: GPoint;
}

/** This class is a simple implementation of a callback set. Note that there are libraries to do this, e.g. jQuery, but I'd rather not acquire a dependency on jQuery just for this. */
export class CallbackSet<T> {
    private callbacks: ((par: T) => void)[] = [];

    /** Adds a callback to the list. If you want to be able to remove the callback later, you'll need to store a reference to it. */
    public add(callback: (par: T) => void) {
        if (callback == null)
            return;
        this.callbacks.push(callback);
    }
    /** Removes a callback from the list. */
    public remove(callback: (par: T) => void) {
        if (callback == null)
            return;
        var idx = this.callbacks.indexOf(callback);
        if (idx >= 0)
            this.callbacks.splice(idx);
    }
    /** Fires all of the callbacks, with the given parameter. */
    public fire(par?: T) {
        for (var i = 0; i < this.callbacks.length; i++)
            this.callbacks[i](par);
    }
    public count(): number {
        return this.callbacks.length;
    }
}

/** A GGraph represents a graph, plus its layout settings, and provides methods to manipulate it. */
export class GGraph implements IGraph {
    public static EdgeEditCircleRadius: number = 8;

    /** Maps node IDs to GNodeInternal instances. */
    private nodesMap: { [id: string]: GNodeInternal };
    /** Maps edge IDs to GEdge instances. */
    private edgesMap: { [id: string]: GEdgeInternal };
    nodes: GNode[];
    edges: GEdge[];
    boundingBox: GRect;
    settings: GSettings;

    constructor() {
        this.nodesMap = {};
        this.edgesMap = {};
        this.nodes = [];
        this.edges = [];
        this.boundingBox = GRect.zero;
        this.settings = new GSettings({ transformation: { m00: -1, m01: 0, m02: 0, m10: 0, m11: -1, m12: 0 } });
    }

    /** Add a node to the node map. Recursively map cluster children. */
    private mapNode(node: GNode): void {
        this.nodesMap[node.id] = <GNodeInternal>{ node: node, outEdges: [], inEdges: [], selfEdges: [] };
        let children = (<GCluster>node).children;
        if (children !== undefined)
            for (let child of children)
                this.mapNode(child);
    }

    /** Add a node to the graph. */
    addNode(node: GNode): void {
        this.nodes.push(node);
        this.mapNode(node);
        let children = (<GCluster>node).children;
        if (children !== undefined)
            for (let child of children)
                this.mapNode(child);
    }

    /** Returns a node, given its ID. */
    getNode(id: string): GNode {
        var nodeInternal = this.nodesMap[id];
        return nodeInternal == null ? null : nodeInternal.node;
    }

    /** Gets a node's in-edges, given its ID. */
    getInEdges(nodeId: string): string[] {
        var nodeInternal = <GNodeInternal>this.nodesMap[nodeId];
        return nodeInternal == null ? null : nodeInternal.inEdges;
    }

    /** Gets a node's out-edges, given its ID. */
    getOutEdges(nodeId: string): string[] {
        var nodeInternal = <GNodeInternal>this.nodesMap[nodeId];
        return nodeInternal == null ? null : nodeInternal.outEdges;
    }

    /** Gets a node's self-edges, given its ID. */
    getSelfEdges(nodeId: string): string[] {
        var nodeInternal = <GNodeInternal>this.nodesMap[nodeId];
        return nodeInternal == null ? null : nodeInternal.selfEdges;
    }

    /** Adds an edge to the graph. The nodes must exist! */
    addEdge(edge: GEdge): void {
        if (this.nodesMap[edge.source] == null)
            throw new Error("Undefined node " + edge.source);
        if (this.nodesMap[edge.target] == null)
            throw new Error("Undefined node " + edge.target);
        if (this.edgesMap[edge.id] != null)
            throw new Error("Edge " + edge.id + " already exists");
        this.edgesMap[edge.id] = <GEdgeInternal>{ edge: edge, polyline: null };
        this.edges.push(edge);
        if (edge.source == edge.target)
            (<GNodeInternal>this.nodesMap[edge.source]).selfEdges.push(edge.id);
        else {
            (<GNodeInternal>this.nodesMap[edge.source]).outEdges.push(edge.id);
            (<GNodeInternal>this.nodesMap[edge.target]).inEdges.push(edge.id);
        }
    }

    /** Returns an edge, given its ID. */
    getEdge(id: string): GEdge {
        var edgeInternal = this.edgesMap[id];
        return edgeInternal == null ? null : edgeInternal.edge;
    }

    /** Gets the JSON representation of the graph. */
    getJSON(): string {
        var igraph: IGraph = { nodes: this.nodes, edges: this.edges, boundingBox: this.boundingBox, settings: this.settings };
        var ret: string = JSON.stringify(igraph);
        return ret;
    }

    /** Rebuilds the GGraph out of a JSON string. */
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

    /** Creates boundaries for all nodes, based on their shape and label. */
    private createNodeBoundariesRec(node: GNode, sizer?: (label: GLabel, owner: IElement) => IPoint) {
        var cluster = <GCluster>node;

        if (node.boundaryCurve == null) {
            if (node.label != null && node.label.bounds == GRect.zero && sizer !== undefined) {
                var labelSize = sizer(node.label, node);
                node.label.bounds = new GRect({ x: 0, y: 0, width: labelSize.x, height: labelSize.y });
            }
            // Do not create boundaries for clusters. The layout algorithm will handle that.
            if (cluster.children == null) {
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
        }

        if (cluster.children != null)
            for (var i = 0; i < cluster.children.length; i++)
                this.createNodeBoundariesRec(cluster.children[i], sizer);
    }

    /** Creates the node boundaries for nodes that don't have one. If the node has a label, it will first compute the label's size based on the provided size function, and then create an appropriate boundary. There are several predefined sizers, corresponding to the most common ways of sizing text. */
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

    /** A sizer function that calculates text sizes as Canvas text. */
    static contextSizer(context: CanvasRenderingContext2D, label: GLabel): IPoint {
        return { x: context.measureText(label.content).width, y: parseInt(context.font) };
    }

    /** Creates node boundaries using the contextSizer on the given Canvas context. */
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

    /** A sizer function that calculates text size as HTML text. */
    static divSizer(div: HTMLDivElement, label: GLabel): IPoint {
        div.innerText = label.content;
        return { x: div.clientWidth, y: div.clientHeight };
    }

    /** Creates node boundaries using the divSizer on the given div. */
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

    /** A sizer function that calculates text size as SVG text. */
    static SVGSizer(svg: Element, label: GLabel): IPoint {
        var element = <any>document.createElementNS('http://www.w3.org/2000/svg', 'text');
        element.setAttribute('fill', 'black');
        var textNode = document.createTextNode(label.content);
        element.appendChild(textNode);
        svg.appendChild(element);
        var bbox = element.getBBox();
        var ret = { x: bbox.width, y: bbox.height };
        svg.removeChild(element);
        if (ret.y > 6)
            ret.y -= 6; // Hack: offset miscalculated height.
        if (label.content.length == 1)
            ret.x = ret.y; // Hack: make single-letter nodes round.
        return ret;
    }

    /** Creates node boundaries using the svgSizer on the given SVG element. You can also not give this function the SVG element, in which case it will use a temporary one. In this case, you can give it a style declaration that will be used for the temporary SVG element.*/
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

    /** Creates node boundaries for text that will be SVG text, placed in an SVG element in the given HTML container. Warning: you are responsible for the container to be valid for this purpose. E.g. it should not be hidden, or the results won't be valid.*/
    createNodeBoundariesForSVGInContainer(container: HTMLElement) {
        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        container.appendChild(svg);
        this.createNodeBoundaries(function (label) { return GGraph.SVGSizer(svg, label); });
        container.removeChild(svg);
    }

    /** Creates node boundaries for text that will be SVG text, placed in an SVG element at the top level of the DOM. This guarantees that the SVG element is not hidden, but it will not use the same style as the real container. You can pass a style to use for font styling. */
    createNodeBoundariesForSVGWithStyle(style: CSSStyleDeclaration) {
        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        if (style != null) {
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
        this.createNodeBoundaries((label, owner) => GGraph.SVGSizer(svg, label));
        document.body.removeChild(svg);
    }

    /** The web worker that performs layout operations. There's at most one of these at any given time. */
    private worker: Worker = null;
    public working: boolean = false;

    /** Aborts a layout operation, if there is one ongoing. You should call this to dispose the worker when you are no longer using the graph, because there is an apparent bug in Chrome that causes workers not to be garbage collected unless they are terminated. If this Chrome issue gets fixed, then calling stopLayoutGraph for disposal becomes unnecessary. */
    public stopLayoutGraph(): void {
        if (this.worker != null) {
            this.worker.terminate();
            this.worker = null;
        }
        this.setWorking(false);
    }

    private workerCallback(msg: MessageEvent) {
        var data: M.Response = msg.data;
        if (data.msgtype == "RunLayout") {
            var runLayoutMsg = <M.Res_RunLayout>data;
            // data.graph contains a string that is the JSON string for an IGraph. Deserialize it into a GGraph. This GGraph doesn't directly replace myself; I just want to copy its curves over mine. This way, the user can keep using this GGraph.
            var gs: GGraph = GGraph.ofJSON(runLayoutMsg.graph);
            // Copy its bounding box to me, extending the margins a little bit.
            this.boundingBox = new GRect({
                x: gs.boundingBox.x - 10, y: gs.boundingBox.y - 10, width: gs.boundingBox.width + 20, height: gs.boundingBox.height + 20
            });
            // Copy all of the curves of the nodes, including the label boundaries.
            for (var nodeId in gs.nodesMap) {
                var workerNode = gs.nodesMap[nodeId];
                var myNode = this.getNode(nodeId);
                myNode.boundaryCurve = workerNode.node.boundaryCurve;
                if (myNode.label != null)
                    myNode.label.bounds = workerNode.node.label.bounds;
            }
            // Copy all of the curves of the edges, including the label boundaries and the arrowheads.
            for (var edgeId in gs.edgesMap) {
                var workerEdge = gs.edgesMap[edgeId];
                var myEdge = this.getEdge(edgeId);
                myEdge.curve = workerEdge.edge.curve;
                if (myEdge.label != null)
                    myEdge.label.bounds = workerEdge.edge.label.bounds;
                if (myEdge.arrowHeadAtSource != null)
                    myEdge.arrowHeadAtSource = workerEdge.edge.arrowHeadAtSource;
                if (myEdge.arrowHeadAtTarget != null)
                    myEdge.arrowHeadAtTarget = workerEdge.edge.arrowHeadAtTarget;
            }
            // Invoke the user callbacks.
            this.layoutCallbacks.fire();
        }
        else if (data.msgtype == "RouteEdges") {
            var routeEdgesMsg = <M.Res_RouteEdges>data;
            // data.graph contains a string that is the JSON string for an IGraph. Deserialize it into a GGraph. This GGraph doesn't directly replace myself; I just want to copy its curves over mine. This way, the user can keep using this GGraph.
            var gs: GGraph = GGraph.ofJSON(routeEdgesMsg.graph);
            // Copy all of the curves of the edges, including the label boundaries and the arrowheads.
            for (var edgeId in gs.edgesMap) {
                // data.graph contains a string that is the JSON string for an IGraph. Deserialize it into a GGraph. This GGraph doesn't directly replace myself; I just want to copy its curves over mine. This way, the user can keep using this GGraph.
                var gs: GGraph = GGraph.ofJSON(routeEdgesMsg.graph);
                var workerEdge = gs.edgesMap[edgeId];
                if (routeEdgesMsg.edges == null || routeEdgesMsg.edges.length == 0 || routeEdgesMsg.edges.indexOf(workerEdge.edge.id) >= 0) {
                    var edgeInternal = this.edgesMap[edgeId];
                    var myEdge = edgeInternal.edge;
                    myEdge.curve = workerEdge.edge.curve;
                    edgeInternal.polyline = null;
                    if (myEdge.label != null)
                        myEdge.label.bounds = workerEdge.edge.label.bounds;
                    if (myEdge.arrowHeadAtSource != null)
                        myEdge.arrowHeadAtSource = workerEdge.edge.arrowHeadAtSource;
                    if (myEdge.arrowHeadAtTarget != null)
                        myEdge.arrowHeadAtTarget = workerEdge.edge.arrowHeadAtTarget;
                }
            }
            // Invoke the user callbacks.
            this.edgeRoutingCallbacks.fire(routeEdgesMsg.edges);
        }
        else if (data.msgtype == "SetPolyline") {
            var setPolylineMsg = <M.Res_SetPolyline>data;
            var edge = this.edgesMap[setPolylineMsg.edge].edge;
            var curve = JSON.parse(setPolylineMsg.curve);
            curve = GCurve.ofCurve(curve);
            edge.curve = curve;
            if (setPolylineMsg.sourceArrowHeadStart != null)
                edge.arrowHeadAtSource.start = JSON.parse(setPolylineMsg.sourceArrowHeadStart);
            if (setPolylineMsg.sourceArrowHeadEnd != null)
                edge.arrowHeadAtSource.end = JSON.parse(setPolylineMsg.sourceArrowHeadEnd);
            if (setPolylineMsg.targetArrowHeadStart != null)
                edge.arrowHeadAtTarget.start = JSON.parse(setPolylineMsg.targetArrowHeadStart);
            if (setPolylineMsg.targetArrowHeadEnd != null)
                edge.arrowHeadAtTarget.end = JSON.parse(setPolylineMsg.targetArrowHeadEnd);
            this.edgeRoutingCallbacks.fire([edge.id]);
        }
        this.setWorking(false);
    }

    /** Ensures that a layout worker is present and ready. */
    private ensureWorkerReady(): void {
        // Stop any currently active layout operations.
        if (this.working)
            this.stopLayoutGraph();
        if (this.worker == null) {
            this.worker = new Worker(require.toUrl("./workerBoot.js"));
            // Hook up to messages from the worker.
            var that = this;
            this.worker.addEventListener('message', ev => this.workerCallback(ev));
        }
    }

    /** Callbacks you can set to be notified when a layout operation is starting. */
    public layoutStartedCallbacks: CallbackSet<void> = new CallbackSet<void>();
    /** Callbacks you can set to be notified when a layout operation is complete. */
    public layoutCallbacks: CallbackSet<void> = new CallbackSet<void>();
    /** Callbacks you can set to be notified when an edge routing operation is complete. The set of routed edges is passed as a parameter. If it is null, it means that all edges in the graph were routed. Note that edge routing may happen after being invoked by the user program, but it may also happen as a consequence of moving a node. */
    public edgeRoutingCallbacks: CallbackSet<string[]> = new CallbackSet<string[]>();
    /** Callbacks you can set to be notified when the engine starts an asynchronous operation. */
    public workStartedCallbacks: CallbackSet<void> = new CallbackSet<void>();
    /** Callbacks you can set to be notified when the engine ends an asynchronous operation. */
    public workStoppedCallbacks: CallbackSet<void> = new CallbackSet<void>();

    private setWorking(working: boolean) {
        this.working = working;
        if (working)
            this.workStartedCallbacks.fire();
        else
            this.workStoppedCallbacks.fire();
    }

    /** Starts running layout on the graph. The layout callback will be invoked when the layout operation is done. */
    public beginLayoutGraph(): void {
        this.ensureWorkerReady();
        this.setWorking(true);
        this.layoutStartedCallbacks.fire();
        // Serialize the graph.
        var serialisedGraph = this.getJSON();
        // Send the worker the serialized graph to layout.
        var msg: M.Req_RunLayout = { msgtype: "RunLayout", graph: serialisedGraph };
        this.worker.postMessage(msg);
    }

    /** Starts running edge routing on the graph. The edge routing callback will be invoked when the edge routing operation is done. */
    public beginEdgeRouting(edges?: string[]): void {
        this.ensureWorkerReady();
        this.setWorking(true);
        // Serialize the graph.
        var serialisedGraph = this.getJSON();
        // Send the worker the serialized graph to layout.
        var msg: M.Req_RouteEdges = { msgtype: "RouteEdges", graph: serialisedGraph, edges: edges };
        this.worker.postMessage(msg);
    }

    /** Starts generating an edge from its current control points. The edge routing callback will be invoked when the operation is done. */
    public beginRebuildEdgeCurve(edge: string): void {
        this.ensureWorkerReady();
        this.setWorking(true);
        var serialisedGraph = this.getJSON();
        var points = this.edgesMap[edge].polyline;
        var msg: M.Req_SetPolyline = { msgtype: "SetPolyline", graph: serialisedGraph, edge: edge, polyline: JSON.stringify(points) };
        this.worker.postMessage(msg);
    }

    /** A list of the current move tokens. Note that currently only one object can be moved at the same time. This may change in the future. */
    private moveTokens: MoveElementToken[] = [];

    /** Returns the control point of the given edge that's within an editing circle radius from the given point. If more than one such points exist, the one which has the closest centre is returned. If no such point exists, returns null. */
    public getControlPointAt(edge: GEdge, point: GPoint): GPoint {
        var points = this.getPolyline(edge.id);
        // Iterate over points, comparing the squared distance.
        var ret = points[0];
        var dret = ret.dist2(point);
        for (var i = 1; i < points.length; i++) {
            var d = points[i].dist2(point);
            if (d < dret) {
                ret = points[i];
                dret = d;
            }
        }
        // Compare the closest distance with the edge edit circle radius.
        if (dret > GGraph.EdgeEditCircleRadius * GGraph.EdgeEditCircleRadius)
            ret = null;
        return ret;
    }

    /** This function selects an element for moving. After calling this, you can call moveElement to apply a delta to the position of the element. You can select multiple elements and then move them all in one operation, but you should not move elements between selections (in that case, call endMoveElements and then select them all again). */
    public startMoveElement(el: IElement, mousePoint: GPoint) {
        if (el instanceof GNode) {
            // In the case of nodes, I need to make a note of the original center location of the node.
            var node = <GNode>el;
            var mnt = new MoveNodeToken();
            mnt.node = node;
            mnt.originalBoundsCenter = node.boundaryCurve.getCenter();
            this.moveTokens.push(mnt);
        }
        else if (el instanceof GLabel) {
            // In the case of labels (which means edge labels, as node labels cannot be moved independently), I need to make a note of the original center location of the label.
            var label = <GLabel>el;
            var melt = new MoveEdgeLabelToken();
            melt.label = label;
            melt.originalLabelCenter = label.bounds.getCenter();
            this.moveTokens.push(melt);
        }
        else if (el instanceof GEdge) {
            // In the case of edges (which means an edge control point, as edges cannot be moved as a whole), I need to make a note of the original polyline, and the point of that polyline that's being moved.
            var edge = <GEdge>el;
            var point = this.getControlPointAt(edge, mousePoint);
            if (point != null) {
                var met = new MoveEdgeToken();
                met.edge = edge;
                met.originalPoint = point;
                met.originalPolyline = this.getPolyline(edge.id);
                this.moveTokens.push(met);
            }
        }
    }

    /** This function applies a delta to the positions of the element(s) currently selected for moving. */
    public moveElements(delta: GPoint) {
        for (var i in this.moveTokens) {
            var token = this.moveTokens[i];
            if (token instanceof MoveNodeToken) {
                // If I'm moving a node, I'll need to apply the delta to the original center, and set it as the new center.
                var ntoken = <MoveNodeToken>token;
                var newBoundaryCenter = ntoken.originalBoundsCenter.add(delta);
                ntoken.node.boundaryCurve.setCenter(newBoundaryCenter);
                // The label too, if there is one.
                if (ntoken.node.label != null) {
                    var newLabelCenter = ntoken.originalBoundsCenter.add(delta);
                    ntoken.node.label.bounds.setCenter(newLabelCenter);
                }
                // Having moved a node, edge routing is required.
                this.checkRouteEdges();
            }
            else if (token instanceof MoveEdgeLabelToken) {
                // If I'm moving an edge, I'll need to apply the delta to the original center, and set it as the new center.
                var ltoken = <MoveEdgeLabelToken>token;
                var newBoundsCenter = ltoken.originalLabelCenter.add(delta);
                ltoken.label.bounds.setCenter(newBoundsCenter);
            }
            else if (token instanceof MoveEdgeToken) {
                // If I'm moving a control point, I'll need to apply the delta to the original control point, and then replace it in the original polyline. The resulting polyline is the new polyline for the edge.
                var etoken = <MoveEdgeToken>token;
                var newPoint = etoken.originalPoint.add(delta);
                for (var j = 0; j < etoken.originalPolyline.length; j++)
                    if (etoken.originalPolyline[j].equals(etoken.originalPoint)) {
                        var edgeInternal = this.edgesMap[etoken.edge.id];
                        edgeInternal.polyline = etoken.originalPolyline.map((p, k) => k == j ? newPoint : p);
                        // Having changed the polyline, I'll need to rebuild the actual edge curve.
                        this.checkRebuildEdge(etoken.edge.id);
                        break;
                    }
            }
        }
    }

    /** The callback that's waiting to attempt to start edge routing again, if it could not be started immediately. */
    private delayCheckRouteEdges: () => void = null;
    /** Attempts to begin edge routing on the given edge set. If no edge set is provided, gets the edges that are outdated (i.e. the edges that are being affected by current node move operations). Edge routing cannot be started if the worker is already busy; in this case, a new attempt to start will be made when the worker becomes free again. Multiple calls to this function while an edge routing operation is pending will reset the request. */
    private checkRouteEdges(edgeSet?: string[]) {
        var edges: string[] = edgeSet == null ? this.getOutdatedEdges() : edgeSet;
        if (edges.length > 0) {
            // Remove any already existing callback.
            if (this.delayCheckRouteEdges != null)
                this.workStoppedCallbacks.remove(this.delayCheckRouteEdges);
            this.delayCheckRouteEdges = null;
            if (this.working) {
                // The worker is busy. Register a callback to try again.
                var that = this;
                this.delayCheckRouteEdges = () => { that.checkRouteEdges(edges); };
                this.workStoppedCallbacks.add(this.delayCheckRouteEdges);
            }
            else
                // The worker is available. Start routing.
                this.beginEdgeRouting(edges);
        }
    }

    /** The callback that's waiting to attempt to start edge rebuild again, if it could not be started immediately. */
    private delayCheckRebuildEdge: () => void = null;
    /** Attempts to begin rebuilding the given edge from its polyline. Edge rebuild cannot be started if the worker is already busy; in this case, a new attempt to start the rebuild will be made when the worker becomes free again. Multiple calls to this function while a rebuild is pending will reset the request. */
    private checkRebuildEdge(edge: string) {
        if (this.delayCheckRebuildEdge != null)
            this.workStoppedCallbacks.remove(this.delayCheckRebuildEdge);
        this.delayCheckRebuildEdge = null;
        if (this.working) {
            // The worker is busy. Register a callback to try again.
            var that = this;
            this.delayCheckRebuildEdge = () => { that.checkRebuildEdge(edge); };
            this.workStoppedCallbacks.add(this.delayCheckRebuildEdge);
        }
        else
            // The worker is available. Start routing.
            this.beginRebuildEdgeCurve(edge);
    }

    /** Returns an array of all the edges that are affected by node move operations. */
    private getOutdatedEdges(): string[] {
        // Prepare a dictionary of affected edges. By doing it this way, I avoid duplicate entries in case two or more nodes are being moved.
        var affectedEdges: { [id: string]: boolean } = {};
        for (var t in this.moveTokens) {
            var token = this.moveTokens[t];
            if (token instanceof MoveNodeToken) {
                // Get all of the edges that connect this node.
                var ntoken = <MoveNodeToken>token;
                var nEdges = this.getInEdges(ntoken.node.id).concat(this.getOutEdges(ntoken.node.id)).concat(this.getSelfEdges(ntoken.node.id));
                // Add them to the dictionary.
                for (var edge in nEdges)
                    affectedEdges[nEdges[edge]] = true;
            }
        }
        // Convert the dictionary to an array.
        var edges: string[] = [];
        for (var e in affectedEdges)
            edges.push(e);
        return edges;
    }

    /** Clear the list of elements that are selected for moving. */
    public endMoveElements() {
        this.checkRouteEdges();
        this.moveTokens = [];
    }

    /** If the triangle formed by three vertices has an area that's less than this, it's okay to discard the middle vertex for the purpose of building an edge's polyline. */
    private static ColinearityEpsilon: number = 50.00;
    /** Simplifies a polyline by removing vertexes that are colinear (or nearly so). */
    private removeColinearVertices(polyline: GPoint[]) {
        for (var i = 1; i < polyline.length - 2; i++) {
            // Get the (signed, doubled) triangle area and compare it with an epsilon.
            var a = GPoint.signedDoubledTriangleArea(polyline[i - 1], polyline[i], polyline[i + 1]);
            // If it's less than that, remove the vertex. This is an approximation, but it's good enough for the purpose of not producing a polyline with many useless control points.
            if (a >= -GGraph.ColinearityEpsilon && a <= GGraph.ColinearityEpsilon)
                polyline.splice(i--, 1);
        }
    }

    /** Creates a polyline for an edge that doesn't have one. Note: generally speaking, this is *not* a round-trip transformation. This is fine. In practice, the polyline will be composed by the start and end points of every segment of the curve, if it is segmented. If it isn't, it'll just be the start and end points. */
    private makePolyline(edge: GEdge): GPoint[] {
        var points: GPoint[] = [];
        // Push the center of the source node.
        var source = this.nodesMap[edge.source];
        points.push(source.node.boundaryCurve.getCenter());
        // If the curve is segmented...
        if (edge.curve != null && edge.curve.curvetype == "SegmentedCurve") {
            // Push the start of the curve (note that, in general, this is not the same as the center of the source node, due to trimming.
            var scurve = <GSegmentedCurve>edge.curve;
            points.push(scurve.getStart());
            // Push the end point of each segment (again, the end point of the last segment will not be the same as the center of the target node, due to trimming).
            for (var i = 0; i < scurve.segments.length; i++)
                points.push(scurve.segments[i].getEnd());
        }
        // Push the center of the target node.
        var target = this.nodesMap[edge.target];
        points.push(target.node.boundaryCurve.getCenter());
        // At this point, the polyline often has a lot of points due to edge routing algorithms producing segmented curves with lots of segments. Simplify the polyline.
        this.removeColinearVertices(points);
        return points;
    }

    /** Returns an edge's polyline. Will construct a polyline if not available already. */
    public getPolyline(edgeID: string): GPoint[] {
        var edgeInternal: GEdgeInternal = this.edgesMap[edgeID];
        if (edgeInternal.polyline == null)
            edgeInternal.polyline = this.makePolyline(edgeInternal.edge);
        return edgeInternal.polyline;
    }

    /** Adds a control point for an edge, at the given point. The control point will be placed, in the polyline order, between the closest existing control point and the one opposite that. If there is no control point opposing the closest one, it'll be placed right after the closest one. If the closest one is the last one, it'll be placed right before. */
    public addEdgeControlPoint(edgeID: string, point: GPoint) {
        var edgeInternal: GEdgeInternal = this.edgesMap[edgeID];
        // Find the closest control point.
        var iclosest = 0;
        var dclosest = edgeInternal.polyline[0].dist2(point);
        for (var i = 0; i < edgeInternal.polyline.length; i++) {
            var d = edgeInternal.polyline[i].dist2(point);
            if (d < dclosest) {
                iclosest = i;
                dclosest = d;
            }
        }
        // If it's the last one, just put it before (i.e. decrease "iclosest", which at this point will mean "the index of the control point right before the new one").
        if (iclosest == edgeInternal.polyline.length - 1)
            iclosest--;
        else if (iclosest > 0) {
            // If it's neither the last one nor the first one, I need to decide which control point, between the next and the previous, can be considered the "opposite" one. I get the distance from the segment made by the closest and the previous, as a parameter on the segment. If it is far from the extremes, i.e. it is somewhere in the middle, that's the one.
            var par = point.closestParameter(edgeInternal.polyline[iclosest - 1], edgeInternal.polyline[iclosest]);
            if (par > 0.1 && par < 0.9)
                iclosest--;
        }
        // Put the new point in the polyline, at the specified position.
        edgeInternal.polyline.splice(iclosest + 1, 0, point);
        // Begin rebuilding the curve.
        this.beginRebuildEdgeCurve(edgeID);
    }

    /** Removes the specified control point from the edge. The point should be the exact location of an existing control point. You cannot remove the first or last control points. */
    public delEdgeControlPoint(edgeID: string, point: GPoint) {
        var edgeInternal: GEdgeInternal = this.edgesMap[edgeID];
        // Search for the index of the control point in the polyline.
        for (var i = 1; i < edgeInternal.polyline.length - 1; i++)
            if (edgeInternal.polyline[i].equals(point)) {
                // Remove it.
                edgeInternal.polyline.splice(i, 1);
                // Begin rebuilding the curve.
                this.beginRebuildEdgeCurve(edgeID);
                break;
            }
    }
}