var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports"], function (require, exports) {
    var GPoint = (function () {
        function GPoint(p) {
            this.x = p.x === undefined ? 0 : p.x;
            this.y = p.y === undefined ? 0 : p.y;
        }
        GPoint.prototype.add = function (other) {
            return new GPoint({ x: this.x + other.x, y: this.y + other.y });
        };
        GPoint.prototype.sub = function (other) {
            return new GPoint({ x: this.x - other.x, y: this.y - other.y });
        };
        GPoint.prototype.div = function (op) {
            return new GPoint({ x: this.x / op, y: this.y / op });
        };
        GPoint.prototype.mul = function (op) {
            return new GPoint({ x: this.x * op, y: this.y * op });
        };
        GPoint.origin = new GPoint({ x: 0, y: 0 });
        return GPoint;
    })();
    exports.GPoint = GPoint;
    var GRect = (function () {
        function GRect(r) {
            this.x = r.x === undefined ? 0 : r.x;
            this.y = r.y === undefined ? 0 : r.y;
            this.width = r.width === undefined ? 0 : r.width;
            this.height = r.height === undefined ? 0 : r.height;
        }
        GRect.prototype.getTopLeft = function () {
            return new GPoint({ x: this.x, y: this.y });
        };
        GRect.prototype.getBottomRight = function () {
            return new GPoint({ x: this.getRight(), y: this.getBottom() });
        };
        GRect.prototype.getBottom = function () {
            return this.y + this.height;
        };
        GRect.prototype.getRight = function () {
            return this.x + this.width;
        };
        GRect.prototype.getCenter = function () {
            return new GPoint({ x: this.x + this.width / 2, y: this.y + this.height / 2 });
        };
        GRect.prototype.extend = function (other) {
            if (other == null)
                return this;
            return new GRect({
                x: Math.min(this.x, other.x),
                y: Math.min(this.y, other.y),
                width: Math.max(this.getRight(), other.getRight()) - Math.min(this.x, other.x),
                height: Math.max(this.getBottom(), other.getBottom()) - Math.min(this.y, other.y)
            });
        };
        GRect.prototype.extendP = function (point) {
            return this.extend(new GRect({ x: point.x, y: point.y, width: 0, height: 0 }));
        };
        GRect.zero = new GRect({ x: 0, y: 0, width: 0, height: 0 });
        return GRect;
    })();
    exports.GRect = GRect;
    var GCurve = (function () {
        function GCurve(type) {
            if (type === undefined)
                throw new Error("Undefined curve type");
            this.type = type;
        }
        GCurve.prototype.getCenter = function () {
            return GPoint.origin;
        };
        GCurve.prototype.getBoundingBox = function () {
            return GRect.zero;
        };
        GCurve.ofCurve = function (curve) {
            if (curve == null || curve === undefined)
                return null;
            var ret;
            if (curve.type == "Ellipse")
                ret = new GEllipse(curve);
            else if (curve.type == "Line")
                ret = new GLine(curve);
            else if (curve.type == "Bezier")
                ret = new GBezier(curve);
            else if (curve.type == "Polyline")
                ret = new GPolyline(curve);
            else if (curve.type == "SegmentedCurve")
                ret = new GSegmentedCurve(curve);
            else if (curve.type == "RoundedRect")
                ret = new GRoundedRect(curve);
            return ret;
        };
        return GCurve;
    })();
    exports.GCurve = GCurve;
    var GEllipse = (function (_super) {
        __extends(GEllipse, _super);
        function GEllipse(ellipse) {
            _super.call(this, "Ellipse");
            this.center = ellipse.center === undefined ? GPoint.origin : new GPoint(ellipse.center);
            this.axisA = ellipse.axisA === undefined ? GPoint.origin : new GPoint(ellipse.axisA);
            this.axisB = ellipse.axisB === undefined ? GPoint.origin : new GPoint(ellipse.axisB);
            this.parStart = ellipse.parStart === undefined ? 0 : ellipse.parStart;
            this.parEnd = ellipse.parStart === undefined ? Math.PI * 2 : ellipse.parEnd;
        }
        GEllipse.prototype.getCenter = function () {
            return this.center;
        };
        GEllipse.prototype.getBoundingBox = function () {
            var width = 2 * Math.max(Math.abs(this.axisA.x), Math.abs(this.axisB.x));
            var height = 2 * Math.max(Math.abs(this.axisA.y), Math.abs(this.axisB.y));
            var p = this.center.sub({ x: width / 2, y: height / 2 });
            return new GRect({ x: p.x, y: p.y, width: width, height: height });
        };
        GEllipse.make = function (width, height) {
            return new GEllipse({ center: GPoint.origin, axisA: new GPoint({ x: width / 2, y: 0 }), axisB: new GPoint({ x: 0, y: height / 2 }), parStart: 0, parEnd: Math.PI * 2 });
        };
        return GEllipse;
    })(GCurve);
    exports.GEllipse = GEllipse;
    var GLine = (function (_super) {
        __extends(GLine, _super);
        function GLine(line) {
            _super.call(this, "Line");
            this.start = line.start === undefined ? GPoint.origin : new GPoint(line.start);
            this.end = line.end === undefined ? GPoint.origin : new GPoint(line.end);
        }
        GLine.prototype.getCenter = function () {
            return this.start.add(this.end).div(2);
        };
        GLine.prototype.getBoundingBox = function () {
            var ret = new GRect({ x: this.start.x, y: this.start.y, width: 0, height: 0 });
            ret = ret.extendP(this.end);
            return ret;
        };
        return GLine;
    })(GCurve);
    exports.GLine = GLine;
    var GPolyline = (function (_super) {
        __extends(GPolyline, _super);
        function GPolyline(polyline) {
            _super.call(this, "Polyline");
            this.start = polyline.start === undefined ? GPoint.origin : new GPoint(polyline.start);
            this.points = [];
            for (var i = 0; i < polyline.points.length; i++)
                this.points.push(new GPoint(polyline.points[i]));
            this.closed = polyline.closed === undefined ? false : polyline.closed;
        }
        GPolyline.prototype.getCenter = function () {
            var ret = this.start;
            for (var i = 0; i < this.points.length; i++)
                ret = ret.add(this.points[i]);
            ret = ret.div(1 + this.points.length);
            return ret;
        };
        GPolyline.prototype.getBoundingBox = function () {
            var ret = new GRect({ x: this.points[0].x, y: this.points[0].y, height: 0, width: 0 });
            for (var i = 1; i < this.points.length; i++)
                ret = ret.extendP(this.points[i]);
            return ret;
        };
        return GPolyline;
    })(GCurve);
    exports.GPolyline = GPolyline;
    var GRoundedRect = (function (_super) {
        __extends(GRoundedRect, _super);
        function GRoundedRect(roundedRect) {
            _super.call(this, "RoundedRect");
            this.bounds = roundedRect.bounds === undefined ? GRect.zero : new GRect(roundedRect.bounds);
            this.radiusX = roundedRect.radiusX === undefined ? 0 : roundedRect.radiusX;
            this.radiusY = roundedRect.radiusY === undefined ? 0 : roundedRect.radiusY;
        }
        GRoundedRect.prototype.getCenter = function () {
            return this.bounds.getCenter();
        };
        GRoundedRect.prototype.getBoundingBox = function () {
            return this.bounds;
        };
        GRoundedRect.prototype.getCurve = function () {
            var segments = [];
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
        };
        return GRoundedRect;
    })(GCurve);
    exports.GRoundedRect = GRoundedRect;
    var GBezier = (function (_super) {
        __extends(GBezier, _super);
        function GBezier(bezier) {
            _super.call(this, "Bezier");
            this.start = bezier.start === undefined ? GPoint.origin : new GPoint(bezier.start);
            this.p1 = bezier.p1 === undefined ? GPoint.origin : new GPoint(bezier.p1);
            this.p2 = bezier.p2 === undefined ? GPoint.origin : new GPoint(bezier.p2);
            this.p3 = bezier.p3 === undefined ? GPoint.origin : new GPoint(bezier.p3);
        }
        GBezier.prototype.getCenter = function () {
            var ret = this.start;
            ret = ret.add(this.p1);
            ret = ret.add(this.p2);
            ret = ret.add(this.p3);
            ret = ret.div(4);
            return ret;
        };
        GBezier.prototype.getBoundingBox = function () {
            var ret = new GRect({ x: this.start.x, y: this.start.y, width: 0, height: 0 });
            ret = ret.extendP(this.p1);
            ret = ret.extendP(this.p2);
            ret = ret.extendP(this.p3);
            return ret;
        };
        return GBezier;
    })(GCurve);
    exports.GBezier = GBezier;
    var GSegmentedCurve = (function (_super) {
        __extends(GSegmentedCurve, _super);
        function GSegmentedCurve(segmentedCurve) {
            _super.call(this, "SegmentedCurve");
            this.segments = [];
            for (var i = 0; i < segmentedCurve.segments.length; i++)
                this.segments.push(GCurve.ofCurve(segmentedCurve.segments[i]));
        }
        GSegmentedCurve.prototype.getCenter = function () {
            var ret = GPoint.origin;
            for (var i = 0; i < this.segments.length; i++)
                ret = ret.add(this.segments[i].getCenter());
            ret = ret.div(this.segments.length);
            return ret;
        };
        GSegmentedCurve.prototype.getBoundingBox = function () {
            var ret = this.segments[0].getBoundingBox();
            for (var i = 1; i < this.segments.length; i++)
                ret = ret.extend(this.segments[i].getBoundingBox());
            return ret;
        };
        return GSegmentedCurve;
    })(GCurve);
    exports.GSegmentedCurve = GSegmentedCurve;
    var GLabel = (function () {
        function GLabel(label) {
            if (typeof (label) == "string")
                this.content = label;
            else {
                this.bounds = label.bounds == undefined || label.bounds == GRect.zero ? GRect.zero : new GRect(label.bounds);
                this.content = label.content;
                this.fill = label.fill === undefined ? "Black" : label.fill;
            }
        }
        return GLabel;
    })();
    exports.GLabel = GLabel;
    var GShape = (function () {
        function GShape() {
            this.radiusX = 0;
            this.radiusY = 0;
            this.multi = 0;
        }
        GShape.GetRect = function () {
            var ret = new GShape();
            ret.shape = "rect";
            return ret;
        };
        GShape.GetRoundedRect = function (radiusX, radiusY) {
            var ret = new GShape();
            ret.shape = "rect";
            ret.radiusX = radiusX === undefined ? 5 : radiusX;
            ret.radiusY = radiusY === undefined ? 5 : radiusY;
            return ret;
        };
        GShape.GetMaxRoundedRect = function () {
            var ret = new GShape();
            ret.shape = "rect";
            ret.radiusX = null;
            ret.radiusY = null;
            return ret;
        };
        GShape.FromString = function (shape) {
            if (shape == "rect")
                return GShape.GetRect();
            else if (shape == "roundedrect")
                return GShape.GetRoundedRect();
            else if (shape == "maxroundedrect")
                return GShape.GetMaxRoundedRect();
            return null;
        };
        GShape.RectShape = "rect";
        return GShape;
    })();
    exports.GShape = GShape;
    var GNode = (function () {
        function GNode(node) {
            if (node.id === undefined)
                throw new Error("Undefined node id");
            this.id = node.id;
            this.tooltip = node.tooltip === undefined ? null : node.tooltip;
            this.shape = node.shape === undefined ? null : typeof (node.shape) == "string" ? GShape.FromString(node.shape) : node.shape;
            this.boundaryCurve = GCurve.ofCurve(node.boundaryCurve);
            this.label = node.label === undefined ? null : node.label == null ? null : typeof (node.label) == "string" ? new GLabel({ content: node.label }) : new GLabel(node.label);
            this.labelMargin = node.labelMargin === undefined ? 5 : node.labelMargin;
            this.thickness = node.thickness == undefined ? 1 : node.thickness;
            this.fill = node.fill === undefined ? "" : node.fill;
            this.stroke = node.stroke === undefined ? "Black" : node.stroke;
        }
        GNode.prototype.isCluster = function () {
            return this.children !== undefined;
        };
        return GNode;
    })();
    exports.GNode = GNode;
    var GCluster = (function (_super) {
        __extends(GCluster, _super);
        function GCluster(cluster) {
            _super.call(this, cluster);
            this.children = [];
            for (var i = 0; i < cluster.children.length; i++)
                if (cluster.children[i].children !== undefined)
                    this.children.push(new GCluster(cluster.children[i]));
                else
                    this.children.push(new GNode(cluster.children[i]));
        }
        return GCluster;
    })(GNode);
    exports.GCluster = GCluster;
    var GArrowHead = (function () {
        function GArrowHead(arrowHead) {
            this.start = arrowHead.start === undefined ? GPoint.origin : arrowHead.start;
            this.end = arrowHead.end === undefined ? GPoint.origin : arrowHead.end;
            this.closed = arrowHead.closed === undefined ? false : arrowHead.closed;
            this.fill = arrowHead.fill === undefined ? false : arrowHead.fill;
        }
        GArrowHead.standard = new GArrowHead({ start: GPoint.origin, end: GPoint.origin, closed: false, fill: false });
        GArrowHead.closed = new GArrowHead({ start: GPoint.origin, end: GPoint.origin, closed: true, fill: false });
        GArrowHead.filled = new GArrowHead({ start: GPoint.origin, end: GPoint.origin, closed: true, fill: true });
        return GArrowHead;
    })();
    exports.GArrowHead = GArrowHead;
    var GEdge = (function () {
        function GEdge(edge) {
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
        return GEdge;
    })();
    exports.GEdge = GEdge;
    var GPlaneTransformation = (function () {
        function GPlaneTransformation(transformation) {
            if (transformation.rotation !== undefined) {
                var angle = transformation.rotation;
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
        GPlaneTransformation.defaultTransformation = new GPlaneTransformation({ m00: -1, m01: 0, m02: 0, m10: 0, m11: -1, m12: 0 });
        return GPlaneTransformation;
    })();
    exports.GPlaneTransformation = GPlaneTransformation;
    var ISettings = (function () {
        function ISettings() {
        }
        return ISettings;
    })();
    exports.ISettings = ISettings;
    var GSettings = (function () {
        function GSettings(settings) {
            this.transformation = settings.transformation === undefined ? GPlaneTransformation.defaultTransformation : settings.transformation;
            this.routing = settings.routing === undefined ? GSettings.sugiyamaSplinesRouting : settings.routing;
        }
        GSettings.sugiyamaSplinesRouting = "sugiyamasplines";
        GSettings.rectilinearRouting = "rectilinear";
        return GSettings;
    })();
    exports.GSettings = GSettings;
    var IGraph = (function () {
        function IGraph() {
        }
        return IGraph;
    })();
    exports.IGraph = IGraph;
    var GNodeInternal = (function () {
        function GNodeInternal() {
        }
        return GNodeInternal;
    })();
    var GGraph = (function () {
        function GGraph() {
            this.nodesMap = new Object();
            this.edgesMap = new Object();
            this.nodes = [];
            this.edges = [];
            this.boundingBox = GRect.zero;
            this.settings = new GSettings({ transformation: { m00: -1, m01: 0, m02: 0, m10: 0, m11: -1, m12: 0 } });
        }
        GGraph.prototype.addNode = function (node) {
            this.nodesMap[node.id] = { node: node, outEdges: [], inEdges: [], selfEdges: [] };
            this.nodes.push(node);
        };
        GGraph.prototype.getNode = function (id) {
            var nodeInternal = this.nodesMap[id];
            return nodeInternal == null ? null : nodeInternal.node;
        };
        GGraph.prototype.getInEdges = function (id) {
            var nodeInternal = this.nodesMap[id];
            return nodeInternal == null ? null : nodeInternal.inEdges;
        };
        GGraph.prototype.getOutEdges = function (id) {
            var nodeInternal = this.nodesMap[id];
            return nodeInternal == null ? null : nodeInternal.outEdges;
        };
        GGraph.prototype.getSelfEdges = function (id) {
            var nodeInternal = this.nodesMap[id];
            return nodeInternal == null ? null : nodeInternal.selfEdges;
        };
        GGraph.prototype.addEdge = function (edge) {
            if (this.nodesMap[edge.source] == null)
                throw new Error("Undefined node " + edge.source);
            if (this.nodesMap[edge.target] == null)
                throw new Error("Undefined node " + edge.target);
            this.edgesMap[edge.id] = edge;
            this.edges.push(edge);
            if (edge.source == edge.target)
                this.nodesMap[edge.source].selfEdges.push(edge.id);
            else {
                this.nodesMap[edge.source].outEdges.push(edge.id);
                this.nodesMap[edge.target].inEdges.push(edge.id);
            }
        };
        GGraph.prototype.getEdge = function (id) {
            return this.edgesMap[id];
        };
        GGraph.prototype.getJSON = function () {
            var igraph = { nodes: this.nodes, edges: this.edges, boundingBox: this.boundingBox, settings: this.settings };
            var ret = JSON.stringify(igraph);
            return ret;
        };
        GGraph.ofJSON = function (json) {
            var igraph = JSON.parse(json);
            if (igraph.edges === undefined)
                igraph.edges = [];
            var ret = new GGraph();
            ret.boundingBox = new GRect(igraph.boundingBox === undefined ? GRect.zero : igraph.boundingBox);
            ret.settings = new GSettings(igraph.settings === undefined ? {} : igraph.settings);
            for (var i = 0; i < igraph.nodes.length; i++) {
                var inode = igraph.nodes[i];
                if (inode.children !== undefined) {
                    var gcluster = new GCluster(inode);
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
        };
        GGraph.prototype.createNodeBoundariesRec = function (node, sizer) {
            if (node.boundaryCurve == null) {
                if (node.label != null && node.label.bounds == GRect.zero && sizer !== undefined) {
                    var labelSize = sizer(node.label, node);
                    node.label.bounds = new GRect({ x: 0, y: 0, width: labelSize.x, height: labelSize.y });
                }
                var labelWidth = node.label == null ? 0 : node.label.bounds.width;
                var labelHeight = node.label == null ? 0 : node.label.bounds.height;
                labelWidth += 2 * node.labelMargin;
                labelHeight += 2 * node.labelMargin;
                var boundary;
                if (node.shape != null && node.shape.shape == GShape.RectShape) {
                    var radiusX = node.shape.radiusX;
                    var radiusY = node.shape.radiusY;
                    if (radiusX == null && radiusY == null) {
                        var k = Math.min(labelWidth, labelHeight);
                        radiusX = radiusY = k / 2;
                    }
                    boundary = new GRoundedRect({
                        bounds: new GRect({ x: 0, y: 0, width: labelWidth, height: labelHeight }),
                        radiusX: radiusX,
                        radiusY: radiusY
                    });
                }
                else
                    boundary = GEllipse.make(labelWidth * Math.sqrt(2), labelHeight * Math.sqrt(2));
                node.boundaryCurve = boundary;
            }
            var cluster = node;
            if (cluster.children !== undefined)
                for (var i = 0; i < cluster.children.length; i++)
                    this.createNodeBoundariesRec(cluster.children[i], sizer);
        };
        // Creates the node boundaries for nodes that don't have one. If the node has a label, it will first
        // compute the label's size based on the provided size function, and then create an appropriate boundary.
        GGraph.prototype.createNodeBoundaries = function (sizer) {
            for (var i = 0; i < this.nodes.length; i++)
                this.createNodeBoundariesRec(this.nodes[i], sizer);
            // Assign size to edge labels too.
            if (sizer !== undefined) {
                for (var i = 0; i < this.edges.length; i++) {
                    var edge = this.edges[i];
                    if (edge.label != null && edge.label.bounds == GRect.zero) {
                        var labelSize = sizer(edge.label, edge);
                        edge.label.bounds.width = labelSize.x;
                        edge.label.bounds.height = labelSize.y;
                    }
                }
            }
        };
        GGraph.contextSizer = function (context, label) {
            return { x: context.measureText(label.content).width, y: parseInt(context.font) };
        };
        GGraph.prototype.createNodeBoundariesFromContext = function (context) {
            var selfMadeContext = (context === undefined);
            if (selfMadeContext) {
                var canvas = document.createElement('canvas');
                document.body.appendChild(canvas);
                context = canvas.getContext("2d");
            }
            this.createNodeBoundaries(function (label) {
                return GGraph.contextSizer(context, label);
            });
            if (selfMadeContext)
                document.body.removeChild(canvas);
        };
        GGraph.divSizer = function (div, label) {
            div.innerText = label.content;
            return { x: div.clientWidth, y: div.clientHeight };
        };
        GGraph.prototype.createNodeBoundariesFromDiv = function (div) {
            var selfMadeDiv = (div === undefined);
            if (selfMadeDiv) {
                div = document.createElement('div');
                div.setAttribute('style', 'float:left');
                document.body.appendChild(div);
            }
            this.createNodeBoundaries(function (label) {
                return GGraph.divSizer(div, label);
            });
            if (selfMadeDiv)
                document.body.removeChild(div);
        };
        GGraph.SVGSizer = function (svg, label) {
            var element = document.createElementNS('http://www.w3.org/2000/svg', 'text');
            element.setAttribute('fill', 'black');
            var textNode = document.createTextNode(label.content);
            element.appendChild(textNode);
            svg.appendChild(element);
            var bbox = element.getBBox();
            var ret = { x: bbox.width, y: bbox.height };
            svg.removeChild(element);
            return ret;
        };
        GGraph.prototype.createNodeBoundariesFromSVG = function (svg, style) {
            var selfMadeSvg = (svg === undefined);
            if (selfMadeSvg) {
                svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
                if (style !== undefined) {
                    svg.style.font = style.font;
                    svg.style.fontFamily = style.fontFamily;
                    svg.style.fontFeatureSettings = style.fontFeatureSettings;
                    svg.style.fontSize = style.fontSize;
                    svg.style.fontSizeAdjust = style.fontSizeAdjust;
                    svg.style.fontStretch = style.fontStretch;
                    svg.style.fontStyle = style.fontStyle;
                    svg.style.fontVariant = style.fontVariant;
                    svg.style.fontWeight = style.fontWeight;
                }
                document.body.appendChild(svg);
            }
            this.createNodeBoundaries(function (label) {
                return GGraph.SVGSizer(svg, label);
            });
            if (selfMadeSvg)
                document.body.removeChild(svg);
        };
        GGraph.SVGInContainerSizer = function (container, svg, label) {
            var element = document.createElementNS('http://www.w3.org/2000/svg', 'text');
            element.setAttribute('fill', 'black');
            var textNode = document.createTextNode(label.content);
            element.appendChild(textNode);
            svg.appendChild(element);
            var bbox = element.getBBox();
            var ret = { x: bbox.width, y: bbox.height };
            svg.removeChild(element);
            return ret;
        };
        GGraph.prototype.createNodeBoundariesForSVGInContainer = function (container) {
            var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            container.appendChild(svg);
            this.createNodeBoundaries(function (label) {
                return GGraph.SVGInContainerSizer(container, svg, label);
            });
            container.removeChild(svg);
        };
        GGraph.prototype.beginLayoutGraph = function (callback) {
            if (callback === void 0) { callback = null; }
            var self = this;
            var workerCallback = function (gstr) {
                var gs = GGraph.ofJSON(gstr.data);
                self.boundingBox = new GRect({
                    x: gs.boundingBox.x - 10,
                    y: gs.boundingBox.y - 10,
                    width: gs.boundingBox.width + 20,
                    height: gs.boundingBox.height + 20
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
            };
            var serialisedGraph = this.getJSON();
            var worker = new Worker('/MSAGL/workerBoot.js');
            worker.addEventListener('message', workerCallback);
            worker.postMessage(serialisedGraph);
        };
        return GGraph;
    })();
    exports.GGraph = GGraph;
});
//# sourceMappingURL=ggraph.js.map