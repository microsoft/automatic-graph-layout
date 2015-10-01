importScripts("sharpkit_pre.js");
importScripts("jsclr.js");
importScripts("Microsoft.Msagl.js");
importScripts("sharpkit_post.js");

import G = require('ggraph');

declare var Microsoft;
declare var Is;
declare var self;

class Worker {
    getMsaglPoint(ipoint: G.IPoint): any {
        return new Microsoft.Msagl.Core.Geometry.Point.ctor$$Double$$Double(ipoint.x, ipoint.y);
    }

    getMsaglRect(grect: G.GRect): any {
        return new Microsoft.Msagl.Core.Geometry.Rectangle.ctor$$Double$$Double$$Point(grect.x, grect.y, this.getMsaglPoint({ x: grect.width, y: grect.height }));
    }

    getMsaglCurve(gcurve: G.GCurve): any {
        if (gcurve == null)
            return null;
        else if (gcurve.type == "Ellipse") {
            var gellipse = <G.GEllipse>gcurve;
            return new Microsoft.Msagl.Core.Geometry.Curves.Ellipse.ctor$$Double$$Double$$Point$$Point$$Point(
                gellipse.parStart,
                gellipse.parEnd,
                this.getMsaglPoint(gellipse.axisA),
                this.getMsaglPoint(gellipse.axisB),
                this.getMsaglPoint(gellipse.center));
        }
        else if (gcurve.type == "Line") {
            var gline = <G.GLine>gcurve;
            return new Microsoft.Msagl.Core.Geometry.Curves.LineSegment.ctor$$Point$$Point(
                this.getMsaglPoint(gline.start),
                this.getMsaglPoint(gline.end));
        }
        else if (gcurve.type == "Bezier") {
            var gbezier = <G.GBezier>gcurve;
            return new Microsoft.Msagl.Core.Geometry.Curves.CubicBezierSegment.ctor(
                this.getMsaglPoint(gbezier.start),
                this.getMsaglPoint(gbezier.p1),
                this.getMsaglPoint(gbezier.p2),
                this.getMsaglPoint(gbezier.p3));
        }
        else if (gcurve.type == "Polyline") {
            var gpolyline = <G.GPolyline>gcurve;
            var points = [];
            for (var i = 0; i < gpolyline.points.length; i++)
                points.push(this.getMsaglPoint(gpolyline.points[i]));
            return new Microsoft.Msagl.Core.Geometry.Curves.Polyline.ctor$$IEnumerable$1$Point(points);
        }
        else if (gcurve.type == "RoundedRect") {
            var groundedrect = <G.GRoundedRect>gcurve;
            return new Microsoft.Msagl.Core.Geometry.Curves.RoundedRect.ctor$$Rectangle$$Double$$Double(
                this.getMsaglRect(groundedrect.bounds),
                groundedrect.radiusX,
                groundedrect.radiusY);
        }
        else if (gcurve.type == "SegmentedCurve") {
            var gsegcurve = <G.GSegmentedCurve>gcurve;
            var curves = [];
            for (var i = 0; i < gsegcurve.segments.length; i++)
                curves.push(this.getMsaglCurve(gsegcurve.segments[i]));
            return new Microsoft.Msagl.Core.Geometry.Curves.Curve.ctor$$List$1$ICurve(curves);
        }
        return null;
    }

    addClusterToMsagl(graph: any, cluster: any, nodeMap: Object, gcluster: G.GCluster) {
        for (var i = 0; i < gcluster.children.length; i++) {
            var gnode = gcluster.children[i];
            this.addNodeToMsagl(graph, cluster, nodeMap, gnode);
        }
    }

    addNodeToMsagl(graph: any, rootCluster: any, nodeMap: Object, gnode: G.GNode) {
        var isCluster = (<G.GCluster>gnode).children !== undefined;
        var node = null;
        if (isCluster) {
            node = new Microsoft.Msagl.Core.Layout.Cluster.ctor();
            rootCluster.AddCluster(node);
            this.addClusterToMsagl(graph, node, nodeMap, <G.GCluster>gnode);
        }
        else {
            node = new Microsoft.Msagl.Core.Layout.Node.ctor();
            rootCluster.AddNode(node);
            graph.get_Nodes().Add(node);
        }
        node.set_UserData(gnode.id);

        var curve = this.getMsaglCurve(gnode.boundaryCurve);
        if (curve == null)
            curve = this.getMsaglCurve(new G.GRoundedRect({
                bounds: { x: 0, y: 0, width: 0, height: 0 }, radiusX: 0, radiusY: 0
            }));
        node.set_BoundaryCurve(curve);
        nodeMap[gnode.id] = { mnode: node, gnode: gnode };
    }

