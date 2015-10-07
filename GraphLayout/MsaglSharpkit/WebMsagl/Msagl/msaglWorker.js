define(["require", "exports", "ggraph"], function (require, exports) {
    /// <amd-dependency path="ggraph"/>
    importScripts("sharpkit_pre.js");
    importScripts("jsclr.js");
    importScripts("Microsoft.Msagl.js");
    importScripts("sharpkit_post.js");
    var Worker = (function () {
        function Worker(graph) {
            this.graph = graph;
        }
        Worker.prototype.getMsaglPoint = function (ipoint) {
            return new Microsoft.Msagl.Core.Geometry.Point.ctor$$Double$$Double(ipoint.x, ipoint.y);
        };
        Worker.prototype.getMsaglRect = function (grect) {
            return new Microsoft.Msagl.Core.Geometry.Rectangle.ctor$$Double$$Double$$Point(grect.x, grect.y, this.getMsaglPoint({ x: grect.width, y: grect.height }));
        };
        Worker.prototype.getMsaglCurve = function (gcurve) {
            if (gcurve == null)
                return null;
            else if (gcurve.type == "Ellipse") {
                var gellipse = gcurve;
                return new Microsoft.Msagl.Core.Geometry.Curves.Ellipse.ctor$$Double$$Double$$Point$$Point$$Point(gellipse.parStart, gellipse.parEnd, this.getMsaglPoint(gellipse.axisA), this.getMsaglPoint(gellipse.axisB), this.getMsaglPoint(gellipse.center));
            }
            else if (gcurve.type == "Line") {
                var gline = gcurve;
                return new Microsoft.Msagl.Core.Geometry.Curves.LineSegment.ctor$$Point$$Point(this.getMsaglPoint(gline.start), this.getMsaglPoint(gline.end));
            }
            else if (gcurve.type == "Bezier") {
                var gbezier = gcurve;
                return new Microsoft.Msagl.Core.Geometry.Curves.CubicBezierSegment.ctor(this.getMsaglPoint(gbezier.start), this.getMsaglPoint(gbezier.p1), this.getMsaglPoint(gbezier.p2), this.getMsaglPoint(gbezier.p3));
            }
            else if (gcurve.type == "Polyline") {
                var gpolyline = gcurve;
                var points = [];
                for (var i = 0; i < gpolyline.points.length; i++)
                    points.push(this.getMsaglPoint(gpolyline.points[i]));
                return new Microsoft.Msagl.Core.Geometry.Curves.Polyline.ctor$$IEnumerable$1$Point(points);
            }
            else if (gcurve.type == "RoundedRect") {
                var groundedrect = gcurve;
                return new Microsoft.Msagl.Core.Geometry.Curves.RoundedRect.ctor$$Rectangle$$Double$$Double(this.getMsaglRect(groundedrect.bounds), groundedrect.radiusX, groundedrect.radiusY);
            }
            else if (gcurve.type == "SegmentedCurve") {
                var gsegcurve = gcurve;
                var curves = [];
                for (var i = 0; i < gsegcurve.segments.length; i++)
                    curves.push(this.getMsaglCurve(gsegcurve.segments[i]));
                return new Microsoft.Msagl.Core.Geometry.Curves.Curve.ctor$$List$1$ICurve(curves);
            }
            return null;
        };
        Worker.prototype.addClusterToMsagl = function (graph, cluster, nodeMap, gcluster) {
            for (var i = 0; i < gcluster.children.length; i++) {
                var gnode = gcluster.children[i];
                this.addNodeToMsagl(graph, cluster, nodeMap, gnode);
            }
        };
        Worker.prototype.addNodeToMsagl = function (graph, rootCluster, nodeMap, gnode) {
            var isCluster = gnode.children !== undefined;
            var node = null;
            if (isCluster) {
                node = new Microsoft.Msagl.Core.Layout.Cluster.ctor();
                rootCluster.AddCluster(node);
                this.addClusterToMsagl(graph, node, nodeMap, gnode);
            }
            else {
                node = new Microsoft.Msagl.Core.Layout.Node.ctor();
                rootCluster.AddNode(node);
                graph.get_Nodes().Add(node);
            }
            node.set_UserData(gnode.id);
            var curve = this.getMsaglCurve(gnode.boundaryCurve);
            if (curve == null)
                curve = this.getMsaglCurve(new GRoundedRect({
                    bounds: { x: 0, y: 0, width: 0, height: 0 },
                    radiusX: 0,
                    radiusY: 0
                }));
            node.set_BoundaryCurve(curve);
            nodeMap[gnode.id] = { mnode: node, gnode: gnode };
        };
        Worker.prototype.addEdgeToMsagl = function (graph, nodeMap, edgeMap, gedge) {
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
        };
        Worker.prototype.getMsagl = function (ggraph) {
            var nodeMap = new Object(); // id -> { msagl node, ggraph node }
            var edgeMap = new Object(); // id -> { msagl edge, ggraph edge }
            var graph = new Microsoft.Msagl.Core.Layout.GeometryGraph.ctor();
            // Add nodes (and clusters)
            var rootCluster = graph.get_RootCluster();
            for (var i = 0; i < ggraph.nodes.length; i++)
                this.addNodeToMsagl(graph, rootCluster, nodeMap, ggraph.nodes[i]);
            for (var i = 0; i < ggraph.edges.length; i++)
                this.addEdgeToMsagl(graph, nodeMap, edgeMap, ggraph.edges[i]);
            var settings;
            if (ggraph.settings.layout == GSettings.mdsLayout) {
                settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings.ctor();
            }
            else {
                settings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings.ctor();
                var transformation = new Microsoft.Msagl.Core.Geometry.Curves.PlaneTransformation.ctor$$Double$$Double$$Double$$Double$$Double$$Double(ggraph.settings.transformation.m00, ggraph.settings.transformation.m01, ggraph.settings.transformation.m02, ggraph.settings.transformation.m10, ggraph.settings.transformation.m11, ggraph.settings.transformation.m12);
                settings.set_Transformation(transformation);
                var edgeRoutingSettings = settings.get_EdgeRoutingSettings();
                if (ggraph.settings.routing == GSettings.rectilinearRouting)
                    edgeRoutingSettings.set_EdgeRoutingMode(Microsoft.Msagl.Core.Routing.EdgeRoutingMode.Rectilinear);
            }
            return { graph: graph, settings: settings, nodeMap: nodeMap, edgeMap: edgeMap, source: ggraph };
        };
        Worker.prototype.getGPoint = function (point) {
            return new GPoint({ x: point.get_X(), y: point.get_Y() });
        };
        Worker.prototype.getGRect = function (rect) {
            return new GRect({ x: rect.get_Left(), y: rect.get_Bottom(), width: rect.get_Width(), height: rect.get_Height() });
        };
        Worker.prototype.getGCurve = function (curve) {
            var ret;
            if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.Curve.ctor)) {
                var segments = [];
                var sEn = curve.get_Segments().GetEnumerator();
                while (sEn.MoveNext())
                    segments.push(this.getGCurve(sEn.get_Current()));
                ret = new GSegmentedCurve({
                    type: "SegmentedCurve",
                    segments: segments
                });
            }
            else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.Polyline.ctor)) {
                var points = [];
                var pEn = curve.get_PolylinePoints().GetEnumerator();
                while (pEn.MoveNext())
                    points.push(this.getGPoint(pEn.get_Current()));
                ret = new GPolyline({
                    type: "Polyline",
                    start: this.getGPoint(curve.get_Start()),
                    points: points,
                    closed: curve.get_Closed()
                });
            }
            else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.CubicBezierSegment.ctor)) {
                ret = new GBezier({
                    type: "Bezier",
                    start: this.getGPoint(curve.get_Start()),
                    p1: this.getGPoint(curve.B(1)),
                    p2: this.getGPoint(curve.B(2)),
                    p3: this.getGPoint(curve.B(3)),
                });
            }
            else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.LineSegment.ctor)) {
                ret = new GLine({
                    type: "Line",
                    start: this.getGPoint(curve.get_Start()),
                    end: this.getGPoint(curve.get_End())
                });
            }
            else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.Ellipse.ctor)) {
                ret = new GEllipse({
                    type: "Ellipse",
                    axisA: this.getGPoint(curve.get_AxisA()),
                    axisB: this.getGPoint(curve.get_AxisB()),
                    center: this.getGPoint(curve.get_Center()),
                    parEnd: curve.get_ParEnd(),
                    parStart: curve.get_ParStart()
                });
            }
            else if (Is(curve, Microsoft.Msagl.Core.Geometry.Curves.RoundedRect.ctor)) {
                ret = new GRoundedRect({
                    type: "RoundedRect",
                    bounds: this.getGRect(curve.get_BoundingBox()),
                    radiusX: curve.get_RadiusX(),
                    radiusY: curve.get_RadiusY()
                });
            }
            return ret;
        };
        Worker.prototype.getGGraph = function (msagl) {
            msagl.source.boundingBox = this.getGRect(msagl.graph.get_BoundingBox());
            for (var id in msagl.nodeMap) {
                var node = msagl.nodeMap[id].mnode;
                var gnode = msagl.nodeMap[id].gnode;
                var curve = node.get_BoundaryCurve();
                gnode.boundaryCurve = this.getGCurve(curve);
                if (gnode.label != null) {
                    gnode.label.bounds.x = node.get_Center().get_X() - gnode.label.bounds.width / 2;
                    gnode.label.bounds.y = node.get_Center().get_Y() - gnode.label.bounds.height / 2;
                }
            }
            for (var id in msagl.edgeMap) {
                var edge = msagl.edgeMap[id].medge;
                var gedge = msagl.edgeMap[id].gedge;
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
        };
        Worker.prototype.runLayout = function () {
            var msagl = this.getMsagl(this.graph);
            var cancelToken = new Microsoft.Msagl.Core.CancelToken.ctor();
            cancelToken.set_Canceled(false);
            Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(msagl.graph, msagl.settings, cancelToken);
            this.graph = this.getGGraph(msagl);
        };
        return Worker;
    })();
    function handleMessage(e) {
        var ggraph = GGraph.ofJSON(e.data);
        var worker = new Worker(ggraph);
        worker.runLayout();
        var serialisedGraph = worker.graph.getJSON();
        self.postMessage(serialisedGraph);
    }
    exports.handleMessage = handleMessage;
});
//# sourceMappingURL=msaglWorker.js.map