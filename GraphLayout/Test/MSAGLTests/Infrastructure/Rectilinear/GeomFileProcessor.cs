// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeomFileProcessor.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;

namespace Microsoft.Msagl.UnitTests.Rectilinear 
{
  internal class GeomFileProcessor : BasicFileProcessor
    {
        // This must be passed in.
        private readonly Func<IEnumerable<Shape>, RectilinearEdgeRouterWrapper> createRouterFunc;

        // These can remain null if not needed.
        internal Action<IEnumerable<Shape>> ShowShapes { get; set; }
        internal Action<RectilinearEdgeRouterWrapper> ShowInitialObstacles { get; set; }
        internal Action<RectilinearEdgeRouterWrapper> ShowGraph { get; set; }

        internal bool NoPorts { get; set; }
        internal bool UseSparseVisibilityGraph { get; set; }        

        internal GeomFileProcessor(Action<string> writeLineFunc,
                            Func<string, bool> errorFunc,
                            Func<IEnumerable<Shape>, RectilinearEdgeRouterWrapper> createRouterFunc,
                            bool verbose,
                            bool quiet)
            : base(writeLineFunc, errorFunc, verbose, quiet)
        {
            this.createRouterFunc = createRouterFunc;
        }

        internal override void LoadAndProcessFile(string fileName)
        {
            RunGeomGraph(fileName, false);
        }

        internal RectilinearEdgeRouterWrapper RunGeomGraph(string fileName, bool loadOnly)
        {
            var geomGraph = GeometryGraphReader.CreateFromFile(fileName);
            if (loadOnly)
            {
                return null;
            }

            var nodeShapeMap = new Dictionary<Node, Shape>();
            foreach (Node node in geomGraph.Nodes)
            {
                Shape shape = RectilinearInteractiveEditor.CreateShapeWithRelativeNodeAtCenter(node);
                nodeShapeMap.Add(node, shape);
            }
            if (null != ShowShapes)
            {
                ShowShapes(nodeShapeMap.Values);
            }

            var router = createRouterFunc(nodeShapeMap.Values);
            if (!this.NoPorts)
            {
                foreach (var edge in geomGraph.Edges)
                {
                    EdgeGeometry edgeGeom = edge.EdgeGeometry;
                    edgeGeom.SourcePort = nodeShapeMap[edge.Source].Ports.First();
                    edgeGeom.TargetPort = nodeShapeMap[edge.Target].Ports.First();

                    // Remove any path results retrieved from the geom file.
                    edgeGeom.Curve = null;
                    if (edgeGeom.SourceArrowhead != null)
                    {
                        edgeGeom.SourceArrowhead.TipPosition = new Point();
                    }
                    if (edgeGeom.TargetArrowhead != null)
                    {
                        edgeGeom.TargetArrowhead.TipPosition = new Point();
                    }
                    router.AddEdgeGeometryToRoute(edgeGeom);
                }
            }
            if (null != ShowInitialObstacles)
            {
                ShowInitialObstacles(router);
            }
            router.Run();
            if (null != ShowGraph)
            {
                ShowGraph(router);
            }
            return router;
        }

        internal RectilinearEdgeRouterWrapper RunGeomGraphWithFreePorts(string fileName, bool loadOnly)
        {
            var geomGraph = GeometryGraphReader.CreateFromFile(fileName);
            if (loadOnly)
            {
                return null;
            }

            var router = createRouterFunc(geomGraph.Nodes.Select(node => new Shape(node.BoundaryCurve)));
            foreach (var edge in geomGraph.Edges)
            {
                // Use a null curve, for consistency with RectilinearVerifier.MakeAbsoluteObstaclePort
                // treatment of UseFreePortsForObstaclePorts.
                EdgeGeometry edgeGeom = edge.EdgeGeometry;
                edgeGeom.SourcePort = new FloatingPort(null, edge.Source.Center);
                edgeGeom.TargetPort = new FloatingPort(null, edge.Target.Center);
                router.AddEdgeGeometryToRoute(edgeGeom);
            }
            router.Run();
            if (null != ShowGraph)
            {
                ShowGraph(router);
            }
            return router;
        }
    }
}