    addEdgeToMsagl(graph: any, nodeMap: Object, edgeMap: Object, gedge: G.GEdge) {
        var source = nodeMap[gedge.source].mnode;
        var target = nodeMap[gedge.target].mnode;
        var edge = new Microsoft.Msagl.Core.Layout.Edge.ctor$$Node$$Node(source, target);
        var curve = this.getMsaglCurve(gedge.curve);
        if (curve != null)
            edge.set_Curve(curve);
        if (gedge.label != null) {
            var label = new Microsoft.Msagl.Core.Layout.Label.ctor$$Double$$Double$$GeometryObject(gedge.label.bounds.width, gedge.label.bounds.height, edge);
            label.set_GeometryParent(edge);
            edge.set_Label(label);
        }
        if (gedge.arrowHeadAtSource != null)
            edge.get_EdgeGeometry().set_SourceArrowhead(new Microsoft.Msagl.Core.Layout.Arrowhead.ctor());
        if (gedge.arrowHeadAtTarget != null)
            edge.get_EdgeGeometry().set_TargetArrowhead(new Microsoft.Msagl.Core.Layout.Arrowhead.ctor());
        graph.get_Edges().Add(edge);
        edgeMap[gedge.id] = { medge: edge, gedge: gedge };
    }

    getMsagl(ggraph: G.GGraph): any {
        var nodeMap = new Object(); // id -> { msagl node, ggraph node }
        var edgeMap = new Object(); // id -> { msagl edge, ggraph edge }
        var graph = new Microsoft.Msagl.Core.Layout.GeometryGraph.ctor();

        // Add nodes (and clusters)
        var rootCluster = graph.get_RootCluster();
        for (var i = 0; i < ggraph.nodes.length; i++)
            this.addNodeToMsagl(graph, rootCluster, nodeMap, ggraph.nodes[i]);

        // Add edges
        for (var i = 0; i < ggraph.edges.length; i++)
            this.addEdgeToMsagl(graph, nodeMap, edgeMap, ggraph.edges[i]);

        var settings;
        if (ggraph.settings.layout == G.GSettings.mdsLayout) {
            settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings.ctor();
        }
        else {
            settings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings.ctor();
            var transformation = new Microsoft.Msagl.Core.Geometry.Curves.PlaneTransformation.ctor$$Double$$Double$$Double$$Double$$Double$$Double(
                ggraph.settings.transformation.m00, ggraph.settings.transformation.m01, ggraph.settings.transformation.m02,
                ggraph.settings.transformation.m10, ggraph.settings.transformation.m11, ggraph.settings.transformation.m12);
            settings.set_Transformation(transformation);

            var edgeRoutingSettings = settings.get_EdgeRoutingSettings();
            if (ggraph.settings.routing == G.GSettings.rectilinearRouting)
                edgeRoutingSettings.set_EdgeRoutingMode(Microsoft.Msagl.Core.Routing.EdgeRoutingMode.Rectilinear);
        }

        return { graph: graph, settings: settings, nodeMap: nodeMap, edgeMap: edgeMap, source: ggraph };
    }

    getGPoint(point): G.GPoint {
        return new G.GPoint({ x: point.get_X(), y: point.get_Y() });
    }

    getGRect(rect): G.GRect {
        return new G.GRect({ x: rect.get_Left(), y: rect.get_Bottom(), width: rect.get_Width(), height: rect.get_Height() });
    }

