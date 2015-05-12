//-----------------------------------------------------------------------
// <copyright file="SugiyamaConstraintTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests 
{
    [TestClass]
    public class SugiyamaConstraintTests : MsaglTestBase 
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
            EnableDebugViewer();                        
        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup();
        }

        #region Test
        [TestMethod]
        [WorkItem(444585)]
        [Description("Testing constraints should not create overaps")]
        public void TreeWithConstraints() 
        {
            
            var graph = new GeometryGraph();

            var closed = new Node(CreateEllipse(), "closed");
            var line = new Node(CreateEllipse(), "line");
            var bezier = new Node(CreateEllipse(), "bezier");
            var arc = new Node(CreateEllipse(), "arc");
            var rectangle = new Node(CreateEllipse(), "rectangle");
            var ellipse = new Node(CreateEllipse(), "ellipse");
            var polygon = new Node(CreateEllipse(), "polygon");
            var shapes = new Node(CreateEllipse(), "shapes");
            var open = new Node(CreateEllipse(), "open");
            graph.Nodes.Add(closed);
            graph.Nodes.Add(line);
            graph.Nodes.Add(bezier);
            graph.Nodes.Add(arc);
            graph.Nodes.Add(rectangle);
            graph.Nodes.Add(ellipse);
            graph.Nodes.Add(polygon);
            graph.Nodes.Add(shapes);
            graph.Nodes.Add(open);
            
            var so = new Edge(shapes, open);
            var sc = new Edge(shapes, closed);
            var ol = new Edge(open, line);
            var ob = new Edge(open, bezier);
            var oa = new Edge(open, arc);
            var cr = new Edge(closed, rectangle);
            var ce = new Edge(closed, ellipse);
            var cp = new Edge(closed, polygon);
            graph.Edges.Add(so);
            graph.Edges.Add(sc);
            graph.Edges.Add(ol);
            graph.Edges.Add(ob);
            graph.Edges.Add(oa);
            graph.Edges.Add(cr);
            graph.Edges.Add(ce);
            graph.Edges.Add(cp);

            var settings = new SugiyamaLayoutSettings();
            settings.AddUpDownVerticalConstraint(closed, ellipse);
            settings.AddUpDownVerticalConstraint(open, bezier);
            settings.AddUpDownConstraint(closed, open);
            settings.AddSameLayerNeighbors(polygon, open);
            settings.AddLeftRightConstraint(closed, open);

            //To verify 444585, just turn on this following commented line
            settings.AddLeftRightConstraint(ellipse, rectangle);
            settings.AddLeftRightConstraint(ellipse, bezier);

            LayeredLayout layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();

            ShowGraphInDebugViewer(graph);

            Assert.IsTrue(Math.Abs(closed.Center.X - ellipse.Center.X) < 0.01);
            Assert.IsTrue(Math.Abs(open.Center.X - bezier.Center.X) < 0.01);

            foreach (var n0 in graph.Nodes) 
            {
                foreach (var n1 in graph.Nodes) 
                {
                    if (n0 == n1) 
                    {
                        continue;
                    }
                    Assert.IsFalse(n0.BoundingBox.Intersects(n1.BoundingBox));
                }
            }

            SugiyamaValidation.ValidateUpDownVerticalConstraint(closed, ellipse);
            SugiyamaValidation.ValidateUpDownVerticalConstraint(open, bezier);
            SugiyamaValidation.ValidateUpDownConstraint(closed, open);
            SugiyamaValidation.ValidateNeighborConstraint(graph, polygon, open, settings);
            SugiyamaValidation.ValidateLeftRightConstraint(closed, open);

            //To verify 444585, also turn on this following commented line
            //SugiyamaValidation.ValidateLeftRightConstraint(ellipse, rectangle);
            SugiyamaValidation.ValidateLeftRightConstraint(ellipse, bezier);
        }

        [TestMethod]
        [Ignore, WorkItem(444585)]
        [Description("a, b, c, d each on their own layer, a.X<b.X, b.X<c.X, d.X>c.X")]
        public void StaircaseLayoutWithConstraints()
        {
            var graph = new GeometryGraph();

            var a = new Node(CreateRectangle());
            var b = new Node(CreateRectangle());
            var c = new Node(CreateRectangle());
            var d = new Node(CreateRectangle());
            // need the following dummy nodes for constraints
            var v1 = new Node(CreateDot());
            var v2 = new Node(CreateDot());
            var v3 = new Node(CreateDot());
            
            graph.Nodes.Add(a);
            graph.Nodes.Add(b);
            graph.Nodes.Add(c);
            graph.Nodes.Add(d);
            graph.Nodes.Add(v1);
            graph.Nodes.Add(v2);
            graph.Nodes.Add(v3);

            graph.Edges.Add(new Edge(a, b));
            graph.Edges.Add(new Edge(a, b));
            graph.Edges.Add(new Edge(b, a));
            graph.Edges.Add(new Edge(b, c));
            graph.Edges.Add(new Edge(c, d));
            graph.Edges.Add(new Edge(d, c));
            graph.Edges.Add(new Edge(c, d));
            graph.Edges.Add(new Edge(d, c));

            // if dummy nodes are not connected to graph then we get a DebugAssert fail
            graph.Edges.Add(new Edge(a, v1));
            graph.Edges.Add(new Edge(b, v2));
            graph.Edges.Add(new Edge(c, v3));

            var settings = new SugiyamaLayoutSettings();

            // it's fairly easy to find debug assert failures by trying different combinations of the following 
            settings.AddUpDownVerticalConstraint(a, v1);
            settings.AddUpDownVerticalConstraint(b, v2);
            settings.AddUpDownVerticalConstraint(c, v3);
            //settings.AddUpDownVerticalConstraint(v2, d);
            //settings.AddUpDownConstraint(a, v1);
            //settings.AddUpDownConstraint(a, b);
            //settings.AddUpDownConstraint(b, c);
            //settings.AddUpDownConstraint(c, d);
            settings.AddSameLayerNeighbors(v1, b);
            settings.AddSameLayerNeighbors(v2, c);
            settings.AddSameLayerNeighbors(d, v3);
            //settings.AddLeftRightConstraint(v1, b);
            //settings.AddLeftRightConstraint(v2, c); - doesn't work

            LayeredLayout layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();

            graph.Nodes.Remove(v1);
            graph.Nodes.Remove(v2);
            graph.Nodes.Remove(v3);

            ShowGraphInDebugViewer(graph);
        }

        [TestMethod]
        [Ignore]
        [WorkItem(444690)]
        [Description("Test Sugiyama constraints with transformation case")]
        public void ConstraintWithTransformation()
        {
            Random random = new Random(999);

            GeometryGraph graph = GraphGenerator.GenerateOneSimpleGraph();            
            GraphGenerator.SetRandomNodeShapes(graph, random);
            SugiyamaLayoutSettings settings = new SugiyamaLayoutSettings();

            //layer direction to be left to right
            settings.Transformation = PlaneTransformation.Rotation(Math.PI / 2);

            List<Node> nodes = graph.Nodes.ToList();

            settings.AddUpDownConstraint(nodes[0], nodes[1]);
            settings.AddLeftRightConstraint(nodes[3], nodes[4]);
            settings.AddUpDownVerticalConstraint(nodes[0], nodes[3]);
            settings.AddSameLayerNeighbors(nodes[2], nodes[4]);

            LayeredLayout layeredLayout = new LayeredLayout(graph, settings);
            layeredLayout.Run();

            ShowGraphInDebugViewer(graph);

            SugiyamaValidation.ValidateUpDownConstraint(nodes[0], nodes[1]);
            SugiyamaValidation.ValidateLeftRightConstraint(nodes[3], nodes[4]);
            SugiyamaValidation.ValidateUpDownVerticalConstraint(nodes[0], nodes[3]);
            SugiyamaValidation.ValidateNeighborConstraint(graph, nodes[2], nodes[4], settings);
        }

        //TODO: need to add more cases involving transformation
        //or conflicting constraints
        #endregion

        #region Helper
        private static ICurve CreateEllipse()
        {
            return CurveFactory.CreateEllipse(20, 10, new Point());
        }
        private static ICurve CreateRectangle()
        {
            return CurveFactory.CreateRectangle(20, 10, new Point());
        }
        private static ICurve CreateDot()
        {
            return CurveFactory.CreateRectangle(5, 5, new Point());
        }
        #endregion
    }
}
