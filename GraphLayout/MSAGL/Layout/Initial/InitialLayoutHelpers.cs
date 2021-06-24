using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;

namespace Microsoft.Msagl.Layout.Initial
{
    static class InitialLayoutHelpers
    {
        internal static void RouteEdges(GeometryGraph component, LayoutAlgorithmSettings settings, CancelToken cancelToken)
        {
            Miscellaneous.LayoutHelpers.RouteAndLabelEdges(component, settings, component.Edges,  2000, cancelToken);
            
            //EdgeRoutingMode mode = settings.EdgeRoutingSettings.EdgeRoutingMode;

            //// Use straight line routing on very large graphs otherwise it is too slow 
            //if (component.Nodes.Count >= 2000)
            //{
            //    mode = EdgeRoutingMode.StraightLine;
            //}

            //switch (mode)
            //{
            //    case EdgeRoutingMode.Spline:
            //        var splineRouter = new SplineRouter(
            //            component,
            //            settings.EdgeRoutingSettings.Padding,
            //            settings.NodeSeparation, settings.EdgeRoutingSettings.ConeAngle, null);
            //        splineRouter.Run(cancelToken);
            //        break;
            //    case EdgeRoutingMode.SplineBundling:
            //        splineRouter = new SplineRouter(
            //            component,
            //            settings.EdgeRoutingSettings.Padding,
            //            settings.NodeSeparation / 20, settings.EdgeRoutingSettings.ConeAngle,
            //          new BundlingSettings());
            //        splineRouter.Run(cancelToken);
            //        break;
            //    case EdgeRoutingMode.Rectilinear:
            //        double edgePadding = settings.EdgeRoutingSettings.Padding;
            //        double cornerRadius = settings.EdgeRoutingSettings.CornerRadius;
            //        var rectilinearEdgeRouter = new RectilinearEdgeRouter(component, edgePadding, cornerRadius,
            //                                                              true);
            //        rectilinearEdgeRouter.Run(cancelToken);
            //        break;
            //    case EdgeRoutingMode.StraightLine:
            //        var router = new StraightLineEdges(component.Edges,
            //                                           settings.EdgeRoutingSettings.Padding);
            //        router.Run(cancelToken);
            //        break;
            //}
        }

        internal static void PlaceLabels(GeometryGraph component, CancelToken cancelToken)
        {
            List<Label> labelList = component.Edges.SelectMany(e => e.Labels).ToList();
            var labelPlacer = new EdgeLabelPlacement(component, labelList);
            labelPlacer.Run(cancelToken);
        }

        internal static void FixBoundingBox(GeometryGraph component, LayoutAlgorithmSettings settings)
        {
            // Pad the graph with margins so the packing will be spaced out.
            component.Margins = settings.ClusterMargin;
            component.UpdateBoundingBox();

            
        }
    }
}