    getGCurve(curve): G.GCurve {
        var ret: G.GCurve;
        if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.Curve.ctor)) {
            var segments = [];
            var sEn = curve.get_Segments().GetEnumerator();
            while (sEn.MoveNext())
                segments.push(this.getGCurve(sEn.get_Current()));
            ret = new G.GSegmentedCurve({
                type: "SegmentedCurve",
                segments: segments
            });
        }
        else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.Polyline.ctor)) {
            var points = [];
            var pEn = curve.get_PolylinePoints().GetEnumerator();
            while (pEn.MoveNext())
                points.push(this.getGPoint(pEn.get_Current()));
            ret = new G.GPolyline({
                type: "Polyline",
                start: this.getGPoint(curve.get_Start()),
                points: points,
                closed: curve.get_Closed()
            });
        }
        else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.CubicBezierSegment.ctor)) {
            ret = new G.GBezier({
                type: "Bezier",
                start: this.getGPoint(curve.get_Start()),
                p1: this.getGPoint(curve.B(1)),
                p2: this.getGPoint(curve.B(2)),
                p3: this.getGPoint(curve.B(3)),
            });
        }
        else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.LineSegment.ctor)) {
            ret = new G.GLine({
                type: "Line",
                start: this.getGPoint(curve.get_Start()),
                end: this.getGPoint(curve.get_End())
            });
        }
        else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.Ellipse.ctor)) {
            ret = new G.GEllipse({
                type: "Ellipse",
                axisA: this.getGPoint(curve.get_AxisA()),
                axisB: this.getGPoint(curve.get_AxisB()),
                center: this.getGPoint(curve.get_Center()),
                parEnd: curve.get_ParEnd(),
                parStart: curve.get_ParStart()
            });
        }
        else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.RoundedRect.ctor)) {
            ret = new G.GRoundedRect({
                type: "RoundedRect",
                bounds: this.getGRect(curve.get_BoundingBox()),
                radiusX: curve.get_RadiusX(),
                radiusY: curve.get_RadiusY()
            });
        }
        return ret;
    }

    getGGraph(msagl): G.GGraph {
        msagl.source.boundingBox = this.getGRect(msagl.graph.get_BoundingBox());
        for (var id in msagl.nodeMap) {
            var node = msagl.nodeMap[id].mnode;
            var gnode: G.GNode = msagl.nodeMap[id].gnode;
            var curve = node.get_BoundaryCurve();
            gnode.boundaryCurve = this.getGCurve(curve);
            if (gnode.label != null) {
                gnode.label.bounds.x = node.get_Center().get_X() - gnode.label.bounds.width / 2;
                gnode.label.bounds.y = node.get_Center().get_Y() - gnode.label.bounds.height / 2;
            }
        }

        for (var id in msagl.edgeMap) {
            var edge = msagl.edgeMap[id].medge;
            var gedge: G.GEdge = msagl.edgeMap[id].gedge;
            var curve = edge.get_Curve();
            if (gedge.label != null) {
                var labelbbox = this.getGRect(edge.get_Label().get_BoundingBox());
                gedge.label.bounds.x = labelbbox.x;
                gedge.label.bounds.y = labelbbox.y;
            }
            if (gedge.arrowHeadAtSource != null) {
                gedge.arrowHeadAtSource.start = this.getGPoint(curve.get_Start());
                gedge.arrowHeadAtSource.end = this.getGPoint(edge.get_EdgeGeometry().get_SourceArrowhead().get_TipPosition());
            }
            if (gedge.arrowHeadAtTarget != null) {
                gedge.arrowHeadAtTarget.start = this.getGPoint(curve.get_End());
                gedge.arrowHeadAtTarget.end = this.getGPoint(edge.get_EdgeGeometry().get_TargetArrowhead().get_TipPosition());
            }
            gedge.curve = this.getGCurve(curve);
        }
        return msagl.source;
    }

    graph: G.GGraph;

    constructor(graph: G.GGraph) {
        this.graph = graph;
    }

    runLayout(): void {
        var msagl = this.getMsagl(this.graph);
        var cancelToken = new Microsoft.Msagl.Core.CancelToken.ctor();
        cancelToken.set_Canceled(false);
        Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(msagl.graph, msagl.settings, cancelToken);
        this.graph = this.getGGraph(msagl);
    }
}

export function handleMessage(e): void {
    var ggraph = G.GGraph.ofJSON(e.data);
    var worker = new Worker(ggraph);
    worker.runLayout();
    var serialisedGraph = worker.graph.getJSON();
    self.postMessage(serialisedGraph);
}
