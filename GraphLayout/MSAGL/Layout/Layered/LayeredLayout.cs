using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;

namespace Microsoft.Msagl.Layout.Layered
{
    /// <summary>
    /// A facade for LayeredLayout that implements AlgorithmBase
    /// todo: handle progress properly
    /// </summary>
    public class LayeredLayout : AlgorithmBase
    {
        private GeometryGraph geometryGraph;
        private SugiyamaLayoutSettings settings;
        private LayeredLayoutEngine engine;

        /// <summary>
        /// Layered layout arranged the given graph on layers inferred from the directed edge structure
        /// </summary>
        /// <param name="geometryGraph">graph to be laid out</param>
        /// <param name="settings">The settings for the algorithm.</param>
        public LayeredLayout(GeometryGraph geometryGraph, SugiyamaLayoutSettings settings)
        {
            this.geometryGraph = geometryGraph;
            this.settings = settings;
            this.engine = new LayeredLayoutEngine(geometryGraph, settings);
        }

        /// <summary>
        /// estimate the aspect ratio without actually doing layout
        /// </summary>
        /// <returns>width / height</returns>
        public double EstimateAspectRatio()
        {
            Point dimensions = this.engine.CalculateApproximateDimensions();
            Debug.Assert(dimensions.X > 0, "The estimated width of the layered layout should be greater than 0");
            Debug.Assert(dimensions.Y > 0, "The estimated width of the layered layout should be greater than 0");
            return dimensions.X / dimensions.Y;
        }

        /// <summary>
        /// Apply layout
        /// </summary>
        protected override void RunInternal()
        {
            PreRunTransform(geometryGraph, settings.Transformation);

            engine.Run();
            geometryGraph.AlgorithmData = engine;

            PostRunTransform(geometryGraph, settings.Transformation);
        }

        internal static void PreRunTransform(GeometryGraph geomGraph, PlaneTransformation matrix) {
            if (matrix.IsIdentity) return;
            var matrixInverse = matrix.Inverse;
            foreach (Node n in geomGraph.Nodes)
                n.Transform(matrixInverse);
            //calculate new label widths and heights
            foreach (Edge e in geomGraph.Edges) {
                if (e.Label != null) {
                    e.OriginalLabelWidth = e.Label.Width;
                    e.OriginalLabelHeight = e.Label.Height;
                    var r = new Rectangle(matrixInverse*new Point(0, 0), matrixInverse*new Point(e.Label.Width, e.Label.Height));
                    e.Label.Width = r.Width;
                    e.Label.Height = r.Height;
                }
            }

            geomGraph.UpdateBoundingBox();
        }


        static void PostRunTransform(GeometryGraph geometryGraph, PlaneTransformation transformation)
        {
            bool transform = !transformation.IsIdentity;
            if (transform)
            {
                foreach (Node n in geometryGraph.Nodes)
                {
                    n.Transform(transformation);
                }

                //restore labels widths and heights
                foreach (Edge e in geometryGraph.Edges)
                {
                    if (e.Label != null)
                    {
                        e.Label.Width = e.OriginalLabelWidth;
                        e.Label.Height = e.OriginalLabelHeight;
                    }
                }

                TransformCurves(geometryGraph, transformation);
            }

            geometryGraph.UpdateBoundingBox();
        }

        static void TransformCurves(GeometryGraph geometryGraph, PlaneTransformation transformation)
        {
            geometryGraph.BoundingBox = new Rectangle(transformation * geometryGraph.LeftBottom, transformation * geometryGraph.RightTop);
            foreach (Edge e in geometryGraph.Edges) {
                if (e.Label != null)
                    e.Label.Center = transformation * e.Label.Center;
                TransformEdgeCurve(transformation, e);
            }
        }

        static void TransformEdgeCurve(PlaneTransformation transformation, Edge e) {
            if (e.Curve != null)
            {
                e.Curve = e.Curve.Transform(transformation);
                var eg = e.EdgeGeometry;
                if (eg.SourceArrowhead != null)
                    eg.SourceArrowhead.TipPosition = transformation * eg.SourceArrowhead.TipPosition;
                if (eg.TargetArrowhead != null)
                    eg.TargetArrowhead.TipPosition = transformation * eg.TargetArrowhead.TipPosition;
                TransformUnderlyingPolyline(e, transformation);
            }
        }

        static void TransformUnderlyingPolyline(Edge e, PlaneTransformation transformation)
        {
            if (e.UnderlyingPolyline != null)
            {
                for (Site s = e.UnderlyingPolyline.HeadSite; s != null; s = s.Next)
                {
                    s.Point = transformation * s.Point;
                }
            }
        }

        ///<summary>
        ///Tries to find the layers if they were there
        ///</summary>
        ///<param name="graph"></param>
        public static void RecoverAlgorithmData(GeometryGraph graph) {
            var recoveryEngine = new RecoveryLayeredLayoutEngine(graph);
            graph.AlgorithmData=recoveryEngine.GetEngine();          
        }

        /// <summary>
        /// adaptes to the node boundary curve change
        /// </summary>
        public static void IncrementalLayout(GeometryGraph geometryGraph, Node node) {
            ValidateArg.IsNotNull(geometryGraph, "geometryGraph");

            LayeredLayoutEngine engine = geometryGraph.AlgorithmData as LayeredLayoutEngine;
            if (engine == null) return;
            PreRunTransform(geometryGraph, engine.SugiyamaSettings.Transformation);
            engine.IncrementalRun(node);
            PostRunTransform(geometryGraph, engine.SugiyamaSettings.Transformation);
        }
    }
}
