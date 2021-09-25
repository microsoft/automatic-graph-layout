// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectilinearTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using PortSpan = System.Tuple<double, double>;

// TUVALU_TODO add tests to route to center

namespace Microsoft.Msagl.UnitTests.Rectilinear
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Tests for Rectilinear edge routing.
    /// </summary>
    [TestClass]
    //[Ignore]
    public class RectilinearTests : RectilinearVerifier
    {
        // ReSharper disable InconsistentNaming

        [TestMethod]
        [Timeout(2000)]
        [Description("Creates a simple test with 3 diamond shapes.")]
        public void Diamond3()
        {
            var obstacles = Create_Diamond3();
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Creates a simple test with 3 diamond shapes and two freeports.")]
        public void Diamond3_With_FreePorts()
        {
            var obstacles = Create_Diamond3();
            var freePorts = new List<FloatingPort>
                {
                    MakeAbsoluteFreePort(new Point(65, 525)), MakeAbsoluteFreePort(new Point(50, 465)) 
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1), freePorts);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Creates a test with diamond shapes and overlapping squares.")]
        public void Diamond3_Square6_Overlap()
        {
            var obstacles = Create_Diamond3_Square6();
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Creates a test with diamond shapes and overlapping squares.")]
        public void Diamond3_Square8_Overlap()
        {
            var obstacles = Create_Diamond3_Square8();
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Creates a test with diamond shapes and overlapping squares.")]
        public void Diamond3_Square9_Overlap()
        {
            var obstacles = Create_Diamond3_Square8();

            // Create one final rectangle that overlaps the two interior overlapped rectangles.
            var rect = new Rectangle(new Point(119.5, 530), new Point(160, 471));
            ICurve overlapper = CurveFactory.CreateRectangle(
                rect.Right - rect.Left, rect.Top - rect.Bottom, rect.Center);
            obstacles.Add(new Shape(overlapper));

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Creates a test with diamond shapes and overlapping squares.")]
        public void Diamond3_Square9_Overlap_HalfWidths()
        {
            var obstacles = Create_Diamond3_Square8();

            // Create one final rectangle that overlaps the two interior overlapped rectangles.
            var rect = new Rectangle(new Point(122.5, 530), new Point(160, 471));
            ICurve overlapper = CurveFactory.CreateRectangle(
                (rect.Right - rect.Left) / 2, (rect.Top - rect.Bottom) / 2, rect.Center);
            obstacles.Add(new Shape(overlapper));

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        private static List<Shape> Create_Diamond3_Square8()
        {
            var obstacles = Create_Diamond3_Square6();

            // Create two interior squares wholly nested inside the big obstacle.
            ICurve overlapper = CurveFactory.CreateRectangle(20, 15, new Point(140, 517.5));
            obstacles.Add(new Shape(overlapper));
            overlapper = CurveFactory.CreateRectangle(10, 10, new Point(140, 480));
            obstacles.Add(new Shape(overlapper));
            return obstacles;
        }

        private static List<Shape> Create_Diamond3()
        {
            return new List<Shape>
                {
                    CurveFromPoints(
                        new[]
                            {
                                new Point(100, 500), new Point(140, 540), new Point(180, 500),
                                new Point(140, 460)
                            }),
                    CurveFromPoints(
                        new[]
                            {
                                new Point(40, 420), new Point(80, 480), new Point(120, 420),
                                new Point(80, 380)
                            }),
                    CurveFromPoints(
                        new[]
                            {
                                new Point(160, 440), new Point(200, 480), new Point(240, 440),
                                new Point(200, 400)
                            })
                };
        }

        private static List<Shape> Create_Diamond3_Square6()
        {
            var obstacles = Create_Diamond3();

            // Overlap the top and bottom points of the upper diamond with rectangles.
            ICurve overlapper = CurveFactory.CreateRectangle(30, 10, new Point(140, 460));
            obstacles.Add(new Shape(overlapper));
            overlapper = CurveFactory.CreateRectangle(30, 10, new Point(140, 540));
            obstacles.Add(new Shape(overlapper));

            // Overlap the left and right points of the upper diamond with rectangles.
            // For the right, overlap the square we'll create to overlap the upper corner of the right diamond.
            overlapper = CurveFactory.CreateRectangle(30, 10, new Point(100, 500));
            obstacles.Add(new Shape(overlapper));
            overlapper = CurveFactory.CreateRectangle(30, 30, new Point(180, 500));
            obstacles.Add(new Shape(overlapper));

            // Overlap the top points of the lower diamonds with squares.
            overlapper = CurveFactory.CreateRectangle(20, 20, new Point(80, 480));
            obstacles.Add(new Shape(overlapper));
            overlapper = CurveFactory.CreateRectangle(20, 20, new Point(200, 480));
            obstacles.Add(new Shape(overlapper));
            return obstacles;
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Creates a test with multiple circles.")]
        public void CircleTest()
        {
            const double Radius = 50;
            var obstacles = new List<Shape>
                {
                    CreateCircle(Radius, new Point(1222, 881)),
                    CreateCircle(Radius, new Point(1296, 1181)),
                    CreateCircle(Radius, new Point(1105, 1197)),
                    CreateCircle(Radius, new Point(835, 1241)),
                    CreateCircle(Radius, new Point(970, 1014)),
                    CreateCircle(Radius, new Point(965, 1259)),
                    CreateCircle(Radius, new Point(630, 1262)),
                    CreateCircle(Radius, new Point(500, 1262)),
                    CreateCircle(Radius, new Point(1800, 1180)),
                    CreateCircle(Radius, new Point(1875, 1330)),
                    CreateCircle(Radius, new Point(1435, 1390)),
                    CreateCircle(Radius, new Point(1125, 1466)),
                    CreateCircle(Radius, new Point(1415, 960)),
                    CreateCircle(Radius, new Point(2036, 1117)),
                    CreateCircle(Radius, new Point(1125, 1711)),
                    CreateCircle(Radius, new Point(1125, 1850)),
                    CreateCircle(Radius, new Point(1186, 583)),
                    CreateCircle(Radius, new Point(874, 863)),
                    CreateCircle(Radius, new Point(2165, 1090)),
                    CreateCircle(Radius, new Point(1163, 450))
                };

            foreach (Shape shape in obstacles)
            {
                MakeSingleRelativeObstaclePort(shape, new Point(0, 0));
            }

            var router = CreateRouter(obstacles);
            AddRoutingPorts(router, obstacles, 0, 16);
            AddRoutingPorts(router, obstacles, 0, 4);
            AddRoutingPorts(router, obstacles, 0, 1);
            AddRoutingPorts(router, obstacles, 0, 12);
            AddRoutingPorts(router, obstacles, 1, 2);
            AddRoutingPorts(router, obstacles, 1, 12);
            AddRoutingPorts(router, obstacles, 1, 10);
            AddRoutingPorts(router, obstacles, 1, 11);
            AddRoutingPorts(router, obstacles, 1, 8);
            AddRoutingPorts(router, obstacles, 2, 1);
            AddRoutingPorts(router, obstacles, 2, 3);
            AddRoutingPorts(router, obstacles, 3, 6);
            AddRoutingPorts(router, obstacles, 4, 5);
            AddRoutingPorts(router, obstacles, 4, 17);
            AddRoutingPorts(router, obstacles, 5, 11);
            AddRoutingPorts(router, obstacles, 6, 7);
            AddRoutingPorts(router, obstacles, 8, 9);
            AddRoutingPorts(router, obstacles, 8, 13);
            AddRoutingPorts(router, obstacles, 11, 14);
            AddRoutingPorts(router, obstacles, 13, 18);
            AddRoutingPorts(router, obstacles, 14, 15);
            AddRoutingPorts(router, obstacles, 16, 19);
            router.Run();
            ShowGraph(router);
        }

        private static Shape CreateCircle(double radius, Point center)
        {
            return new Shape(CurveFactory.CreateCircle(radius, center));
        }

        private static Shape CreateRoundedRect(double width, double height, double radiusX, double radiusY, Point center)
        {
            return new Shape(CurveFactory.CreateRectangleWithRoundedCorners(width, height, radiusX, radiusY, center));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Based on a cutdown of clust5.dot.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Clust5_Minimal()
        {
            var obstacles = new List<Shape>();
            Shape shape;

            // Top.  Make a single port at the bottom and explicitly add EdgeGeometries to ensure routing 
            // starts downward to illustrate nudger benefits.
            obstacles.Add(shape = PolylineFromRectanglePoints(new Point(80, 80), new Point(100, 100)));
            var offset0 = new Point(0, -(shape.BoundingBox.Top - shape.BoundingBox.Bottom) / 2);
            Port p0 = MakeSingleRelativeObstaclePort(shape, offset0);

            // Bottom
            obstacles.Add(shape = PolylineFromRectanglePoints(new Point(20, 40), new Point(40, 60)));
            Port p1 = MakeSingleRelativeObstaclePort(shape, new Point(0, 0));
            obstacles.Add(shape = PolylineFromRectanglePoints(new Point(50, 40), new Point(70, 60)));
            Port p2 = MakeSingleRelativeObstaclePort(shape, new Point(0, 0));
            obstacles.Add(shape = PolylineFromRectanglePoints(new Point(80, 40), new Point(100, 60)));
            Port p3 = MakeSingleRelativeObstaclePort(shape, new Point(0, 0));
            obstacles.Add(shape = PolylineFromRectanglePoints(new Point(110, 40), new Point(130, 60)));
            Port p4 = MakeSingleRelativeObstaclePort(shape, new Point(0, 0));
            obstacles.Add(shape = PolylineFromRectanglePoints(new Point(140, 40), new Point(160, 60)));
            Port p5 = MakeSingleRelativeObstaclePort(shape, new Point(0, 0));

            var router = CreateRouter(obstacles);
            AddRoutingPorts(router, p0, p1);
            AddRoutingPorts(router, p0, p2);
            AddRoutingPorts(router, p0, p3);
            AddRoutingPorts(router, p0, p4);
            AddRoutingPorts(router, p0, p5);
            router.Run();
            ShowGraph(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reflections with a single large blocking obstacle between the two reflecting obstacles.")]
        public void Reflection_Block1_Big()
        {
            Reflection_Block1_Big_Worker();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reflections with a single large blocking obstacle between the two reflecting obstacles.")]
        public void Reflection_Block1_Big_UseRect() 
        {
            // See notes in TransientGraphUtility.SplitEdge.  This test has X coordinates (at y == 75) after
            // splicing source and the collinear parts of target westward to 64:
            //  29     ->     75
            //      64 ->  68
            // Splicing target from 64->29 calls SplitEdge on edge 29->75 at 64; the 64->68 edge must be followed
            // before adding the edge to 75.
            base.UseObstacleRectangles = true;
            Reflection_Block1_Big_Worker();
        }

        private void Reflection_Block1_Big_Worker()
        {
            var obstacles = new List<Shape>
            {
                    CurveFromPoints(new[] { new Point(30, 30), new Point(130, 130), new Point(140, 120), new Point(40, 20) }),
                    CurveFromPoints(new[] { new Point(70, 10), new Point(190, 130), new Point(200, 120), new Point(80, 0) }),
                    CurveFromPoints(new[] { new Point(65, 40), new Point(80, 55), new Point(95, 40), new Point(80, 25) })
            };
            this.DoRouting(obstacles, this.CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reflections with 3 adjacent triangles.")]
        public void Reflection_Triangle1()
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(50, 10), new Point(80, 80), new Point(110, 10) }),
                    CurveFromPoints(new[] { new Point(40, 10), new Point(10, 80), new Point(70, 80) }),
                    CurveFromPoints(new[] { new Point(120, 10), new Point(90, 80), new Point(150, 80) })
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reflections with 3 adjacent triangles where the outer inverted triangles overlap above the middle one.")]
        public void Reflection_Triangle1_Overlap()
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(50, 10), new Point(80, 80), new Point(110, 10) }),
                    CurveFromPoints(new[] { new Point(40, 10), new Point(-5, 110), new Point(85, 110) }),
                    CurveFromPoints(new[] { new Point(120, 10), new Point(75, 110), new Point(165, 110) })
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reflections with 3 adjacent triangles where the outer inverted triangles do not overlap.")]
        public void Reflection_Triangle1_NoOverlap()
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(47, 10), new Point(80, 80), new Point(110, 10) }),
                    CurveFromPoints(new[] { new Point(40, 10), new Point(4, 90), new Point(76, 90) }),
                    CurveFromPoints(new[] { new Point(120, 10), new Point(84, 90), new Point(156, 90) })
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reflections with a single small blocking obstacle between the two reflecting obstacles.")]
        public void Reflection_Block1_Small()
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(30, 30), new Point(130, 130), new Point(140, 120), new Point(40, 20) }),
                    CurveFromPoints(new[] { new Point(70, 10), new Point(190, 130), new Point(200, 120), new Point(80, 0) }),
                    CurveFromPoints(new[] { new Point(75, 40), new Point(80, 45), new Point(85, 40), new Point(80, 35) })
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reflections with a two blocking obstacle between the two reflecting obstacles.")]
        public void Reflection_Block2()
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(30, 310), new Point(100, 380), new Point(110, 370), new Point(40, 300) }),
                    CurveFromPoints(new[] { new Point(90, 290), new Point(170, 370), new Point(180, 360), new Point(100, 280) }),
                    CurveFromPoints(new[] { new Point(70, 320), new Point(80, 330), new Point(80, 320) }),
                    CurveFromPoints(new[] { new Point(90, 320), new Point(110, 340), new Point(110, 320) })
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reflections with two long non-orthogonal rectangles; this is the baseline for comparison to the Reflection_LongAngle_Overlap* tests.")]
        public void Reflection_LongAngle()
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(20, 30), new Point(20, 40), new Point(40, 40), new Point(40, 30) }),
                    CurveFromPoints(new[] { new Point(30, 50), new Point(20, 60), new Point(70, 110), new Point(80, 100) }),
                    CurveFromPoints(new[] { new Point(50, 10), new Point(40, 20), new Point(100, 110), new Point(110, 100) })
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test Reflections going into an intersection valley closing at the upper right.")]
        public void Reflection_LongAngle_Overlap_ToHighRight()
        {
            Reflection_LongAngle_Overlap_Worker(1, 1);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test Reflections going into an intersection valley closing at the upper left.")]
        public void Reflection_LongAngle_Overlap_ToHighLeft()
        {
            Reflection_LongAngle_Overlap_Worker(-1, 1);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test Reflections going coming out of an intersection valley closed at the lower right.")]
        public void Reflection_LongAngle_Overlap_FromLowRight()
        {
            Reflection_LongAngle_Overlap_Worker(1, -1);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test Reflections going coming out of an intersection valley closed at the lower left.")]
        public void Reflection_LongAngle_Overlap_FromLowLeft()
        {
            Reflection_LongAngle_Overlap_Worker(-1, -1);
        }

        private void Reflection_LongAngle_Overlap_Worker(int invertX, int invertY)
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(
                        new[]
                            {
                                new Point(20 * invertX, 30 * invertY), new Point(20 * invertX, 40 * invertY),
                                new Point(40 * invertX, 40 * invertY), new Point(40 * invertX, 30 * invertY)
                            }),
                    CurveFromPoints(
                        new[]
                            {
                                new Point(30 * invertX, 50 * invertY), new Point(20 * invertX, 60 * invertY),
                                new Point(100 * invertX, 120 * invertY), new Point(110 * invertX, 110 * invertY)
                            }),
                    CurveFromPoints(
                        new[]
                            {
                                new Point(50 * invertX, 10 * invertY), new Point(40 * invertX, 20 * invertY),
                                new Point(100 * invertX, 110 * invertY), new Point(110 * invertX, 100 * invertY)
                            })
                };
            DoRouting(obstacles);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test FreePorts outside of the graph bounds defined by its obstacles.")]
        public void FreePorts_OutOfBounds()
        {
            FreePorts_OutOfBounds_Worker(1);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test duplicate FreePorts outside of the graph bounds defined by its obstacles.")]
        public void FreePorts_OutOfBounds_Dup()
        {
            FreePorts_OutOfBounds_Worker(2);
        }

        private void FreePorts_OutOfBounds_Worker(int numPorts)
        {
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(20, 20), new Point(100, 100)) 
                };

            // Cover all the boundary conditions - each corner has three points (two extensions of
            // sentinels and one that has an H-to-V bend), and each side has two near midpoint.
            // These are clockwise from lower left.  Test duplicates also.
            var freePorts = new List<FloatingPort>();
            for (int ii = 0; ii < numPorts; ++ii)
            {
                freePorts.Add(MakeAbsoluteFreePort(new Point(10, 10)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(10, 20)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(10, 50)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(10, 70)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(10, 100)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(10, 110)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(20, 110)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(50, 110)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(70, 110)));

                freePorts.Add(MakeAbsoluteFreePort(new Point(100, 110)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(110, 110)));

                freePorts.Add(MakeAbsoluteFreePort(new Point(110, 100)));

                freePorts.Add(MakeAbsoluteFreePort(new Point(110, 70)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(110, 50)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(110, 20)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(110, 10)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(100, 10)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(70, 10)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(50, 10)));
                freePorts.Add(MakeAbsoluteFreePort(new Point(20, 10)));
            }
            DoRouting(obstacles, null /*routings*/, freePorts);
        }

        // For extensive out-of-bounds (oob) freeport tests.
        private class OobSetup
        {
            private readonly RectilinearTests tester;

            // The only obstacle's port, at its center.
            private FloatingPort portA;

            // The two or three test ports we create.
            private readonly FloatingPort[] ports;

            // Populated by the obstacle and ports.
            private List<Shape> obstacles;

            private RectilinearEdgeRouterWrapper router;

            // The number of vertices after initial setup, before any ports are added to the VG.
            internal int NumberOfVerticesBefore { get; private set; }

            internal OobSetup(RectilinearTests tester, Point loc0, Point loc1)
            {
                this.tester = tester;
                ports = new FloatingPort[2];
                ports[0] = MakeAbsoluteFreePort(loc0);
                ports[1] = MakeAbsoluteFreePort(loc1);
                CreateRouter();
            }

            internal OobSetup(RectilinearTests tester, Point loc0, Point loc1, Point loc2)
            {
                this.tester = tester;
                ports = new FloatingPort[3];
                ports[0] = MakeAbsoluteFreePort(loc0);
                ports[1] = MakeAbsoluteFreePort(loc1);
                ports[2] = MakeAbsoluteFreePort(loc2);
                CreateRouter();
            }

            internal OobSetup(RectilinearTests tester, Point loc0, Point loc1, Point loc2, Point loc3)
            {
                this.tester = tester;
                ports = new FloatingPort[4];
                ports[0] = MakeAbsoluteFreePort(loc0);
                ports[1] = MakeAbsoluteFreePort(loc1);
                ports[2] = MakeAbsoluteFreePort(loc2);
                ports[3] = MakeAbsoluteFreePort(loc3);
                CreateRouter();
            }

            private void CreateRouter()
            {
                obstacles = new List<Shape>
                    {
                        tester.PolylineFromRectanglePoints(new Point(20, 20), new Point(100, 100)) 
                    };
                portA = tester.MakeAbsoluteObstaclePort(obstacles[0], obstacles[0].BoundingBox.Center);

                router = tester.CreateRouter(obstacles);
                tester.AddRoutingPorts(router, portA, ports[0]);
                tester.AddRoutingPorts(router, portA, ports[1]);
                if (ports.Length > 2)
                {
                    tester.AddRoutingPorts(router, portA, ports[2]);
                    if (ports.Length > 3)
                    {
                        tester.AddRoutingPorts(router, portA, ports[3]);
                    }
                }
                router.GenerateVisibilityGraph();
                this.NumberOfVerticesBefore = router.VisibilityGraph.VertexCount;
            }

            internal int AddPort(int portIndex, int numExpectedVertices)
            {
                router.AddPortToVisibilityGraph(ports[portIndex]);
                int numVertices = router.VisibilityGraph.VertexCount;
                // Don't verify vertex count for these tests if we are doing path routing,
                // because RouteEdges() reorganizes what's in ActivePorts.
                if (!router.WantPaths && (-1 != numExpectedVertices))
                {
                    Validate.AreEqual(numExpectedVertices, numVertices, "Number of vertices on AddPort was not as expected");
                }
                return numVertices;
            }

            internal void RemoveControlPoints()
            {
                router.RemoveAllControlPointsFromVisibilityGraph();
                Validate.AreEqual(this.NumberOfVerticesBefore, router.VisibilityGraph.VertexCount,
                        "Number of vertices on RemovePort was not numberOfVerticesBefore");
            }

            internal void ShowGraph()
            {
                if (router.WantPaths)
                {
                    router.Run();
                }
                tester.ShowGraph(router);
            }
        }

        // end OobSetup

        [TestMethod]
        [Timeout(2000)]
        [Description("Test two freePorts above the same bend, one higher than the other.")]
        public void FreePorts_OobCorner_BendUsedTwice_Vertical()
        {
            // Two freePorts above the same bend, one higher than the other.
            //     F
            //     F
            //   --V
            var setup = new OobSetup(this, new Point(110, 110), new Point(110, 120));
            int numberOfVerticesAfter0 = setup.AddPort(0, setup.NumberOfVerticesBefore + 2);
            setup.AddPort(1, numberOfVerticesAfter0 + 1);
            setup.ShowGraph();
            setup.RemoveControlPoints();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reusing a bend vertex as an out-of-bounds free vertex.")]
        public void FreePorts_OobCorner_BendReusedAsFreePort()
        {
            // Create one FreePort with a bend, then create another FreePort at that bend.
            //   F        F       F
            // --V  --> --F --> --V --> none
            var setup = new OobSetup(this, new Point(110, 110), new Point(110, 101));
            int numberOfVerticesAfter0 = setup.AddPort(0, setup.NumberOfVerticesBefore + 2);
            // Should just have replaced the previous vertex with a FreePortVisibilityVertex.
            setup.AddPort(1, numberOfVerticesAfter0);
            setup.ShowGraph();
            setup.RemoveControlPoints();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test reusing an out-of-bounds free vertex as a bend vertex.")]
        public void FreePorts_OobCorner_FreePortReusedAsBend()
        {
            // Create one FreePort without a bend, then create another FreePort with the bend
            // at the first FreePort.
            //            F       F
            // --F  --> --F --> --V --> none
            var setup = new OobSetup(this, new Point(110, 101), new Point(110, 110));
            int numberOfVerticesAfter0 = setup.AddPort(0, setup.NumberOfVerticesBefore + 1);
            setup.AddPort(1, numberOfVerticesAfter0 + 1);
            setup.ShowGraph();
            setup.RemoveControlPoints();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test using a bend vertex twice for out-of-bounds freeports.")]
        public void FreePorts_OobCorner_BendUsedTwiceHorizontal()
        {
            // Create one FreePort with a bend, then create another FreePort collinear
            // with that bend and past it in the horizontal direction.
            //   F
            // --V--F
            var setup = new OobSetup(this, new Point(110, 110), new Point(120, 101));
            int numberOfVerticesAfter0 = setup.AddPort(0, setup.NumberOfVerticesBefore + 2);
            setup.AddPort(1, numberOfVerticesAfter0 + 1);
            setup.ShowGraph();
            setup.RemoveControlPoints();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test two bend vertices for out-of-bounds freeports.")]
        public void FreePorts_OobCorner_TwoBends()
        {
            // Create two FreePorts with collinear bends, then create another FreePort collinear
            // with that bend and past it in the horizontal direction.
            //   F  F
            // --V--V--F
            var setup = new OobSetup(this, new Point(110, 110), new Point(120, 110), new Point(130, 101));
            int numberOfVerticesAfter0 = setup.AddPort(0, setup.NumberOfVerticesBefore + 2);
            int numberOfVerticesAfter1 = setup.AddPort(1, numberOfVerticesAfter0 + 2);
            setup.AddPort(2, numberOfVerticesAfter1 + 1);
            setup.ShowGraph();
            setup.RemoveControlPoints();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test two bend vertices for out-of-bounds freeports, then reuse one bend vertex as a freeport.")]
        public void FreePorts_OobCorner_TwoBends_Rep1Free_Rem3210()
        {
            // Create two FreePorts with collinear bends, then create another FreePort collinear
            // with that bend and past it in the horizontal direction.
            //   F  F           F  F
            // --V--V--F  =>  --V--F--F
            var setup = new OobSetup(this, new Point(110, 110), new Point(120, 110), new Point(130, 101), new Point(120, 101));
            int numberOfVerticesAfter0 = setup.AddPort(0, setup.NumberOfVerticesBefore + 2);
            int numberOfVerticesAfter1 = setup.AddPort(1, numberOfVerticesAfter0 + 2);
            int numberOfVerticesAfter2 = setup.AddPort(2, numberOfVerticesAfter1 + 1);
            setup.AddPort(3, numberOfVerticesAfter2);
            setup.ShowGraph();
            setup.RemoveControlPoints();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test three collinear out-of-bounds freeports.")]
        public void FreePorts_Oob_NoBendsThree()
        {
            // Create three collinear FreePorts at mid-height (thus no bends).
            // --F--F--F
            var setup = new OobSetup(this, new Point(110, 90), new Point(120, 90), new Point(130, 90));
            int numberOfVerticesAfter0 = setup.AddPort(0, setup.NumberOfVerticesBefore + 2); // includes MakeInBounds vertex
            int numberOfVerticesAfter1 = setup.AddPort(1, numberOfVerticesAfter0 + 1);
            setup.AddPort(2, numberOfVerticesAfter1 + 1);
            setup.ShowGraph();
            setup.RemoveControlPoints();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test freeports on the padded border of an object at the outer limit of the graph bounding box.")]
        public void FreePorts_OnPaddedBorder()
        {
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(20, 20), new Point(100, 100)) 
                };

            // Create obstacles on the outer, padded border.
            var ports = new List<FloatingPort>();
            for (int ii = 0; ii < 1; ++ii)
            {
                ports.Add(MakeAbsoluteFreePort(new Point(19, 60)));
                ports.Add(MakeAbsoluteFreePort(new Point(60, 101)));
                ports.Add(MakeAbsoluteFreePort(new Point(101, 60)));
                ports.Add(MakeAbsoluteFreePort(new Point(60, 19)));
            }
            DoRouting(obstacles, null /*routings*/, ports);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test freeports on the padded border of an object at the outer limit of the graph bounding box, then create collinear freeports just outside them.")]
        public void FreePorts_OnPaddedBorder_Plus_Collinear_Outer()
        {
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(20, 20), new Point(100, 100)) 
                };

            // This starts out like FreePorts_OnPaddedBorder but adds collinear ports too,
            // away from the object along the FreePort-created PortSegment toward the sentinel.
            var ports = new List<FloatingPort>();
            for (int ii = 0; ii < 1; ++ii)
            {
                ports.Add(MakeAbsoluteFreePort(new Point(19, 60)));
                ports.Add(MakeAbsoluteFreePort(new Point(18.5, 60)));

                ports.Add(MakeAbsoluteFreePort(new Point(60, 101)));
                ports.Add(MakeAbsoluteFreePort(new Point(60, 101.5)));

                ports.Add(MakeAbsoluteFreePort(new Point(101, 60)));
                ports.Add(MakeAbsoluteFreePort(new Point(101.5, 60)));

                ports.Add(MakeAbsoluteFreePort(new Point(60, 19)));
                ports.Add(MakeAbsoluteFreePort(new Point(60, 18.5)));
            }
            DoRouting(obstacles, null /*routings*/, ports);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple test with two squares.")]
        public void TwoSquares()
        {
            var shapes = CreateTwoTestSquares();
            this.DoRouting(shapes, this.CreateRoutingBetweenObstacles(shapes, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple test with two squares and outer sentinel rectangles.")]
        public void TwoSquaresWithSentinels()
        {
            var obstacles = CreateTwoTestSquaresWithSentinels();
            var router = CreateRouter(obstacles);
            ShowGraph(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test two collinear freeports.")]
        public void FreePorts_OnSameLine()
        {
            var obstacles = CreateTwoTestSquaresWithSentinels();
            var abox = obstacles[0].BoundingBox; // left square

            // Create the second FreePort on the horizontal line of the first freeport.
            // This means that the second FreePort will not coincide with an existing
            // VisibilityVertex, so no restoration of that VisibilityVertex is needed.
            var loc1 = new Point(abox.Right + 10, abox.Bottom + 10);
            var freePort1 = MakeAbsoluteFreePort(loc1);
            var loc2 = new Point(abox.Right + 20, abox.Bottom + 10);
            var freePort2 = MakeAbsoluteFreePort(loc2);
            var portA = MakeAbsoluteObstaclePort(obstacles[0], abox.Center);

            var router = CreateRouter(obstacles);

            AddRoutingPorts(router, portA, freePort1);
            AddRoutingPorts(router, portA, freePort2);

            // Add the ports, tracking the vertex counts before and after each port addition.
            router.GenerateVisibilityGraph();
            ShowGraph(router);
            int numberOfVerticesBefore = router.VisibilityGraph.VertexCount;
            router.AddPortToVisibilityGraph(freePort1);
            ShowGraph(router);
            int numberOfVerticesAfter1 = router.VisibilityGraph.VertexCount;

            // This should add the freePoint location, the 4 intersections on the surrounding obstacles,
            // and 2 intersections on ScanSegments at the lower and upper boundaries of the rectangles.
            Validate.AreEqual(numberOfVerticesBefore + 7, numberOfVerticesAfter1, "number of vertices is not expected value");
            router.AddPortToVisibilityGraph(freePort2);
            ShowGraph(router);

            // This should add the freePoint location, 2 more intersections on the upper and lower obstacles
            // (the lateral obstacle intersections are already there), and 2 more intersections on ScanSegments
            // at the lower and upper boundaries of the rectangles.
            Validate.AreEqual(numberOfVerticesAfter1 + 5, router.VisibilityGraph.VertexCount, "number of vertices is not expected value");
            router.RemoveAllControlPointsFromVisibilityGraph();
            Validate.AreEqual(numberOfVerticesBefore, router.VisibilityGraph.VertexCount, "number of vertices is not expected value");
        }

        [TestMethod]
        [Timeout(5 * 1000)]
        [Description("Test multiple collinear freeports.")]
        [TestCategory("NonRollingBuildTest")]
        public void Multiple_Collinear_FreePorts_RouteFromObstacle0()
        {
            Multiple_Collinear_FreePorts_Worker(0);
        }

        [TestMethod]
        [Ignore]    // TODObug: repeated SolverShell loop in nudger, currently takes 2.5 minutes in release.
        [Timeout(15 * 1000)]
        [Description("Test multiple collinear freeports.")]
        [TestCategory("NonRollingBuildTest")]
        public void Multiple_Collinear_FreePorts_RouteFromAllObstacles()
        {
            Multiple_Collinear_FreePorts_Worker(DefaultSourceOrdinal);
        }

        public void Multiple_Collinear_FreePorts_Worker(int sourceOrdinal)
        {
            var obstacles = CreateTwoTestSquaresWithSentinels();

            // Create 5 floating ports per line across 10 lines.
            Rectangle leftRect = obstacles[0].BoundingBox;
            leftRect.Right += RouterPadding;
            Rectangle rightRect = obstacles[1].BoundingBox;
            rightRect.Left -= RouterPadding;
            const int NumSegments = 10;
            const int NumPortsPerSergment = 20;
            double verticalInterval = (leftRect.Top - leftRect.Bottom) / (NumSegments - 1);
            double horizontalInterval = (rightRect.Left - leftRect.Right) / (NumPortsPerSergment - 1);
            var ports = new List<FloatingPort>();
            for (var ii = 0; ii < NumSegments; ++ii)
            {
                for (var jj = 0; jj < NumPortsPerSergment; ++jj)
                {
                    ports.Add(MakeAbsoluteFreePort(leftRect.RightBottom + new Point(jj * horizontalInterval, ii * verticalInterval)));
                }
            }
            DoRouting(obstacles, CreateSourceToFreePortRoutings(obstacles, sourceOrdinal, ports));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiple freeports collinear with obstacle center ports.")]
        public void Collinear_Center_Ports()
        {
            // Put the ports at the midpoint.
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var abox = obstacles[0].BoundingBox; // left square
            var sourcePort = MakeAbsoluteObstaclePort(obstacles[0], abox.Center);

            var bbox = obstacles[1].BoundingBox; // right square
            var targetPort = MakeAbsoluteObstaclePort(obstacles[1], bbox.Center);

            var routings = new List<EdgeGeometry> { CreateRouting(sourcePort, targetPort) };

            DoRouting(obstacles, routings);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test ports on the borders on the opposite sides of the objects being routed between.")]
        public void Outer_Border_Ports()
        {
            // Put the ports on the outer border (the border away from the neighbour).
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var abox = obstacles[0].BoundingBox; // left square
            var sourcePort = MakeAbsoluteObstaclePort(obstacles[0], (abox.Center - new Point(abox.Width / 2, 0)));

            var bbox = obstacles[1].BoundingBox; // right square
            var targetPort = MakeAbsoluteObstaclePort(obstacles[1], (bbox.Center + new Point(bbox.Width / 2, 0)));

            var routings = new List<EdgeGeometry> { CreateRouting(sourcePort, targetPort) };

            DoRouting(obstacles, routings);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test ports on the borders on the top sides of the objects being routed between.")]
        public void Top_Border_Ports()
        {
            // Put the ports on the top border.
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var abox = obstacles[0].BoundingBox; // left square
            var sourcePort = MakeAbsoluteObstaclePort(obstacles[0], (abox.Center + new Point(0, abox.Height / 2)));

            var bbox = obstacles[1].BoundingBox; // right square
            var targetPort = MakeAbsoluteObstaclePort(obstacles[1], (bbox.Center + new Point(0, bbox.Height / 2)));

            var routings = new List<EdgeGeometry> { CreateRouting(sourcePort, targetPort) };

            DoRouting(obstacles, routings);
        }

        
        
        
        [TestMethod]
        [Timeout(2000)]
        [Description("Test movement of freeports.")]
        public void Update_FreePort()
        {
            // Tests that we leave the visibility graph intact after removing a freeport
            // that was on another freeport line.
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            var abox = a.BoundingBox;
            var bbox = b.BoundingBox;

            // Create a FreePort, then "drag" it by calling ReplaceEdgeGeometryToRoute.
            var loc1 = new Point(abox.Right + 10, abox.Center.Y + 10);
            var freePort1 = MakeAbsoluteFreePort(loc1);
            var portA = MakeAbsoluteObstaclePort(a, abox.Center);
            var portB = MakeAbsoluteObstaclePort(b, bbox.Center);

            var router = CreateRouter(obstacles);

            // Add the simple obstacle-to-obstacle port.
            AddRoutingPorts(router, portA, portB);

            // Manually add and adjust the moving FreePort.
            EdgeGeometry priorEg = CreateRouting(portA, freePort1);
            router.AddEdgeGeometryToRoute(priorEg);

            // Add the ports, tracking the vertex counts before and after each port addition.
            // When we do the re-routing in ReplaceEdgeGeometry, we'll remove portB from the VG,
            // so take it out and then put it back in as a test.
            int numberOfVerticesBefore = router.VisibilityGraph.VertexCount;
            router.Run();
            Validate.AreEqual(numberOfVerticesBefore, router.VisibilityGraph.VertexCount, "mismatched vertex count after RouteEdges");
            // ii and jj start at 0 so the first ShowGraph will be the initial freeport location.
            for (int ii = 0; ii < 2; ++ii)
            {
                for (int jj = 0; jj < 5; ++jj)
                {
                    var newLoc = loc1 + new Point(ii * 3, jj * 4);
                    var newPort = MakeAbsoluteFreePort(newLoc);
                    EdgeGeometry newEg = CreateRouting(portA, newPort);
                    router.RemoveEdgeGeometryToRoute(priorEg);
                    router.AddEdgeGeometryToRoute(newEg);
                    Validate.AreEqual(2, router.EdgeGeometriesToRoute.Count(), "Expected 2 EdgeGeometries");
                    Validate.AreEqual(numberOfVerticesBefore, router.VisibilityGraph.VertexCount, "VertexCount should not have changed");
                    priorEg = newEg;
                    router.Run();
                    ShowIncrementalGraph(router);
                }
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test movement of freeports without router.UpdateObstacles().")]
        public void UpdatePortPosition_Without_UpdateObstacles()
        {
            // Tests auto-detection of changes in Shape.Ports membership.
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            var abox = a.BoundingBox;

            // PortA does not change.
            var portA = MakeAbsoluteObstaclePort(a, abox.Center);

            // Create portB in a way that will allow us to modify its CenterDelegate.
            var offset = new Point(0, 0);
            // ReSharper disable AccessToModifiedClosure
            var portB = new RelativeFloatingPort(
                () => b.BoundaryCurve, () => b.BoundingBox.Center + offset, new Point(0, 0));
            // ReSharper restore AccessToModifiedClosure
            b.Ports.Insert(portB);

            // Show the first graph.
            var router = CreateRouter(obstacles);
            AddRoutingPorts(router, portA, portB);
            router.Run();
            ShowGraph(router);

            // Now move the Port around inside the obstacle and verify it draws correctly.
            offset = new Point(-5, b.BoundingBox.Top - b.BoundingBox.Center.Y);
            router.Run();
            ShowGraph(router);

            offset = new Point(-10, b.BoundingBox.Bottom - b.BoundingBox.Center.Y);
            router.Run();
            ShowGraph(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test adding and removing of ports without router.UpdateObstacles().")]
        public void AddRemovePorts_Without_UpdateObstacles()
        {
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            var abox = a.BoundingBox;

            // PortA does not change.
            var portA = MakeAbsoluteObstaclePort(a, abox.Center);

            // Create portB in a way that will allow us to modify its CenterDelegate.
            var portB = new RelativeFloatingPort(() => b.BoundaryCurve, () => b.BoundingBox.Center, new Point(0, 0));
            b.Ports.Insert(portB);

            // Show the first graph.
            var router = CreateRouter(obstacles);
            AddRoutingPorts(router, portA, portB);
            router.Run();
            ShowGraph(router);

            // Now add two more Ports and verify it draws correctly.
            var offsetC = new Point(-5, b.BoundingBox.Top - b.BoundingBox.Center.Y);
            var portC = new RelativeFloatingPort(
                () => b.BoundaryCurve, () => b.BoundingBox.Center + offsetC, new Point(0, 0));
            b.Ports.Insert(portC);
            var offsetD = new Point(-10, b.BoundingBox.Bottom - b.BoundingBox.Center.Y);
            var portD = new RelativeFloatingPort(
                () => b.BoundaryCurve, () => b.BoundingBox.Center + offsetD, new Point(0, 0));
            b.Ports.Insert(portD);
            var egC = AddRoutingPorts(router, portA, portC);
            var egD = AddRoutingPorts(router, portA, portD);
            router.Run();
            ShowGraph(router);

            // Now remove the two edges we added and verify it draws correctly.
            b.Ports.Remove(portC);
            b.Ports.Remove(portD);
            router.RemoveEdgeGeometryToRoute(egC);
            router.RemoveEdgeGeometryToRoute(egD);
            router.Run();
            ShowGraph(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test movement of non-relative obstacle ports with no explicit router.UpdateObstacles().")]
        public void MoveOneObstacle_ManuallyUpdateAbsolutePorts()
        {
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            var abox = a.BoundingBox;
            var bbox = b.BoundingBox;

            // Create ObstaclePorts for each obstacle, and a FreePort, then "drag" the first obstacle
            // rightward by calling UpdateObstacle.  We verify that it still routes correctly to
            // ObstacleB and FreePort1.
            var loc1 = new Point(bbox.Left - 10, bbox.Center.Y + 10);
            var freePort1 = MakeAbsoluteFreePort(loc1);
            var portA = MakeAbsoluteFreePort(abox.Center);
            var portB = MakeAbsoluteFreePort(bbox.Center);

            var router = CreateRouter(obstacles);
            router.GenerateVisibilityGraph();
            int numberOfVerticesBefore = router.VisibilityGraph.VertexCount;

            // Add the simple obstacle-to-obstacle port and the freePort.
            var connectAtoB = AddRoutingPorts(router, portA, portB);
            var connectBto1 = AddRoutingPorts(router, portB, freePort1);

            // Route the paths, which will generate the visibility graph if necessary.
            // No routing happens until we call RouteEdges().
            router.Run();
            ShowGraph(router);

            // The first movement is to shrink the obstacle just so it'll fit inside the sentinel.
            var offset = new Point(10, 10);
            Shape newB = PolylineFromRectanglePoints(bbox.Center - offset, bbox.Center + offset);
            var replacedObstacles = new List<Tuple<Shape, Shape>> { new Tuple<Shape, Shape>(obstacles[1], newB) };

            // Remove relevant Port, update obstacles, and re-calculate and re-add the relevant Port.
            // TODOperf: try to avoid iterating the entire list of ports to find the nullpaths
            router.RemoveEdgeGeometryToRoute(connectAtoB);
            router.RemoveEdgeGeometryToRoute(connectBto1);
            Validate.AreEqual(numberOfVerticesBefore, router.VisibilityGraph.VertexCount, "Expected unchanged vertex count");

            // The shrinkage will introduce 8 new vertices.  There are no ports yet to replace those we removed.
            ReplaceObstacles(replacedObstacles, router);
            numberOfVerticesBefore += 8;
            if (!this.UseSparseVisibilityGraph) 
            {
                Validate.AreEqual(numberOfVerticesBefore, router.VisibilityGraph.VertexCount, "Expected 8 new vertices");
            }

            // Add the new ports and route them.
            bbox = newB.BoundingBox;
            var newPortB = MakeAbsoluteFreePort(bbox.Center);
            connectAtoB = AddRoutingPorts(router, portA, newPortB);
            connectBto1 = AddRoutingPorts(router, newPortB, freePort1);
            if (!this.UseSparseVisibilityGraph)
            {
                Validate.AreEqual(numberOfVerticesBefore, router.VisibilityGraph.VertexCount, "Expected unchanged vertex count");
            }
            router.Run();
            ShowGraph(router);

            for (int ii = 0; ii < 5; ++ii)
            {
                newB.BoundaryCurve.Translate(new Point(-5, -5));

                router.RemoveEdgeGeometryToRoute(connectAtoB);
                router.RemoveEdgeGeometryToRoute(connectBto1);
                bbox = newB.BoundingBox;
                newPortB = MakeAbsoluteFreePort(bbox.Center);
                connectAtoB = AddRoutingPorts(router, portA, newPortB);
                connectBto1 = AddRoutingPorts(router, newPortB, freePort1);
                if (!this.UseSparseVisibilityGraph)
                {
                    Validate.AreEqual(numberOfVerticesBefore, router.VisibilityGraph.VertexCount, "VisibilityVertex count mismatch after movement");
                }
                router.UpdateObstacle(newB);
                router.Run();
                ShowGraph(router);
            }
        }

        private static void ReplaceObstacles(List<Tuple<Shape, Shape>> newObstacles, RectilinearEdgeRouter router)
        {
            foreach (var newObstacle in newObstacles)
            {
                router.RemoveObstacle(newObstacle.Item1);
                router.AddObstacle(newObstacle.Item2);
            }
        }

        private static void ReplaceObstaclesAndRouteEdges(
            List<Tuple<Shape, Shape>> newObstacles, RectilinearEdgeRouter router)
        {
            ReplaceObstacles(newObstacles, router);
            router.Run();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test movement of obstacle that causes a freeport to be inside then outside of the obstacle.")]
        public void MoveOneObstacle_NoUpdateAbsolutePorts_FreePoint_InAndOut()
        {
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            var abox = a.BoundingBox;
            var bbox = b.BoundingBox;

            // Create ObstaclePorts for each obstacle, and a FreePort, then "drag" the first obstacle
            // rightward by calling router.UpdateObstacle.  We aren't moving Ports, so just verify we don't
            // blow up and they route OK regardless of whether the freeport is inside or outside an obstacle.
            var loc1 = new Point(bbox.Left - 10, bbox.Center.Y + 10);
            var freePort1 = MakeAbsoluteFreePort(loc1);
            var portA = MakeAbsoluteFreePort(abox.Center);
            var portB = MakeAbsoluteFreePort(bbox.Center);

            var router = CreateRouter(obstacles);
            router.GenerateVisibilityGraph();

            // Add the simple obstacle-to-obstacle port and the freePort.
            AddRoutingPorts(router, portA, portB);
            AddRoutingPorts(router, portB, freePort1);

            // Route the paths, which will generate the visibility graph, which will cause 
            // the router to update the VisibilityGraph every time we move the obstacle.
            router.Run();
            ShowGraph(router);

            // Use ReplaceObstacles to shrink the obstacle just so it'll fit inside the sentinel.
            var offset = new Point(20, 20);
            Shape newB = PolylineFromRectanglePoints(bbox.Center - offset, bbox.Center + offset);
            newB.BoundaryCurve.Translate(new Point(-15, -15));
            var newObstacles = new List<Tuple<Shape, Shape>> { new Tuple<Shape, Shape>(obstacles[1], newB) };

            // Remove relevant Port, update obstacles, and re-calculate and re-add the relevant Port.
            ReplaceObstaclesAndRouteEdges(newObstacles, router);
            ShowGraph(router);

            for (int ii = 0; ii < 3; ++ii)
            {
                newB.BoundaryCurve.Translate(new Point(-5, -5));
                router.UpdateObstacle(newB);
                router.Run();
                ShowGraph(router);
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test automatic update of absolute obstacle ports when the obstacle is moved.")]
        public void MoveOneObstacle_AutomaticallyUpdateAbsolutePorts()
        {
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            var abox = a.BoundingBox;
            var bbox = b.BoundingBox;

            // Create ObstaclePorts for each obstacle, and a FreePort, then "drag" the first obstacle rightward
            // by calling ReplaceObstacles.  We verify that we route to the correct port positions.
            var loc1 = new Point(bbox.Left - 10, bbox.Center.Y + 10);
            var freePort1 = MakeAbsoluteFreePort(loc1);
            var portA = MakeAbsoluteObstaclePort(a, abox.Center);
            var portB = MakeAbsoluteObstaclePort(b, bbox.Center);

            var router = CreateRouter(obstacles);

            // Add the simple obstacle-to-obstacle port and the freePort.
            var connectAtoB = AddRoutingPorts(router, portA, portB);
            var connectBto1 = AddRoutingPorts(router, portB, freePort1);

            // Route the paths, which will generate the visibility graph, which will cause 
            // the router to update the VisibilityGraph every time we move the obstacle.
            router.Run();
            ShowGraph(router);

            // The first movement is to shrink the obstacle just so it'll fit inside the sentinel.
            // Though we're testing automatic updates here, we can't re-assign offsets to a relative
            // node, so we must make a new Port.
            var offset = new Point(10, 10);
            Shape newB = PolylineFromRectanglePoints(bbox.Center - offset, bbox.Center + offset);
            var newObstacles = new List<Tuple<Shape, Shape>> { new Tuple<Shape, Shape>(b, newB) };
            bbox = newB.BoundingBox;
            var newPortB = MakeAbsoluteObstaclePort(newB, bbox.Center);

            // We're no longer routing the old EdgeGeometries, as the current portB will go away, so
            // replace them in the EdgeGeometries list.  Remove them before the ReplaceObstacles call,
            // which means there will be no path routing for this ReplaceObstacles call; then add them
            // back after ReplaceObstacles, which will automatically re-route since we've called RouteEdges.
            router.RemoveEdgeGeometryToRoute(connectAtoB);
            router.RemoveEdgeGeometryToRoute(connectBto1);
            AddRoutingPorts(router, portA, newPortB);
            AddRoutingPorts(router, newPortB, freePort1);
            ReplaceObstaclesAndRouteEdges(newObstacles, router);
            ShowGraph(router);

            // Now move the obstacle.  This just moves the obstacle and makes sure that the code does
            // the right thing about whether the port (at its absolute position) is inside or outside
            // the obstacle during obstacle update; the CurveDelegate's indirection through the
            // shape ensures we get the right results here and we just reuse the shape object.
            newObstacles.Clear();
            newObstacles.Add(new Tuple<Shape, Shape>(newB, newB));

            for (int ii = 0; ii < 5; ++ii)
            {
                // Note:  Get the "unRelatedPort inside Obstacle" assert by changing this to -15, 5.
                newB.BoundaryCurve.Translate(new Point(-5, -5));
                bbox = newB.BoundingBox;
                newB.Ports.Remove(newPortB);
                newPortB = MakeAbsoluteObstaclePort(newB, bbox.Center);
                newB.Ports.Insert(newPortB);

                // ReplaceObstacles will automatically do the routing as the Port object hasn't changed.
                ReplaceObstaclesAndRouteEdges(newObstacles, router);
                ShowGraph(router);
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test automatic update of relative obstacle ports when the obstacle is moved.")]
        public void MoveOneObstacle_AutomaticallyUpdateRelativePorts()
        {
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            var abox = a.BoundingBox;
            var bbox = b.BoundingBox;

            // Create ObstaclePorts for each obstacle, and a FreePort, then "drag" the first obstacle rightward
            // by calling ReplaceObstacles.  We verify that it still routes correctly to ObstacleB and FreePort1.
            var loc1 = new Point(bbox.Left - 10, bbox.Center.Y + 10);
            var freePort1 = MakeAbsoluteFreePort(loc1);
            var portA = MakeSingleRelativeObstaclePort(a, new Point(abox.Right - abox.Center.X, 0));
            var portB = MakeSingleRelativeObstaclePort(b, new Point(bbox.Left - bbox.Center.X, 0));

            var router = CreateRouter(obstacles);

            // Add the simple obstacle-to-obstacle port and the freePort.
            var connectAtoB = AddRoutingPorts(router, portA, portB);
            var connectBto1 = AddRoutingPorts(router, portB, freePort1);

            // Route the paths, which will generate the visibility graph, which will cause 
            // the router to update the VisibilityGraph every time we move the obstacle.
            router.Run();
            ShowGraph(router);

            // The first movement is to shrink the obstacle just so it'll fit inside the sentinel.
            // Though we're testing automatic updates here, we can't re-assign offsets to a relative
            // node, so we must make a new Port.
            var offset = new Point(10, 10);
            Shape newB = PolylineFromRectanglePoints(bbox.Center - offset, bbox.Center + offset);
            var newObstacles = new List<Tuple<Shape, Shape>> { new Tuple<Shape, Shape>(b, newB) };
            bbox = newB.BoundingBox;
            var newPortB = MakeSingleRelativeObstaclePort(newB, new Point(bbox.Left - bbox.Center.X, 0));

            // We're no longer routing the old EdgeGeometries, as the current portB will go away, so
            // replace them in the EdgeGeometries list.  Remove them before the ReplaceObstacles call,
            // which means there will be no path routing for this ReplaceObstacles call; then add them
            // back after ReplaceObstacles, which will automatically re-route since we've called RouteEdges.
            router.RemoveEdgeGeometryToRoute(connectAtoB);
            router.RemoveEdgeGeometryToRoute(connectBto1);
            AddRoutingPorts(router, portA, newPortB);
            AddRoutingPorts(router, newPortB, freePort1);
            ReplaceObstaclesAndRouteEdges(newObstacles, router);
            ShowGraph(router);

            // Now move the obstacle.  This will all be relative because the obstacle dimensions won't
            // change, so we won't need to replace ports; the CurveDelegate's indirection through the
            // shape ensures we get the right results here and we just reuse the shape object.
            newObstacles.Clear();
            newObstacles.Add(new Tuple<Shape, Shape>(newB, newB));

            int numberOfVerticesBefore = router.VisibilityGraph.VertexCount;
            for (int ii = 0; ii < 5; ++ii)
            {
                newB.BoundaryCurve.Translate(new Point(-5, -5));

                // ReplaceObstacles will automatically do the routing as the Port object hasn't changed.
                ReplaceObstaclesAndRouteEdges(newObstacles, router);
                if (!this.UseSparseVisibilityGraph) 
                {
                    Validate.AreEqual(numberOfVerticesBefore, router.VisibilityGraph.VertexCount, "VisibilityVertex count mismatch after movement");
                }
                ShowGraph(router);
            }
        }

        
        
        [TestMethod]
        [Timeout(2000)]
        [Description("Test two simple waypoints.")]
        public void Waypoints2()
        {
            RunSimpleWaypoints(2, multiplePaths:false, wantTopRect:true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test three simple waypoints.")]
        public void Waypoints3()
        {
            RunSimpleWaypoints(3, multiplePaths:false, wantTopRect:true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test four simple waypoints.")]
        public void Waypoints4()
        {
            RunSimpleWaypoints(4, multiplePaths:false, wantTopRect:true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test ten simple waypoints.")]
        public void Waypoints11()
        {
            RunSimpleWaypoints(11, multiplePaths:false, wantTopRect:true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test two waypoints with coinciding paths.")]
        public void Waypoints2_Multiple()
        {
            RunSimpleWaypoints(2, multiplePaths:true, wantTopRect:true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test three waypoints with coinciding paths.")]
        public void Waypoints3_Multiple()
        {
            RunSimpleWaypoints(3, multiplePaths:true, wantTopRect:true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test four waypoints with coinciding paths.")]
        public void Waypoints4_Multiple()
        {
            RunSimpleWaypoints(4, multiplePaths:true, wantTopRect:true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test ten waypoints with coinciding paths.")]
        public void Waypoints11_Multiple()
        {
            RunSimpleWaypoints(11, multiplePaths:true, wantTopRect:true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test two simple waypoints outside graph boundaries")]
        public void Waypoints2_Oob()
        {
            RunSimpleWaypoints(2, multiplePaths: false, wantTopRect:false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test four simple waypoints outside graph boundaries.")]
        public void Waypoints4_Oob()
        {
            RunSimpleWaypoints(4, multiplePaths: false, wantTopRect:false);
        }

        
        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of collinear OpenVertex and CloseVertex events.")]
        public void MultipleCollinearOpenAndCloseVertices()
        {
            // Add this offset to the start position of the interior open vertices so we get
            // them collinear.  This was the above triangle which had its apex at 15, 20.
            // {CloseVertexEvent (15 22.236068)}
            const double YAdd = 2.236068 * 2;
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(10, 10), new Point(15, 20), new Point(20, 10) }),
                    CurveFromPoints(new[] { new Point(20, 30 + YAdd), new Point(25, 20 + YAdd), new Point(30, 30 + YAdd) }),
                    CurveFromPoints(new[] { new Point(30, 10), new Point(35, 20), new Point(40, 10) }),
                    CurveFromPoints(new[] { new Point(40, 30 + YAdd), new Point(45, 20 + YAdd), new Point(50, 30 + YAdd) }),
                    CurveFromPoints(new[] { new Point(50, 10), new Point(55, 20), new Point(60, 10) })
                };

            var router = DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
            Validate.AreEqual(3, router.HorizontalScanLineSegments.Count(), "apexes are not collinear");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of collinear OpenVertex and CloseVertex events collinear with an intersection.")]
        public void CollinearOpenVertexAndIntersection()
        {
            // Add this offset to the start position of the interior open vertices so we get
            // them collinear.  This was the above triangle which had its apex at 15, 20.
            // {CloseVertexEvent (15 22.236068)}
            const double YAdd = 2.236068 * 2;
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(10, 10), new Point(15, 20), new Point(20, 10) }),
                    CurveFromPoints(new[] { new Point(20, 30 + YAdd), new Point(25, 20 + YAdd), new Point(30, 30 + YAdd) }),
                    CurveFromPoints(new[] { new Point(30, 10), new Point(40, 30), new Point(50, 10) }),
                    CurveFromPoints(new[] { new Point(36, 15), new Point(36, 25), new Point(50, 25), new Point(50, 15) })
                };

            var router = DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));

            // This includes the interior overlapped vertices and their non-overlapped projections to both sides.
            // Note that we create a convex hull so there are less than 2n horizontal ScanSegments.
            const int ExpectedNumberOfSegments = 5;
            Validate.AreEqual(ExpectedNumberOfSegments, router.HorizontalScanLineSegments.Count(), 
                            String.Format("apexes are not collinear; expected {0}, actual {1}",
                            ExpectedNumberOfSegments, router.HorizontalScanLineSegments.Count()));
        }

        // This must be enough to avoid "flat top/bottom" being true, but small enough that
        // the intersections through the border will remain at the same scanline-perpendicular
        // coordinate.
        private static readonly double FlatOffset = ApproximateComparer.DistanceEpsilon * 2;

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of a flat top side with multiple other obstacle sides crossing it.")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "This is the two-word usage.")]
        public void FlatTopSideWithMultipleCrosses()
        {
            FlatWorker(0 /*LeftBottom */, 0 /*LeftTop*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of a flat bottom side with multiple other obstacle sides crossing it.")]
        public void FlatBottomSideWithMultipleCrosses()
        {
            FlatWorker(0 /*LeftBottom */, 0 /*LeftTop*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat HighObstacleSide with multiple other obstacle sides crossing it.")]
        public void AlmostFlatHighSideWithMultipleCrosses()
        {
            FlatWorker(0 /*LeftBottom */, FlatOffset /*LeftTop*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat LowObstacleSide with multiple other obstacle sides crossing it.")]
        public void AlmostFlatLowSideWithMultipleCrosses()
        {
            FlatWorker(FlatOffset /*LeftBottom */, 0 /*LeftTop*/);
        }

        private void FlatWorker(double leftBottomOffset, double leftTopOffset)
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[]
                            {
                                new Point(10, 10 + leftBottomOffset), new Point(10, 20 + leftTopOffset),
                                new Point(20, 20), new Point(20, 10)
                            })
                };

            // Multiple crossing obstacles that are close enough to the VertexEvent site
            // that the intersection will be scanline-collinear with it.
            const int Divisor = 2;
            for (int ii = 0; ii < Divisor; ++ii)
            {
                double inc = (double)ii / Divisor;
                obstacles.Add(CurveFromPoints(new[]
                            {
                                new Point(18.5 + inc, 15), new Point(23.5 + inc, 30), new Point(28.5 + inc, 15) 
                            }));
            }

            // Another obstacle crossing the lower ones, to cause scanline operations that should
            // discover any mismatched ordering due to missing overlap events.
            obstacles.Add(CurveFromPoints(new[] { new Point(18, 25), new Point(23, 40), new Point(28, 25) }));
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom LowObstacleSide with no overlaps.")]
        public void AlmostFlat_Open_LowSide_NoOverlap()
        {
            AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
                true /*isOpen*/, false /*wantInteriorOverlap*/, false /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom LowObstacleSide with an overlapping LowObstacleSide.")]
        public void AlmostFlat_Open_LowSide_InteriorLowOverlap()
        {
            // This hits the overlap when looking in the high direction for interior overlaps - so stops.  Needs to check for almost-flat.
            AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
                true /*isOpen*/, true /*wantInteriorOverlap*/, false /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom LowObstacleSide with an overlapping HighObstacleSide.")]
        public void AlmostFlat_Open_LowSide_InteriorLowNeighbor()
        {
            // Because it looks in the high direction first for an interior overlap, it finds this.
            AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
                true /*isOpen*/, false /*wantInteriorOverlap*/, true /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom LowObstacleSide with an overlapping LowObstacleSide and HighObstacleSide.")]
        public void AlmostFlat_Open_LowSide_InteriorLowOverlap_LowNeighbor()
        {
            // Because it looks in the high direction first for an interior overlap, it finds the low interior overlap and the right nbour.
            AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
                true /*isOpen*/, true /*wantInteriorOverlap*/, true /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom HighObstacleSide with no overlaps.")]
        public void AlmostFlat_Open_HighSide_NoOverlap()
        {
            AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
                true /*isOpen*/, false /*wantInteriorOverlap*/, false /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom HighObstacleSide with an overlapping HighObstacleSide.")]
        public void AlmostFlat_Open_HighSide_InteriorHighOverlap()
        {
            // Finds this as an interior side when looking toward the high side from lowSideNode.
            AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
                true /*isOpen*/, true /*wantInteriorOverlap*/, false /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom HighObstacleSide with an overlapping LowObstacleSide.")]
        public void AlmostFlat_Open_HighSide_InteriorHighNeighbor()
        {
            // Finds these as an interior side when looking toward the high side from lowSideNode.
            AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
                true /*isOpen*/, false /*wantInteriorOverlap*/, true /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom HighObstacleSide with an overlapping HighObstacleSide and LowObstacleSide.")]
        public void AlmostFlat_Open_HighSide_InteriorHighOverlap_HighNeighbor()
        {
            // Finds this as interior side nodes when looking toward the high side from lowSideNode.
            AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
                true /*isOpen*/, true /*wantInteriorOverlap*/, true /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top LowObstacleSide with no overlaps.")]
        public void AlmostFlat_Close_LowSide_NoOverlap()
        {
            AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
                false /*isOpen*/, false /*wantInteriorOverlap*/, false /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top LowObstacleSide with an overlapping LowObstacleSide.")]
        public void AlmostFlat_Close_LowSide_InteriorLowOverlap()
        {
            // Found as lowestOuterLowSideNode.
            AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
                false /*isOpen*/, true /*wantInteriorOverlap*/, false /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top LowObstacleSide with an overlapping HighObstacleSide.")]
        public void AlmostFlat_Close_LowSide_InteriorLowNeighbor()
        {
            // Found as lowNeighbor
            AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
                false /*isOpen*/, false /*wantInteriorOverlap*/, true /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top LowObstacleSide with an overlapping LowObstacleSide and HighObstacleSide.")]
        public void AlmostFlat_Close_LowSide_InteriorLowOverlap_LowNeighbor()
        {
            // Found as lowNeighbor and lowestOuterLowSideNode
            AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
                false /*isOpen*/, true /*wantInteriorOverlap*/, true /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top HighObstacleSide with no overlaps.")]
        public void AlmostFlat_Close_HighSide_NoOverlap()
        {
            AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
                false /*isOpen*/, false /*wantInteriorOverlap*/, false /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top HighObstacleSide with an overlapping HighObstacleSide.")]
        public void AlmostFlat_Close_HighSide_InteriorHighOverlap()
        {
            // Found as highestOuterHighSideNode
            AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
                false /*isOpen*/, true /*wantInteriorOverlap*/, false /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top HighObstacleSide with an overlapping LowObstacleSide.")]
        public void AlmostFlat_Close_HighSide_InteriorHighNeighbor()
        {
            // Found as highNeighbor
            AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
                false /*isOpen*/, false /*wantInteriorOverlap*/, true /*wantInteriorNeighbor*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top HighObstacleSide with an overlapping HighObstacleSide and LowObstacleSide.")]
        public void AlmostFlat_Close_HighSide_InteriorHighOverlap_HighNeighbor()
        {
            // Found as highestOuterHighSideNode and highNeighbor
            AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
                false /*isOpen*/, true /*wantInteriorOverlap*/, true /*wantInteriorNeighbor*/);
        }

        private void AlmostFlat_OpenOrClose_LowSide_InteriorHighOverlap_Worker(
            bool isOpen, bool wantInteriorOverlap, bool wantInteriorNeighbor)
        {
            // This tests short-circuiting of edges between multiple overlapping obstacles inside an
            // encompassing overlapping obstacle.
            var obstacles = new List<Shape>
                {
                    // Fixed neighbour.
                    PolylineFromRectanglePoints(
                        new Point(10, isOpen ? -10 : 0),
                        new Point(20, isOpen ? 30 : 40)),

                    // Object with almost-flat sides: LowSide is almost flat.
                    CurveFromPoints(new[]
                            {
                                new Point(40, 10 + (isOpen ? ApproximateComparer.DistanceEpsilon : 0.0)),
                                new Point(40, 20),
                                new Point(50, 20 + (!isOpen ? ApproximateComparer.DistanceEpsilon : 0.0)),
                                new Point(50, 10)
                            }),

                    // An obstacle that may overlap the event vertex and part of the interior of the almost-flat side.
                    PolylineFromRectanglePoints(
                        new Point(wantInteriorOverlap ? 49.5 : 54, 5),
                        new Point(55, 25)),

                    // An obstacle that may be an interior neighbour that stops the scan segment from the event vertex.
                    PolylineFromRectanglePoints(
                        new Point(35, 5), new Point(wantInteriorNeighbor ? 45.5 : 36, 25))
                };

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        private void AlmostFlat_OpenOrClose_HighSide_InteriorHighOverlap_Worker(
            bool isOpen, bool wantInteriorOverlap, bool wantInteriorNeighbor)
        {
            // This tests short-circuiting of edges between multiple overlapping obstacles inside an
            // encompassing overlapping obstacle.
            var obstacles = new List<Shape>
                {
                    // Fixed neighbour.
                    PolylineFromRectanglePoints(
                        new Point(60, isOpen ? -10 : 0),
                        new Point(70, isOpen ? 30 : 40)),

                    // Object with almost-flat sides: HighSide is almost flat.
                    CurveFromPoints(new[]
                            {
                                new Point(40, 10),
                                new Point(40, 20 + (!isOpen ? ApproximateComparer.DistanceEpsilon : 0.0)),
                                new Point(50, 20),
                                new Point(50, 10 + (isOpen ? ApproximateComparer.DistanceEpsilon : 0.0))
                            }),

                    // An obstacle that may overlap the event vertex and part of the interior of the almost-flat side.
                    PolylineFromRectanglePoints(
                        new Point(35, 5),
                        new Point(wantInteriorOverlap ? 40.5 : 36, 25)),

                    // An obstacle that may be an interior neighbour that stops the scan segment from the event vertex.
                    PolylineFromRectanglePoints(
                        new Point(wantInteriorNeighbor ? 44.5 : 54, 5), new Point(55, 25))
                };

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat bottom sides.")]
        public void AlmostFlat_MultipleBottomInversion1()
        {
            this.AlmostFlat_MultipleInversion_Worker(1, /*isBottom:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat bottom sides.")]
        public void AlmostFlat_MultipleBottomInversion2()
        {
            this.AlmostFlat_MultipleInversion_Worker(2, /*isBottom:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat bottom sides.")]
        public void AlmostFlat_MultipleBottomInversion3()
        {
            this.AlmostFlat_MultipleInversion_Worker(3, /*isBottom:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat bottom sides.")]
        public void AlmostFlat_MultipleBottomInversion4()
        {
            this.AlmostFlat_MultipleInversion_Worker(4, /*isBottom:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat bottom sides.")]
        public void AlmostFlat_MultipleBottomInversion5()
        {
            this.AlmostFlat_MultipleInversion_Worker(5, /*isBottom:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat bottom sides.")]
        public void AlmostFlat_MultipleBottomInversion6()
        {
            this.AlmostFlat_MultipleInversion_Worker(6, /*isBottom:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat bottom sides.")]
        public void AlmostFlat_MultipleBottomInversion7()
        {
            this.AlmostFlat_MultipleInversion_Worker(7, /*isBottom:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat top sides.")]
        public void AlmostFlat_MultipleTopInversion1()
        {
            this.AlmostFlat_MultipleInversion_Worker(1, /*isBottom:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat top sides.")]
        public void AlmostFlat_MultipleTopInversion2()
        {
            this.AlmostFlat_MultipleInversion_Worker(2, /*isBottom:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat top sides.")]
        public void AlmostFlat_MultipleTopInversion3()
        {
            this.AlmostFlat_MultipleInversion_Worker(3, /*isBottom:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat top sides.")]
        public void AlmostFlat_MultipleTopInversion4()
        {
            this.AlmostFlat_MultipleInversion_Worker(4, /*isBottom:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat top sides.")]
        public void AlmostFlat_MultipleTopInversion5()
        {
            this.AlmostFlat_MultipleInversion_Worker(5, /*isBottom:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat top sides.")]
        public void AlmostFlat_MultipleTopInversion6()
        {
            this.AlmostFlat_MultipleInversion_Worker(6, /*isBottom:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of multiple inverted side ordering on almost-flat top sides.")]
        public void AlmostFlat_MultipleTopInversion7()
        {
            this.AlmostFlat_MultipleInversion_Worker(7, /*isBottom:*/ false);
        }

        private void AlmostFlat_MultipleInversion_Worker(int mask, bool isBottom)
        {
            double epsilon = ApproximateComparer.DistanceEpsilon;
            var obstacles = new List<Shape>
            {
                // Top "plate" with almost-flat bottom sides at bottom point.
                CurveFromPoints(new[] 
                    {
                        new Point(0, 0), new Point(20, epsilon),  new Point(20, 8),
                        new Point(-20, 8), new Point(-20, epsilon)
                    })
            };

            if (0 != (mask & 1))
            {
                // Left and right upper inner intersecting triangles.
                obstacles.Add(CurveFromPoints(new[] { new Point(-6, -6), new Point(-4, 6), new Point(-8, 6) }));
                obstacles.Add(CurveFromPoints(new[] { new Point(6, -6), new Point(8, 6), new Point(4, 6) }));
            }

            if (0 != (mask & 2))
            {
                // Left and right lower inner intersecting triangles.
                obstacles.Add(CurveFromPoints(new[] { new Point(-4, -3), new Point(-1, -3), new Point(-2.5, 3) }));
                obstacles.Add(CurveFromPoints(new[] { new Point(1, -3), new Point(4, -3), new Point(2.5, 3) }));
            }

            if (0 != (mask & 4))
            {
                // Left and right lower outer triangles with almost-flat tops.  The topside slopes are calculated
                // to intersect the upper rectangle's bottom sides at {+/- 5, epsilon/3}, assuming padding 1.0.
                obstacles.Add(CurveFromPoints(new[] { new Point(-50, -10), new Point(85, -2 - epsilon), new Point(-50, -2 + epsilon) }));
                obstacles.Add(CurveFromPoints(new[] { new Point(50, -10), new Point(50, -2 + epsilon), new Point(-85, -2 - epsilon) }));
            }

            if (!isBottom)
            {
                // We are right at the epsilon boundary so avoid possible math rounding issues and
                // explicitly invert all Y coordinates.
                var obstacles2 = new List<Shape>();
                foreach (var shape in obstacles)
                {
                    var curve2 = new Curve();
                    foreach (var segment in ((Curve)shape.BoundaryCurve).Segments)
                    {
                        curve2.AddSegment(new LineSegment(new Point(segment.Start.X, -segment.Start.Y),
                                                          new Point(segment.End.X, -segment.End.Y)));
                    }
                    obstacles2.Add(new Shape(curve2) { UserData = shape.UserData });
                }
                obstacles = obstacles2;
            }

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 1, 2));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of two squares whose adjacent padded orders coincide.")]
        public void TouchingSquares()
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(10, 10), new Point(10, 20), new Point(20, 20), new Point(20, 10) }),
                    // Two units of separation to account for padding.
                    CurveFromPoints(new[] { new Point(22, 5), new Point(22, 25), new Point(32, 25), new Point(32, 5) })
                };

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("With no outer encompassing obstacle; test routing between two obstacles horizontally blocked by a large blocking obstacle with various other obstacles that create a short-circuit across the high-weight interior edges.")]
        public void InterOverlapShortCircuit_Low()
        {
            // There are obstacles at both low corners of the blocking obstacle, and the left corner obstacle is shorter than the blocking obstacle.
            InterOverlapShortCircuit_Worker(0, false /*wantMiddle*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("With no outer encompassing obstacle; test routing between two obstacles horizontally blocked by a large blocking obstacle with various other obstacles that create a short-circuit across the high-weight interior edges.")]
        public void InterOverlapShortCircuit_High()
        {
            // There are obstacles at both low corners of the blocking obstacle, and the left corner obstacle is taller than the blocking obstacle.
            InterOverlapShortCircuit_Worker(40, false /*wantMiddle*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("With no outer encompassing obstacle; test routing between two obstacles horizontally blocked by a large blocking obstacle with various other obstacles that create a short-circuit across the high-weight interior edges.")]
        public void InterOverlapShortCircuit_Low_Middle()
        {
            // There are obstacles at both low corners and in the middle of the blocking obstacle, and the left corner obstacle is shorter than the blocking obstacle.
            InterOverlapShortCircuit_Worker(0, true /*wantMiddle*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("With no outer encompassing obstacle; test routing between two obstacles horizontally blocked by a large blocking obstacle with various other obstacles that create a short-circuit across the high-weight interior edges.")]
        public void InterOverlapShortCircuit_High_Middle()
        {
            // There are obstacles at both low corners and in the middle of the blocking obstacle, and the left corner obstacle is taller than the blocking obstacle.
            InterOverlapShortCircuit_Worker(40, true /*wantMiddle*/);
        }

        private void InterOverlapShortCircuit_Worker(int extraHeight, bool wantMiddle)
        {
            OverlapShortCircuit_Worker(extraHeight, wantMiddle, false /*wantOuter*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("With an outer encompassing obstacle; test routing between two obstacles horizontally blocked by a large blocking obstacle with various other obstacles that create a short-circuit across the high-weight interior edges.")]
        public void IntraOverlapShortCircuit_Low()
        {
            // There are obstacles at both low corners of the blocking obstacle, and the left corner obstacle is shorter than the blocking obstacle.
            IntraOverlapShortCircuit_Worker(0, false /*wantMiddle*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("With an outer encompassing obstacle; test routing between two obstacles horizontally blocked by a large blocking obstacle with various other obstacles that create a short-circuit across the high-weight interior edges.")]
        public void IntraOverlapShortCircuit_High()
        {
            // There are obstacles at both low corners of the blocking obstacle, and the left corner obstacle is taller than the blocking obstacle.
            IntraOverlapShortCircuit_Worker(40, false /*wantMiddle*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("With an outer encompassing obstacle; test routing between two obstacles horizontally blocked by a large blocking obstacle with various other obstacles that create a short-circuit across the high-weight interior edges.")]
        public void IntraOverlapShortCircuit_Low_Middle()
        {
            // There are obstacles at both low corners and in the middle of the blocking obstacle, and the left corner obstacle is shorter than the blocking obstacle.
            IntraOverlapShortCircuit_Worker(0, true /*wantMiddle*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("With an outer encompassing obstacle; test routing between two obstacles horizontally blocked by a large blocking obstacle with various other obstacles that create a short-circuit across the high-weight interior edges.")]
        public void IntraOverlapShortCircuit_High_Middle()
        {
            // There are obstacles at both low corners and in the middle of the blocking obstacle, and the left corner obstacle is taller than the blocking obstacle.
            IntraOverlapShortCircuit_Worker(40, true /*wantMiddle*/);
        }

        private void IntraOverlapShortCircuit_Worker(int extraHeight, bool wantMiddle)
        {
            OverlapShortCircuit_Worker(extraHeight, wantMiddle, true /*wantOuter*/);
        }

        private void OverlapShortCircuit_Worker(int extraHeight, bool wantMiddle, bool wantOuter)
        {
            // This tests short-circuiting of edges between multiple overlapping obstacles inside an
            // encompassing overlapping obstacle.
            var obstacles = new List<Shape>
                {
                    // Source
                    CurveFromPoints(new[]
                            {
                                new Point(10, 10), new Point(10, 20),
                                new Point(20, 20), new Point(20, 10)
                            }),
                    // Target
                    CurveFromPoints(new[]
                            {
                                new Point(120, 10), new Point(120, 20),
                                new Point(130, 20), new Point(130, 10)
                            }),
                    // Big rectangle in the middle
                    CurveFromPoints(new[]
                            {
                                new Point(40, 10), new Point(40, 40),
                                new Point(100, 40), new Point(100, 10)
                            })
                };

            if (wantMiddle)
            {
                // Small rectangles in the middle (cutting border of big rectangle).
                obstacles.Add(CurveFromPoints(
                        new[] { new Point(60, 5), new Point(60, 15), new Point(65, 15), new Point(65, 5) }));
                obstacles.Add(CurveFromPoints(
                        new[] { new Point(75, 5), new Point(75, 15), new Point(80, 15), new Point(80, 5) }));
            }

            // Cover the lower corners of the big rectangle.
            obstacles.Add(CurveFromPoints(new[]
                        {
                            new Point(30, 5), new Point(30, 15 + extraHeight), new Point(50, 15 + extraHeight),
                            new Point(50, 5)
                        }));
            obstacles.Add(CurveFromPoints(
                    new[] { new Point(90, 5), new Point(90, 15), new Point(110, 15), new Point(110, 5) }));

            if (wantOuter)
            {
                // Make all the foregoing be nested inside another obstacle.
                obstacles.Add(CurveFromPoints(
                        new[] { new Point(0, 0), new Point(0, 50), new Point(140, 50), new Point(140, 0) }));
            }

            DoRouting(obstacles, this.CreateRoutingBetweenFirstTwoObstacles(obstacles));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Overlapping obstacles outside all four corners of a square, with one non-overlapping square at bottom middle.")]
        public void InterOverlap_AllBorders_H1()
        {
            InterOverlap_AllBorders_Worker(1, /*midHorizontal:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Overlapping obstacles outside all four corners of a square, with two overlapping squares at bottom middle.")]
        public void InterOverlap_AllBorders_H2()
        {
            InterOverlap_AllBorders_Worker(2, /*midHorizontal:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Overlapping obstacles outside all four corners of a square, with three overlapping squares at bottom middle.")]
        public void InterOverlap_AllBorders_H3()
        {
            InterOverlap_AllBorders_Worker(3, /*midHorizontal:*/ true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Overlapping obstacles outside all four corners of a square, with one non-overlapping squares at left middle.")]
        public void InterOverlap_AllBorders_V1()
        {
            InterOverlap_AllBorders_Worker(1, /*midHorizontal:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Overlapping obstacles outside all four corners of a square, with two overlapping squares at left middle.")]
        public void InterOverlap_AllBorders_V2()
        {
            InterOverlap_AllBorders_Worker(2, /*midHorizontal:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Overlapping obstacles outside all four corners of a square, with three overlapping squares at left middle.")]
        public void InterOverlap_AllBorders_V3()
        {
            InterOverlap_AllBorders_Worker(3, /*midHorizontal:*/ false);
        }

        private void InterOverlap_AllBorders_Worker(int midCount, bool midHorizontal)
        {
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(40, 40), new Point(100, 100)) 
                };

            // Big square in the middle
            Rectangle bbox = obstacles[0].BoundingBox;

            // Two outer squares at each corner.
            obstacles.Add(PolylineFromRectanglePoints(
                    bbox.LeftBottom + new Point(-30, -10), bbox.LeftBottom + new Point(-10, 10)));
            obstacles.Add(PolylineFromRectanglePoints(
                    bbox.LeftBottom + new Point(-10, -30), bbox.LeftBottom + new Point(10, -10)));

            obstacles.Add(PolylineFromRectanglePoints(bbox.LeftTop + new Point(-30, -10), bbox.LeftTop + new Point(-10, 10)));
            obstacles.Add(PolylineFromRectanglePoints(bbox.LeftTop + new Point(-10, 30), bbox.LeftTop + new Point(10, 10)));

            obstacles.Add(PolylineFromRectanglePoints(bbox.RightTop + new Point(10, -10), bbox.RightTop + new Point(30, 10)));
            obstacles.Add(PolylineFromRectanglePoints(bbox.RightTop + new Point(-10, 30), bbox.RightTop + new Point(10, 10)));

            obstacles.Add(PolylineFromRectanglePoints(
                    bbox.RightBottom + new Point(10, -10), bbox.RightBottom + new Point(30, 10)));
            obstacles.Add(PolylineFromRectanglePoints(
                    bbox.RightBottom + new Point(-10, -30), bbox.RightBottom + new Point(10, -10)));

            // Middle-overlaps in both directions.
            double width = (bbox.Left - bbox.Right) / ((2 * midCount) + 1);
            for (int ii = 0; ii < midCount; ++ii)
            {
                double parStart = width;
                double perpStart = width * (ii + 1);

                if (midHorizontal)
                {
                    obstacles.Add(PolylineFromRectanglePoints(
                            bbox.LeftBottom + new Point(-parStart, perpStart),
                            bbox.RightBottom + new Point(parStart, perpStart + width)));
                }
                else
                {
                    obstacles.Add(PolylineFromRectanglePoints(
                            bbox.LeftBottom + new Point(perpStart, -parStart),
                            bbox.LeftTop + new Point(perpStart + width, parStart)));
                }
            }

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Three rectangles with the left two adjoining sharing a portion of a vertical border.")]
        public void AdjoiningRectangles_Left()
        {
            this.AdjoiningRectangles_Worker(0, 5);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Three rectangles with the right two adjoining sharing a portion of a vertical border.")]
        public void AdjoiningRectangles_Right()
        {
            this.AdjoiningRectangles_Worker(5, 0);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Three rectangles with the left and right each sharing a portion of a vertical border with the middle.")]
        public void AdjoiningRectangles_Both()
        {
            this.AdjoiningRectangles_Worker(0, 0);
        }

        private void AdjoiningRectangles_Worker(double leftSpace, double rightSpace)
        {
            // Make sure no Asserts fail and that we have proper overlapping when either or both sides have intervening obstacles.
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(30 - leftSpace, 10), new Point(50 - leftSpace, 30)),
                    PolylineFromRectanglePoints(new Point(52, 4), new Point(88, 15)),
                    PolylineFromRectanglePoints(new Point(90 + rightSpace, 10), new Point(110 + rightSpace, 30))
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Adjoining obstacles with overlaps with collinear CloseVertexEvents.")]
        public void AdjoiningObstacles_DipToOverlapped()
        {
            // Tests the gap between 1-3 and 1-5 does not cause problems when 2 has closed with 
            // overlap and 4 is trying to close.
            var obstacles = new List<Shape>
                {
                    // Big rectangle in the middle
                    PolylineFromRectanglePoints(new Point(48, 6), new Point(92, 32)),
                    // Left wing
                    PolylineFromRectanglePoints(new Point(30, 10), new Point(50, 30)),
                    PolylineFromRectanglePoints(new Point(10, 30), new Point(30, 50)),
                    // Right wing
                    PolylineFromRectanglePoints(new Point(90, 10), new Point(110, 30)),
                    PolylineFromRectanglePoints(new Point(110, 30), new Point(130, 50))
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Adjoining obstacles with overlaps with OpenVertexEvent collinear with CloseVertexEvent.")]
        public void AdjoiningObstacles_DipToOverlapped_Collinear_CloseOpen()
        {
            // Tests the gap between 2-4 does not cause problems when 1 has closed with 
            // overlap and 3 opens collinear with the 1 close.
            var obstacles = new List<Shape>
                {
                    // Big rectangle in the middle
                    PolylineFromRectanglePoints(new Point(60, 86), new Point(64, 91)),
                    PolylineFromRectanglePoints(
                        new Point(65.5, 90), new Point(71, 99)),
                    PolylineFromRectanglePoints(new Point(72, 93), new Point(80, 105)),
                    PolylineFromRectanglePoints(new Point(78, 85), new Point(84, 94))
                };

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Semi-landlocked obstacle with a border collinear with an overlapped border such that there are collinear overlapped/nonoeverlapped ScanSegments.")]
        public void Landlocked_OverlapSide_NonAdjoining()
        {
            var obstacles = new List<Shape>
                {
                    // With ConvexHulls this now routes outside the hull, but the path passes across obstacles inside the hull.
                    // If a crossed obstacle is the source or destination, then the path will be trimmed by CalculateArrowheads
                    // to the first intersection with the crossed obstacle; for example, the Diamond-to-littleRectangle path passes
                    // across the diamond so is trimmed from the 5-segment curve that existed after nudging to a single segment.
                    CurveFromPoints(
                        new[] { new Point(30, 20), new Point(0, 50), new Point(30, 80), new Point(60, 50) }),
                    CurveFromPoints(
                        new[] { new Point(40, 70), new Point(10, 100), new Point(40, 130), new Point(70, 100) }),

                    // This shape makes it easier to calculate the collinear lowSide with the (partially)
                    // landlocked obstacle.
                    CurveFromPoints(new[]
                            {
                                new Point(105, 40), new Point(55, 95), new Point(55, 115), new Point(155, 115),
                                new Point(155, 95)
                            }),

                    // The poor little old partially landlocked obstacle.
                    PolylineFromRectanglePoints(new Point(55, 63), new Point(65, 73))
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Obstacle with a border sharing part of the non-overlapped portion of a partially-overlapped obstacle's border.")]
        public void Landlocked_OverlapSide_Adjoining()
        {
            var obstacles = new List<Shape>
                {
                    // Test that the little rectangle that is collinear with overlapped sides doesn't cause
                    // problems - more precisely, the overlapped sides are correctly extended as non-overlapped
                    // when they leave the encompassing obstacle.
                    CurveFromPoints(new[] { new Point(30, 20), new Point(0, 50), new Point(30, 80), new Point(60, 50) }),
                    CurveFromPoints(new[] { new Point(40, 70), new Point(10, 100), new Point(40, 130), new Point(70, 100) }),
                    // High side (padded) collinear with landlocked obstacle's low side (padded).
                    PolylineFromRectanglePoints(new Point(10, 50), new Point(53, 110)),
                    // The poor little old partially landlocked obstacle.
                    PolylineFromRectanglePoints(new Point(55, 63), new Point(65, 73))
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        ////
        //// These paths look funny because they are overlapping obstacles and taking the shortest path which may be
        //// across the obstacle interior.
        ////

        [TestMethod]
        [Timeout(2000)]
        [Description("Adjoining obstacles overlapping the bottom of an obstacle with the unpadded border shared.")]
        public void OverlappedObstacles_InMiddleOfBottom_AdjoiningUnpadded_Inside()
        {
            OverlappedObstacles_InMiddleOfBottom_Worker(false, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Adjoining obstacles overlapping the bottom of an obstacle with the padded border shared.")]
        public void OverlappedObstacles_InMiddleOfBottom_AdjoiningPadded_Inside()
        {
            OverlappedObstacles_InMiddleOfBottom_Worker(true, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Adjoining obstacles overlapping the bottom of an obstacle and extending outside it, with the unpadded border shared.")]
        public void OverlappedObstacles_InMiddleOfBottom_AdjoiningUnpadded_Outside()
        {
            OverlappedObstacles_InMiddleOfBottom_Worker(false, true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Adjoining obstacles overlapping the bottom of an obstacle and extending outside it, with the padded border shared.")]
        public void OverlappedObstacles_InMiddleOfBottom_AdjoiningPadded_Outside()
        {
            OverlappedObstacles_InMiddleOfBottom_Worker(true, true);
        }

        private void OverlappedObstacles_InMiddleOfBottom_Worker(bool padded, bool outside)
        {
            var obstacles = new List<Shape>();
            double inpad = padded ? 1.0 : 0.0;
            double outpad = outside ? 20.0 : 0.0;

            // Two obstacles touching each other in the middle of the bottom of an obstacle they overlap.
            //     |    obs0   |
            //     ------|------
            //      |obs1|obs2|
            obstacles.Add(PolylineFromRectanglePoints(new Point(10, 50), new Point(50, 80)));
            obstacles.Add(PolylineFromRectanglePoints(new Point(20 - outpad, 40), new Point(30 - inpad, 60)));
            obstacles.Add(PolylineFromRectanglePoints(new Point(30 + inpad, 40), new Point(40 + outpad, 60)));

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiple identical objects of the same shape, size, and location.")]
        public void Coinciding_SameHeight3()
        {
            Coinciding_Worker(0, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiple identical objects of the same shape, size, and location, with nesting.")]
        public void Coinciding_SameHeight3_Nested()
        {
            Coinciding_Worker(0, true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiple identical objects of the same shape, of varying heights, at the same location.")]
        public void Coinciding_DifferentHeight3()
        {
            Coinciding_Worker(5, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiple identical objects of the same shape, of varying heights, at the same location, with nesting.")]
        public void Coinciding_DifferentHeight3_Nested()
        {
            Coinciding_Worker(5, true);
        }

        private void Coinciding_Worker(double growHeight, bool nested)
        {
            var obstacles = new List<Shape>
                {
                    // High side (padded) collinear with landlocked obstacle's low side (padded).
                    PolylineFromRectanglePoints(new Point(10, 50), new Point(50, 90 + growHeight)),
                    PolylineFromRectanglePoints(new Point(10, 50), new Point(50, 90 + (growHeight * 2))),
                    PolylineFromRectanglePoints(new Point(10, 50), new Point(50, 90 + (growHeight * 3))),
                    PolylineFromRectanglePoints(new Point(80, 60), new Point(100, 80))
                };

            if (nested)
            {
                // Create an encompassing obstacle.
                obstacles.Add(PolylineFromRectanglePoints(new Point(0, 40), new Point(60, 100 + (growHeight * 3))));
            }

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiple rectangles that overlap with shared open/close coordinates.")]
        public void Overlapped_Rectangles_With_Same_Open_And_Close_Coordinate()
        {
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(10, 50), new Point(50, 90)),
                    PolylineFromRectanglePoints(new Point(20, 50), new Point(60, 90)),
                    PolylineFromRectanglePoints(new Point(30, 50), new Point(70, 90)),
                    PolylineFromRectanglePoints(new Point(120, 60), new Point(140, 80))
                };

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verifies open/close sequencing of vertical segment opens and closes with respect to horizontal segment open/close between them.")]
        public void Connected_Vertical_Segments_Are_Intersected()
        {
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(10, 50), new Point(50, 90)),
                    PolylineFromRectanglePoints(new Point(20, 50), new Point(60, 90)),
                    PolylineFromRectanglePoints(new Point(30, 50), new Point(70, 90)),
                    PolylineFromRectanglePoints(new Point(120, 60), new Point(140, 80)),

                    // Add these to make exterior non-overlapped extensions of the overlapped segments.
                    PolylineFromRectanglePoints(new Point(0, 110), new Point(150, 120)),
                    PolylineFromRectanglePoints(new Point(0, 20), new Point(150, 30)),
                };

            var router = DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
            Validate.AreEqual(52, router.VisibilityGraph.VertexCount, "VertexCount");
            Validate.AreEqual(44, router.VisibilityGraph.Edges.Where(edge => !StaticGraphUtility.IsVertical(StaticGraphUtility.EdgeDirection(edge))).Count(), "HorizontalEdgeCount");
            Validate.AreEqual(42, router.VisibilityGraph.Edges.Where(edge => StaticGraphUtility.IsVertical(StaticGraphUtility.EdgeDirection(edge))).Count(), "VerticalEdgeCount");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test obstacle port just outside the obstacle unpadded boundary.")]
        public void Triangle_ObstaclePort_Outside_Obstacle()
        {
            var obstacles = new List<Shape>
                {
                    // The centre of this triangle is just outside the borders.
                    CurveFromPoints(new[]
                            {
                                new Point(101.634005539731, 56.3400765589772),
                                new Point(107.685715981301, 62.2845999050207),
                                new Point(101.745914212075, 62.2788268345073)
                            }),
                    PolylineFromRectanglePoints(new Point(80, 50), new Point(90, 60))
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test flatbottom fully overlapped by 3 adjoining obstacles sharing padded borders.")]
        public void FlatBottom_FullyOverlapped_WithAdjoiningOverlapNeighbors()
        {
            FlatBottom_FullyOverlapped_WithAdjoiningOverlapNeighbors_Worker(false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test flatbottom fully overlapped by 3 adjoining obstacles sharing padded borders, with the left and right corner obstacles being overlapped by other obstacles sharing the shared padded border.")]
        public void FlatBottom_FullyOverlapped_WithDupAdjoiningOverlapNeighbors()
        {
            FlatBottom_FullyOverlapped_WithAdjoiningOverlapNeighbors_Worker(true);
        }

        private void FlatBottom_FullyOverlapped_WithAdjoiningOverlapNeighbors_Worker(bool dup)
        {
            var obstacles = new List<Shape>
                {
                    // Flat bottom overlapped completely, with the overlapping rectangle having adjacent neighbours.
                    PolylineFromRectanglePoints(new Point(15, 55), new Point(37, 65)),
                    PolylineFromRectanglePoints(new Point(10, 50), new Point(20, 60))
                };

            if (dup)
            {
                obstacles.Add(PolylineFromRectanglePoints(new Point(12, 48), new Point(20, 62)));
            }
            obstacles.Add(PolylineFromRectanglePoints(new Point(22, 50), new Point(32, 60)));
            obstacles.Add(PolylineFromRectanglePoints(new Point(34, 50), new Point(44, 60)));
            if (dup)
            {
                obstacles.Add(PolylineFromRectanglePoints(new Point(34, 52), new Point(42, 62)));
            }
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a case that encountered an obstacle between the previousPoint and startVertex of a port splice.")]
        public void Overlap_Obstacle_Between_PreviousPoint_And_StartVertex_TargetAtTop()
        {
            // This will hit the Assert condition (if the assert's enabled) because the first connection
            // is before the limitrect.
            this.Overlap_Obstacle_Between_PreviousPoint_And_StartVertex_Worker(true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a case that encountered an obstacle between the previousPoint and startVertex of a port splice.")]
        public void Overlap_Obstacle_Between_PreviousPoint_And_StartVertex_TargetInsideLeft()
        {
            // This won't hit the Assert condition (if the assert's enabled) because the first connection
            // goes past the limitrect.
            this.Overlap_Obstacle_Between_PreviousPoint_And_StartVertex_Worker(false);
        }

        private void Overlap_Obstacle_Between_PreviousPoint_And_StartVertex_Worker(bool targetAtTop)
        {
            // Call with -ports 0 1 -route
            var obstacles = new List<Shape>
                {
                    // Create a "channel" in the scan segments across a border, by having the middle
                    // of a flatbottom be in the middle of two overlapping outer segments.  This will
                    // cause Port splicing to encounter gaps where border visibility should be.
                    PolylineFromRectanglePoints(new Point(10, -20), new Point(30, 20)),
                    targetAtTop
                        ? PolylineFromRectanglePoints(new Point(12, 65), new Point(17, 70))
                        : PolylineFromRectanglePoints(new Point(-15, 30), new Point(-10, 35)),
                    // Blocking upper obstacle
                    PolylineFromRectanglePoints(new Point(10, 50), new Point(30, 60)),
                    // Gap in top of source obstacle and bottom of blocking obstacle.
                    PolylineFromRectanglePoints(new Point(-25, 5), new Point(25, 55)),
                    PolylineFromRectanglePoints(new Point(15, 7), new Point(45, 53))
                };

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create gaps in border visibility and verify routing still works.")]
        public void Overlap_Gaps_On_All_Boundaries_TargetUpperLeft()
        {
            Overlap_Gaps_On_All_Boundaries_Worker(new Point(25, 115), new Point(35, 125));
        }

        private void Overlap_Gaps_On_All_Boundaries_Worker(Point targetLeftBottom, Point targetRightTop)
        {
            // Requires -ports 0 1 -route; caller passes in the targetrect bounding points.
            var obstacles = new List<Shape>
                {
                    // Create a "channel" in the scan segments across all four boundaries, by having the middle
                    // of the flat boundaries be in the middle of two overlapping outer segments.  This will
                    // cause Port splicing to encounter gaps where border visibility should be.
                    PolylineFromRectanglePoints(new Point(30, 30), new Point(120, 100)),
                    PolylineFromRectanglePoints(targetLeftBottom, targetRightTop),
                    // Left boundary gap
                    PolylineFromRectanglePoints(new Point(20, 50), new Point(40, 70)),
                    PolylineFromRectanglePoints(new Point(10, 60), new Point(50, 80)),
                    // Top boundary gap
                    PolylineFromRectanglePoints(new Point(60, 90), new Point(80, 110)),
                    PolylineFromRectanglePoints(new Point(70, 80), new Point(90, 120)),
                    // Right boundary gap
                    PolylineFromRectanglePoints(new Point(110, 50), new Point(130, 70)),
                    PolylineFromRectanglePoints(new Point(100, 60), new Point(140, 80)),
                    // Bottom boundary gap
                    PolylineFromRectanglePoints(new Point(60, 20), new Point(80, 40)),
                    PolylineFromRectanglePoints(new Point(70, 10), new Point(90, 50))
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Two dead-ends of ScanSegments strike an angled side of an obstacle, creating open space.")]
        public void DeadEnd_OpenSpace_ObstaclePort0()
        {
            // Call with -ports 0 1 -route.
            var obstacles = DeadEnd_OpenSpace_Obstacles();
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Two dead-ends of ScanSegments strike an angled side of an obstacle, creating open space.")]
        public void DeadEnd_OpenSpace_FreePort0()
        {
            // Call with -ports -1 free -route.
            var obstacles = DeadEnd_OpenSpace_Obstacles();
            var freePorts = new List<FloatingPort> { MakeAbsoluteFreePort(new Point(40, 50)) };
            DoRouting(obstacles, CreateSourceToFreePortRoutings(obstacles, -1, freePorts));
        }

        private List<Shape> DeadEnd_OpenSpace_Obstacles()
        {
            // Two dead-ends of scansegs strike an angled side of an obstacle, creating open space.
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(70, 30), new Point(30, 70), new Point(110, 70) }),
                    PolylineFromRectanglePoints(new Point(0, 60), new Point(20, 80)),
                    PolylineFromRectanglePoints(new Point(60, 0), new Point(80, 20))
                };

            return obstacles;
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Two dead-ends of ScanSegments strike an angled side of an obstacle, creating open space; verify no extraneous bends exist.")]
        public void DeadEnd_OpenSpace_ObstaclePort0_EliminateExtraBend()
        {
            // Two dead-ends of scansegs strike an angled side of an obstacle, creating open space.
            // Currently the port only splices horizontally to that side, not vertically; see if 
            // there is an extra bend.  Create obstacles in this order so we force the lower
            // obstacle's port visibility chain to be created first, as it stops before the intersection.
            // Note: This path now uses direct visibility splice so there is only a single bend.
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(0, 0), new Point(90, 20)),
                    CurveFromPoints(new[] { new Point(70, 30), new Point(30, 70), new Point(110, 70) }),
                    PolylineFromRectanglePoints(new Point(0, 60), new Point(20, 80))
                };
            var router = DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
            Validate.AreEqual(1, CountBends(router.EdgeGeometries.First()), "pathCurve.Segments.Count");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Two dead-ends of scansegs intersect before striking an angled side of an obstacle.")]
        public void DeadEnd_Crossing()
        {
            //  0->1 should be normal.
            //  0->2 needs inspection to see if there are extra bends.
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(70, 30), new Point(30, 70), new Point(110, 70) }),
                    PolylineFromRectanglePoints(new Point(0, 40), new Point(20, 80)),
                    PolylineFromRectanglePoints(new Point(40, 0), new Point(80, 20))
                };
            var router = DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, -1));
            foreach (var edgeGeom in router.EdgeGeometries)
            {
                Validate.AreEqual(2, CountBends(edgeGeom), "pathCurve.Segments.Count");
            }
        }

        private static int CountBends(EdgeGeometry edgeGeom) {
            return CountBends((Curve)edgeGeom.Curve);
        }

        private static int CountBends(Curve pathCurve) {
            var inEllipse = false;
            var bendCount = 0;
            foreach (var segment in pathCurve.Segments) 
            {
                // Bends are formed by one or more successive Ellipses.
                if (segment is Ellipse)
                {
                    if (!inEllipse) 
                    {
                        inEllipse = true;
                        ++bendCount;
                    }
                    continue;
                }
                inEllipse = false;
            }
            return bendCount;
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Two dead-ends of scansegs intersect before striking an angled side of an obstacle, and the visibility chains from the two ports intersect in the space between the scanseg intersection and the obstacle..")]
        public void DeadEnd_Crossing_EdgeChains_Intersect()
        {
            // Two dead-ends of scansegs intersect before striking an angled side of an obstacle,
            // and the visibility chains of two Ports extend from outside them to intersect in the
            // space between the scanseg intersection and the obstacle.
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(0, 10), new Point(20, 90)),
                    PolylineFromRectanglePoints(new Point(30, 0), new Point(110, 20)),
                    CurveFromPoints(new[] { new Point(110, 30), new Point(50, 90), new Point(110, 90) })
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 2));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Ensure that visibility chains along both primary and secondary directions from the unpaddedBorderIntersect are created.")]
        public void Secondary_Port_Visibility_RotatedClockwise()
        {
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(50, 0), new Point(20, 10), new Point(30, 40), new Point(60, 30) }),
                    PolylineFromRectanglePoints(new Point(70, 70), new Point(90, 90))
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Ensure that visibility chains along both primary and secondary directions from the unpaddedBorderIntersect are created.")]
        public void Secondary_Port_Visibility_RotatedCounterclockwise()
        {
            // Ensure that visibility chains along both primary and secondary directions from the 
            // unpaddedBorderIntersect are created.
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[] { new Point(20, 10), new Point(10, 40), new Point(40, 50), new Point(50, 20) }),
                    PolylineFromRectanglePoints(new Point(70, 70), new Point(90, 90))
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        
        private static Shape ReplaceTestSquareWithDiamond(Shape square, List<Point> midRectOffsets)
        {
            // Revise the source obstacle to a diamond shape.
            midRectOffsets.Clear();
            var xOffset = square.BoundingBox.Width / 2;
            var yOffset = square.BoundingBox.Height / 2;
            midRectOffsets.Add(new Point(-xOffset, 0.0)); // left
            midRectOffsets.Add(new Point(0.0, yOffset)); // top
            midRectOffsets.Add(new Point(xOffset, 0.0)); // right
            midRectOffsets.Add(new Point(0.0, -yOffset)); // bottom

            return CurveFromPoints(new[]
                        {
                            square.BoundingBox.Center + midRectOffsets[0], square.BoundingBox.Center + midRectOffsets[1],
                            square.BoundingBox.Center + midRectOffsets[2], square.BoundingBox.Center + midRectOffsets[3]
                        });
        }


        private static PortSpan NormalizeSpan(ICurve curve, PortSpan span)
        {
            // Basically just assume we wrapped by a single side and normalize by modulo to the curve domain.
            if (span.Item1 < curve.ParStart)
            {
                //span.Item1 += curve.ParEnd - curve.ParStart; //ted's strang code
                span = new PortSpan(span.Item1+curve.ParEnd - curve.ParStart, span.Item2);
            }
            if (span.Item2 > curve.ParEnd)
            {
                //span.Item2 -= curve.ParEnd;
                span = new PortSpan(span.Item1, span.Item2-curve.ParEnd);
            }
            return span;
        }



        
        [TestMethod]
        [Timeout(2000)]
        [Description("Route between a non-PortEntry port in the middle of the source diamond side above the corner closest to the non-portEntry target port, which is below that diamond corner, and with a middle obstacle above that diamond corner that blocks the single-bend path.  This is for comparison with PortEntry-path bends.")]
        public void PortEntry_Diamond_AboveCorner_TargetAboveCorner_MiddleObstacle_PortsMoved()
        {
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0];
            var b = obstacles[1];

            // For a comparison with PortEntry_Diamond_MidpointAboveCorner_TargetBelowCorner_MiddleObstacle, comparing
            // the bend-removal (or lack thereof) at the source connection.

            // Put a single Port in the source and target at a height that will be blocked by the middle obstacle.
            var unused = new List<Point>();
            a = obstacles[0] = ReplaceTestSquareWithDiamond(a, unused);
            /*var sourcePort =*/
            MakeSingleRelativeObstaclePort(a, new Point(0, 12));
            var targetPort = MakeSingleRelativeObstaclePort(b, new Point(0, 12));

            // Put an obstacle in the middle to block no-bend routing between the Ports.
            obstacles.Add(PolylineFromRectanglePoints(new Point(100, 70), new Point(120, 90)));

            var routings = new List<EdgeGeometry> { CreateRouting(a.Ports.First(), targetPort) };
            DoRouting(obstacles, routings, null /*freePorts*/);
        }



        
        [TestMethod]
        [Timeout(2000)]
        [Description("Verify there are no extra bends after nudging.")]
        public void Nudger_NoExtraBends_With_Rectangles()
        {
            // Note: The attachment to the upper-right obstacle is off its port due to the nudger using the 
            // whole obstacle side to optimize the two path separation.  A PortEntry could be used to control that if desired.
            var obstacles = new List<Shape>
                {
                    PolylineFromRectanglePoints(new Point(20, 20), new Point(100, 100)),
                    // left
                    PolylineFromRectanglePoints(new Point(230, 30), new Point(250, 50)),
                    // right
                    PolylineFromRectanglePoints(new Point(200, 70), new Point(220, 90)) // mid
                };
            var port0 = MakeSingleRelativeObstaclePort(obstacles[0], new Point());

            // Force the port on the right obstacle to be on its left side only, by placing the port offset onto
            // the border (this could be done with a PortEntry too).
            Rectangle box1 = obstacles[1].BoundingBox;
            var port1 = MakeSingleRelativeObstaclePort(obstacles[1], new Point(box1.Left, box1.Center.Y) - box1.Center);

            // For the middle obstacle, force the port to be only on the bottom.
            Rectangle box2 = obstacles[2].BoundingBox;
            var port2 = MakeSingleRelativeObstaclePort(obstacles[2], new Point(box2.Center.X, box2.Bottom) - box2.Center);

            var routings = new List<EdgeGeometry> { CreateRouting(port0, port1), CreateRouting(port2, port1) };
            var router = DoRouting(obstacles, routings, null /*freePorts*/);
            foreach (var pair in router.EdgeGeometries.Select((eg, index) => new Tuple<EdgeGeometry, int>(eg, index)))
            {
                var expectedBends = (pair.Item2 == 0) ? 2 : 1;
                Validate.AreEqual(expectedBends, CountBends(pair.Item1), "pathCurve.Segments.Count");
            }
        }

        
        [TestMethod]
        [Timeout(2000)]
        [Description("Route between two obstacles blocked by a group, and between each of those two obstacles and an obstacle inside the group.")]
        public void GroupTest_Simple()
        {
            GroupTest_Simple_Worker(true /*wantGroup*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Route between two obstacles blocked by a non-group obstacle (for path comparison with the group case).")]
        public void GroupTest_Simple_NoGroup()
        {
            GroupTest_Simple_Worker(false /*wantGroup*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple test illustrating how LimitRect can introduce extra bends.")]
        public void GroupTest_Simple_NoGroup_PortSplice_LimitRect()
        {
            base.LimitPortVisibilitySpliceToEndpointBoundingBox = true;
            GroupTest_Simple_Worker(false /*wantGroup*/);
            base.LimitPortVisibilitySpliceToEndpointBoundingBox = false;
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Similar to TestForGdi GroupTest0; route multiple objects across nested groups.")]
        public void GroupTest0()
        {
            GroupTest_Worker(false /*wantFourthGroup*/, false /*outsideGroup1*/, false /*offCenter*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Similar to GroupTest0 but make group1 too small to contain the second shape.")]
        public void GroupTest0_OutsideGroup1()
        {
            GroupTest_Worker(false /*wantFourthGroup*/, true /*outsideGroup1*/, false /*offCenter*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Same as GroupTest0 but adjust obstacle sizes so the ports are not collinear.")]
        public void GroupTest0_OffCenter()
        {
            GroupTest_Worker(false /*wantFourthGroup*/, false /*outsideGroup1*/, true /*offCenter*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Same as GroupTest0_OutsideGroup1 but adjust obstacle sizes so the ports are not collinear.")]
        public void GroupTest0_OutsideGroup1_OffCenter()
        {
            GroupTest_Worker(false /*wantFourthGroup*/, true /*outsideGroup1*/, true /*offCenter*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Similar to TestForGdi GroupTest; similar to GroupTest0 but add an additional group and obstacle.")]
        public void GroupTest()
        {
            GroupTest_Worker(true /*wantFourthGroup*/, false /*outsideGroup1*/, false /*offCenter*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Similar to GroupTest but make group1 too small to contain the second shape.")]
        public void GroupTest_OutsideGroup1()
        {
            GroupTest_Worker(true /*wantFourthGroup*/, true /*outsideGroup1*/, false /*offCenter*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Same as GroupTest but adjust obstacle sizes so the ports are not collinear.")]
        public void GroupTest_OffCenter()
        {
            GroupTest_Worker(true /*wantFourthGroup*/, false /*outsideGroup1*/, true /*offCenter*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Same as GroupTest_OutsideGroup1 but adjust obstacle sizes so the ports are not collinear.")]
        public void GroupTest_OutsideGroup1_OffCenter()
        {
            GroupTest_Worker(true /*wantFourthGroup*/, true /*outsideGroup1*/, true /*offCenter*/);
        }

        // Return whether the desired routing obstacles are restricted and if so, whether they include
        // the passed pair.  Passing (-1, -1) returns false if there is any restriction at all.
        private bool WantRoutingObstaclePair(int source, int target)
        {
            if ((0 != this.SourceOrdinals.Count) && (!this.SourceOrdinals.Contains(source)))
            {
                return false;
            }
            if ((0 != this.TargetOrdinals.Count) && (!this.TargetOrdinals.Contains(target)))
            {
                return false;
            }
            return true;
        }

        private void GroupTest_Worker(bool wantFourthGroup, bool outsideGroup1, bool offCenter)
        {
            var obstacles = new List<Shape>();

            // Add initial singles first.  Make s2, s3, and s4 slightly off-aligned so the centers don't match
            // so we can get better testing of port visibility extension.
            Shape s1, s2, s3, s4, s5;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(-45, -5), new Point(-35, 5)));
            s1.UserData = "s1";
            obstacles.Add(s2 = PolylineFromRectanglePoints(new Point(-5, offCenter ? -2 : -5), new Point(5, 5)));
            s2.UserData = "s2";
            obstacles.Add(s3 = PolylineFromRectanglePoints(new Point(85, -5), new Point(95, offCenter ? 2 : 5)));
            s3.UserData = "s3";
            obstacles.Add(s4 = PolylineFromRectanglePoints(new Point(145, offCenter ? -2 : -5), new Point(155, 5)));
            s4.UserData = "s4";
            obstacles.Add(s5 = PolylineFromRectanglePoints(new Point(245, -5), new Point(255, 5)));
            s5.UserData = "s5";

            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(s2, new Point());
            var ps3 = MakeSingleRelativeObstaclePort(s3, new Point());
            var ps4 = MakeSingleRelativeObstaclePort(s4, new Point());
            var ps5 = MakeSingleRelativeObstaclePort(s5, new Point());

            var routings = new List<EdgeGeometry>();
            if (WantRoutingObstaclePair(1, 5))
            {
                routings.Add(CreateRouting(ps1, ps5));
            }
            if (WantRoutingObstaclePair(2, 3))
            {
                routings.Add(CreateRouting(ps2, ps3));
            }
            if (WantRoutingObstaclePair(2, 4))
            {
                routings.Add(CreateRouting(ps2, ps4));
            }
            if (WantRoutingObstaclePair(2, 5))
            {
                routings.Add(CreateRouting(ps2, ps5));
            }
            if (WantRoutingObstaclePair(4, 5))
            {
                routings.Add(CreateRouting(ps4, ps5));
            }

            // Groups.  If outsideGroup1, make the group too small to include s3.
            Shape g1, g2, g3;
            obstacles.Add(g1 = PolylineFromRectanglePoints(new Point(-25, -15), new Point(outsideGroup1 ? 80 : 105, 15)));
            g1.UserData = "g1";
            obstacles.Add(g2 = PolylineFromRectanglePoints(new Point(-50, -30), new Point(120, 30)));
            g2.UserData = "g2";
            obstacles.Add(g3 = PolylineFromRectanglePoints(new Point(145, -15), new Point(295, 15)));
            g3.UserData = "g3";

            g1.AddChild(s2);
            g1.AddChild(s3);
            g2.AddChild(s1);
            g2.AddChild(g1);
            g3.AddChild(s4);
            g3.AddChild(s5);

            var pg2 = MakeSingleRelativeObstaclePort(g2, new Point());
            if (WantRoutingObstaclePair(-1, -1))
            {
                routings.Add(CreateRouting(pg2, ps5));
            }

            if (wantFourthGroup)
            {
                Shape s6, g4;
                obstacles.Add(s6 = PolylineFromRectanglePoints(new Point(-3, 19), new Point(4, 25)));
                s6.UserData = "s6";
                var ps6 = MakeSingleRelativeObstaclePort(s6, new Point());

                obstacles.Add(g4 = PolylineFromRectanglePoints(new Point(-12, -40), new Point(12, 40)));
                g4.UserData = "g4";

                g4.AddChild(s6);
                g4.AddChild(s2);
                g2.AddChild(s6);

                if (WantRoutingObstaclePair(1, 4))
                {
                    routings.Add(CreateRouting(ps1, ps4));
                }
                if (WantRoutingObstaclePair(1, 6))
                {
                    routings.Add(CreateRouting(ps1, ps6));
                }
                if (WantRoutingObstaclePair(5, 6))
                {
                    routings.Add(CreateRouting(ps6, ps5));
                }
            }

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify no group boundary crossing when routing between two obstacles of a group partially blocked by an obstacle that crosses the group border and forces a longer interior path to be taken.")]
        public void Group_Obstacle_Crossing_Boundary_Between_Routed_Obstacles_Gap2()
        {
            Group_Obstacle_Crossing_Boundary_Worker(/*gap:*/ 2, /*blocking:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify no group boundary crossing when routing between two obstacles of a group partially blocked by an obstacle that crosses the group border and forces a longer interior path to be taken.")]
        public void Group_Obstacle_Crossing_Boundary_Between_Routed_Obstacles_Gap4()
        {
            Group_Obstacle_Crossing_Boundary_Worker(/*gap:*/ 4, /*blocking:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify no group boundary crossing when routing between two obstacles of a group partially blocked by an obstacle that shares a border with the group.")]
        public void Group_Obstacle_Crossing_Boundary_On_Routed_Obstacles()
        {
            Group_Obstacle_Crossing_Boundary_Worker(/*gap:*/ 0, /*blocking:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify no group boundary crossing when routing between two obstacles of a group partially blocked by an obstacle that is just inside the group border.")]
        public void Group_Obstacle_Crossing_Boundary_Inside_Routed_Obstacles_Gap2()
        {
            Group_Obstacle_Crossing_Boundary_Worker(/*gap:*/ -2, /*blocking:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify no group boundary crossing when routing between two obstacles of a group partially blocked by an obstacle that is just inside the group border.")]
        public void Group_Obstacle_Crossing_Boundary_Inside_Routed_Obstacles_Gap4()
        {
            Group_Obstacle_Crossing_Boundary_Worker(/*gap:*/ -4, /*blocking:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify we can cross the group boundary when routing between two obstacles of a group totally blocked within the group by an obstacle that crosses both group borders.")]
        public void Group_Obstacle_Crossing_Boundary_Fully_Blocking_Routed_Obstacles()
        {
            Group_Obstacle_Crossing_Boundary_Worker(/*gap:*/ 10, /*blocking:*/ true);
        }

        private void Group_Obstacle_Crossing_Boundary_Worker(int gap, bool blocking)
        {
            var obstacles = new List<Shape>();
            Shape s1, s2, s3;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(45, 20), new Point(55, 30)));
            s1.UserData = "s1";
            obstacles.Add(s2 = PolylineFromRectanglePoints(new Point(45, 60), new Point(55, 70)));
            s2.UserData = "s2";

            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(s2, new Point());

            obstacles.Add(s3 = PolylineFromRectanglePoints(new Point(blocking ? -40 : -20, 40), new Point(70 + gap, 50)));
            s3.UserData = "s3";

            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2) };

            Shape g1;
            obstacles.Add(g1 = PolylineFromRectanglePoints(new Point(-30, 10), new Point(70, 80)));
            g1.UserData = "g1";
            g1.AddChild(s1);
            g1.AddChild(s2);
            g1.AddChild(s3);

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify routing across and around adjacent groups sharing a padded border.")]
        public void Group_AdjacentOuterEdge_Outside()
        {
            Group_AdjacentOuterEdge_Worker(false /*nested*/, 0);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify routing across and around nested groups sharing a padded border.")]
        public void Group_AdjacentOuterEdge_Nested()
        {
            Group_AdjacentOuterEdge_Worker(true /*nested*/, 0);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify routing across and around adjacent groups with a gap between their borders.")]
        public void Group_AdjacentOuterEdge_Gap()
        {
            Group_AdjacentOuterEdge_Worker(false /*nested*/, 5);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify routing across and around adjacent groups whose borders straddle each other.")]
        public void Group_AdjacentOuterEdge_Straddle()
        {
            Group_AdjacentOuterEdge_Worker(false /*nested*/, -5);
        }

        private void Group_AdjacentOuterEdge_Worker(bool nested, int gap)
        {
            var obstacles = new List<Shape>();

            // Add initial singles first.  Make them slightly off-aligned so the centers don't match
            // so we can get better testing of port visibility extension.
            Shape s1, s2, s3, s4;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(20, 40), new Point(30, 50)));
            s1.UserData = "s1";
            obstacles.Add(s2 = PolylineFromRectanglePoints(new Point(70, 30), new Point(80, 50)));
            s2.UserData = "s2";
            obstacles.Add(s3 = PolylineFromRectanglePoints(new Point(120, 40), new Point(130, 50)));
            s3.UserData = "s3";
            obstacles.Add(s4 = PolylineFromRectanglePoints(new Point(120, 60), new Point(130, 70)));
            s4.UserData = "s4";

            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(s2, new Point());
            var ps3 = MakeSingleRelativeObstaclePort(s3, new Point());
            var ps4 = MakeSingleRelativeObstaclePort(s4, new Point());
            var routings = new List<EdgeGeometry>
                {
                    CreateRouting(ps1, ps2), CreateRouting(ps1, ps4), CreateRouting(ps2, ps3) 
                };

            Shape g1, g2;
            // If the groups are not to be nested, then padding is 1.0 so use 2.0 of space between the groups.
            obstacles.Add(
                g1 = PolylineFromRectanglePoints(new Point(10, 10), new Point(nested ? 100 : 48 - gap, 80)));
            obstacles.Add(g2 = PolylineFromRectanglePoints(new Point(50, 20), new Point(100, 70)));
            g1.AddChild(s1);
            g2.AddChild(s2);
            if (nested)
            {
                g1.AddChild(g2);
            }
            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify routing through non-overlapping groups that are spatial but not hierarchical parents.")]
        public void Group_Spatial_Parent()
        {
            Group_Spatial_Parent_Worker(false /*overlapping*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify routing through overlapping groups that are spatial but not hierarchical parents.")]
        public void Group_Spatial_Parent_GroupOverlap()
        {
            Group_Spatial_Parent_Worker(true /*overlapping*/);
        }

        private void Group_Spatial_Parent_Worker(bool overlapping)
        {
            var obstacles = new List<Shape>();

            // Add initial singles first.  Dummies are so we can make sure the spatial parents are treated as groups.
            Shape shape1, shape2, shape1Dummy, shape2Dummy, shape3Dummy;
            obstacles.Add(shape1 = PolylineFromRectanglePoints(new Point(40, 40), new Point(50, 50)));
            shape1.UserData = "s1";
            obstacles.Add(shape2 = PolylineFromRectanglePoints(new Point(100, 50), new Point(110, 60)));
            shape2.UserData = "s2";
            obstacles.Add(shape1Dummy = PolylineFromRectanglePoints(new Point(34, 34), new Point(36, 36)));
            shape1Dummy.UserData = "s1dummy";
            obstacles.Add(shape2Dummy = PolylineFromRectanglePoints(new Point(94, 44), new Point(96, 46)));
            shape2Dummy.UserData = "s2dummy";
            obstacles.Add(shape3Dummy = PolylineFromRectanglePoints(new Point(100, 20), new Point(105, 25)));
            shape3Dummy.UserData = "s3dummy";

            var ps1 = MakeSingleRelativeObstaclePort(shape1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(shape2, new Point());
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2) };

            // g0 is the big outer non-parent; other groups are not parents unless they have "p" in the name,
            // and the number refers to which obstacle they contain.  g3dummy should not be in the spatial hierarchy.
            Shape group0, group1A, group1Parent, group1B, group1C, group2A, group2Parent, group3Dummy;
            obstacles.Add(group0 = PolylineFromRectanglePoints(new Point(10, 10), new Point(140, 80)));
            group0.UserData = "g0";
            obstacles.Add(group1A = PolylineFromRectanglePoints(new Point(15, 15), new Point(75, 75)));
            group1A.UserData = "g1a";
            obstacles.Add(group1Parent = PolylineFromRectanglePoints(new Point(20, 20), new Point(70, 70)));
            group1Parent.UserData = "g1p";
            obstacles.Add(group1B = PolylineFromRectanglePoints(new Point(overlapping ? -25 : 25, 25), new Point(65, 65)));
            group1B.UserData = "g1b";
            obstacles.Add(group1C = PolylineFromRectanglePoints(new Point(30, 30), new Point(60, 60)));
            group1C.UserData = "g1c";
            obstacles.Add(group2A = PolylineFromRectanglePoints(new Point(85, 35), new Point(125, 75)));
            group2A.UserData = "g2a";
            obstacles.Add(group2Parent = PolylineFromRectanglePoints(new Point(90, 40), new Point(overlapping ? 170 : 120, 70)));
            group2Parent.UserData = "g2p";
            obstacles.Add(group3Dummy = PolylineFromRectanglePoints(new Point(85, 15), new Point(125, 30)));
            group3Dummy.UserData = "g3dummy";
            group1Parent.AddChild(shape1);
            group2Parent.AddChild(shape2);
            group3Dummy.AddChild(shape3Dummy);

            // Add the dummmies so we'll recognize the spatial parents as groups.
            group1A.AddChild(shape1Dummy);
            group1B.AddChild(shape1Dummy);
            group1C.AddChild(shape1Dummy);
            group2A.AddChild(shape2Dummy);
            group0.AddChild(shape1Dummy);
            group0.AddChild(shape2Dummy);

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify routing between collinear ports through groups that are spatial but not hierarchical parents.")]
        public void Group_Spatial_Parent_GroupOverlap_Collinear()
        {
            // Caused an AF inserting into the middle of the VisibilityVertex chain of a scansegment during SegmentIntersector.
            var obstacles = new List<Shape>();

            // Add initial singles first.  Dummies are so we can make sure the spatial parents are treated as groups.
            Shape shape1, shape2, shape3Dummy;
            obstacles.Add(shape1 = PolylineFromRectanglePoints(new Point(40, 40), new Point(50, 50)));
            shape1.UserData = "s1";
            obstacles.Add(shape2 = PolylineFromRectanglePoints(new Point(100, 40), new Point(110, 50)));
            shape2.UserData = "s2";
            obstacles.Add(shape3Dummy = PolylineFromRectanglePoints(new Point(100, 72), new Point(110, 75)));
            shape3Dummy.UserData = "s3dummy";

            var ps1 = MakeSingleRelativeObstaclePort(shape1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(shape2, new Point());
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2) };

            // g0 is the big outer non-parent; other groups are not parents unless they have "p" in the name,
            // and the number refers to which obstacle they contain.
            // Add the dummy child to all groups to make sure they're all recognized as groups.  The child is
            // out of range just because it was easier to tack it onto the test that way; we don't route to/from it.
            Shape group0, group1A, group1Parent, group1B, group1C, group2A, group2Parent;
            obstacles.Add(group0 = PolylineFromRectanglePoints(new Point(10, 10), new Point(140, 80)));
            group0.UserData = "g0";
            group0.AddChild(shape3Dummy);
            obstacles.Add(group1A = PolylineFromRectanglePoints(new Point(15, 15), new Point(75, 75)));
            group1A.UserData = "g1a";
            group1A.AddChild(shape3Dummy);
            obstacles.Add(group1Parent = PolylineFromRectanglePoints(new Point(20, 20), new Point(70, 70)));
            group1Parent.UserData = "g1p";
            group1Parent.AddChild(shape3Dummy);
            obstacles.Add(group1B = PolylineFromRectanglePoints(new Point(25, 25), new Point(65, 65)));
            group1B.UserData = "g1b";
            group1B.AddChild(shape3Dummy);
            obstacles.Add(group1C = PolylineFromRectanglePoints(new Point(30, 30), new Point(60, 60)));
            group1C.UserData = "g1c";
            group1C.AddChild(shape3Dummy);
            obstacles.Add(group2A = PolylineFromRectanglePoints(new Point(85, 25), new Point(125, 65)));
            group2A.UserData = "g2a";
            group2A.AddChild(shape3Dummy);
            obstacles.Add(group2Parent = PolylineFromRectanglePoints(new Point(90, 30), new Point(170, 60)));
            group2Parent.UserData = "g2p";
            group2Parent.AddChild(shape3Dummy);
            group1Parent.AddChild(shape1);
            group2Parent.AddChild(shape2);

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify routing through landlocking groups.")]
        public void Group_Landlock()
        {
            // Groups that are not in an obstacle hierarchy may landlock it.
            var obstacles = new List<Shape>();

            // Add initial singles first.  Dummies are so we can make sure the spatial parents are treated as groups.
            Shape shape1, shape2, shapeAdummy, shapeBdummy, shapeCdummy, shapeDdummy;
            obstacles.Add(shape1 = PolylineFromRectanglePoints(new Point(50, 40), new Point(60, 50)));
            shape1.UserData = "s1";
            obstacles.Add(shape2 = PolylineFromRectanglePoints(new Point(140, 40), new Point(150, 50)));
            shape2.UserData = "s2";
            obstacles.Add(shapeAdummy = PolylineFromRectanglePoints(new Point(13, 50), new Point(17, 54)));
            shapeAdummy.UserData = "sadummy";
            obstacles.Add(shapeBdummy = PolylineFromRectanglePoints(new Point(53, 83), new Point(57, 87)));
            shapeBdummy.UserData = "sbdummy";
            obstacles.Add(shapeCdummy = PolylineFromRectanglePoints(new Point(93, 50), new Point(97, 54)));
            shapeCdummy.UserData = "scdummy";
            obstacles.Add(shapeDdummy = PolylineFromRectanglePoints(new Point(53, 13), new Point(57, 17)));
            shapeDdummy.UserData = "sddummy";

            var ps1 = MakeSingleRelativeObstaclePort(shape1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(shape2, new Point());
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2) };

            // g1p and g2p are the parents of s1 and s2.  g(a-d) are the landlocking groups.
            Shape group1Parent, group2Parent, groupA, groupB, groupC, groupD;
            obstacles.Add(group1Parent = PolylineFromRectanglePoints(new Point(40, 30), new Point(70, 60)));
            group1Parent.UserData = "g1p";
            obstacles.Add(group2Parent = PolylineFromRectanglePoints(new Point(130, 30), new Point(160, 60)));
            group2Parent.UserData = "g2p";
            obstacles.Add(groupA = PolylineFromRectanglePoints(new Point(10, 10), new Point(20, 90)));
            groupA.UserData = "ga";
            obstacles.Add(groupB = PolylineFromRectanglePoints(new Point(10, 80), new Point(100, 90)));
            groupB.UserData = "gb";
            obstacles.Add(groupC = PolylineFromRectanglePoints(new Point(90, 10), new Point(100, 90)));
            groupC.UserData = "gc";
            obstacles.Add(groupD = PolylineFromRectanglePoints(new Point(10, 10), new Point(100, 20)));
            groupD.UserData = "gd";
            group1Parent.AddChild(shape1);
            group2Parent.AddChild(shape2);

            groupA.AddChild(shapeAdummy);
            groupB.AddChild(shapeBdummy);
            groupC.AddChild(shapeCdummy);
            groupD.AddChild(shapeDdummy);

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Route outside multiple nested groups with two intervening triangles crossing the group boundaries.")]
        public void Group_Obstacle_Overlap_Triangle()
        {
            Group_Obstacle_Overlap_Worker(true /*triangle*/, false /*inverted*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Route outside multiple nested groups with two intervening inverted triangles crossing the group boundaries.")]
        public void Group_Obstacle_Overlap_Triangle_Inverted()
        {
            Group_Obstacle_Overlap_Worker(true /*triangle*/, true /*inverted*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Route outside multiple nested groups with an intervening rectangle crossing the group boundaries.")]
        public void Group_Obstacle_Overlap_Rectangle()
        {
            Group_Obstacle_Overlap_Worker(false /*triangle*/, false /*inverted*/);
        }

        private void Group_Obstacle_Overlap_Worker(bool triangle, bool inverted)
        {
            // Groups that are not in an obstacle hierarchy may landlock it.
            var obstacles = new List<Shape>();

            // Add initial singles first.
            Shape shape1, shape2;
            obstacles.Add(shape1 = PolylineFromRectanglePoints(new Point(50, 40), new Point(60, 60)));
            shape1.UserData = "s1";
            obstacles.Add(shape2 = PolylineFromRectanglePoints(new Point(140, 40), new Point(150, 60)));
            shape2.UserData = "s2";

            // The obstacles overlapping the group boundary are not named in this test.
            if (triangle)
            {
                Shape triangle1 =
                    CurveFromPoints(new[] { new Point(70, 40), new Point(90, 60), new Point(90, 40) });
                Shape triangle2 =
                    CurveFromPoints(new[] { new Point(70, 44), new Point(70, 50.5), new Point(76.5, 50.5) });
                if (inverted)
                {
                    triangle1.BoundaryCurve = CurveFactory.RotateCurveAroundCenterByDegree(
                        triangle1.BoundaryCurve, triangle1.BoundingBox.Center, 180);
                    triangle2.BoundaryCurve = CurveFactory.RotateCurveAroundCenterByDegree(
                        triangle2.BoundaryCurve, triangle2.BoundingBox.Center, 180);
                }
                obstacles.Add(triangle1);
                obstacles.Add(triangle2);
            }
            else
            {
                obstacles.Add(PolylineFromRectanglePoints(new Point(70, 40), new Point(90, 60)));
            }

            var ps1 = MakeSingleRelativeObstaclePort(shape1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(shape2, new Point());
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2) };

            // g1p* are parents of s1.
            Shape group1Parent1, group1Parent2, group1Parent3, group1Parent4;
            obstacles.Add(group1Parent1 = PolylineFromRectanglePoints(new Point(40, 30), new Point(72, 70)));
            group1Parent1.UserData = "g1p1";
            group1Parent1.AddChild(shape1);
            obstacles.Add(group1Parent2 = PolylineFromRectanglePoints(new Point(40, 30), new Point(75, 70)));
            group1Parent2.UserData = "g1p2";
            group1Parent2.AddChild(group1Parent1);
            obstacles.Add(group1Parent3 = PolylineFromRectanglePoints(new Point(40, 30), new Point(78, 70)));
            group1Parent2.UserData = "g1p3";
            group1Parent3.AddChild(group1Parent2);
            obstacles.Add(group1Parent4 = PolylineFromRectanglePoints(new Point(40, 30), new Point(81, 70)));
            group1Parent4.UserData = "g1p4";
            group1Parent4.AddChild(group1Parent3);
            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Route from an obstacle inside a group to a FreePort outside the group.")]
        public void Group_FreePort_Outside_Group()
        {
            Group_FreePort_Worker(/*inside:*/ false, /*nested:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Route from an obstacle inside a group to a FreePort inside the group.")]
        public void Group_FreePort_Inside_Group()
        {
            Group_FreePort_Worker(/*inside:*/ true, /*nested:*/ false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Route from an obstacle inside a nested group to a FreePort inside the group.")]
        public void Group_FreePort_Inside_Group_Nested()
        {
            Group_FreePort_Worker(/*inside:*/ true, /*nested:*/ true);
        }

        private void Group_FreePort_Worker(bool inside, bool nested)
        {
            var obstacles = new List<Shape>();

            // Add initial single first.
            Shape s1;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(30, 30), new Point(40, 40)));
            s1.UserData = "s1";

            // The first group's size depends on whether the FreePort's to be inside or out.
            Shape g1;
            obstacles.Add(g1 = PolylineFromRectanglePoints(new Point(20, 20), new Point(inside ? 70 : 50, 50)));
            g1.AddChild(s1);
            if (nested)
            {
                // inside is always true if nested.
                Shape group1Nested;
                obstacles.Add(group1Nested = PolylineFromRectanglePoints(new Point(10, 10), new Point(80, 60)));
                group1Nested.AddChild(g1);
            }

            // The routing is always between the obstacle and the FreePort.
            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            var fp1 = MakeAbsoluteFreePort(new Point(60, 35));
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, fp1) };

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        
        private class PointAndShape
        {
            internal readonly Point Intersect;
            internal readonly Obstacle Group;

            internal PointAndShape(Point point, Func<Point, Point, Shape> createPolylineFunc)
            {
                Intersect = point;
                var shape = createPolylineFunc(Intersect - new Point(0.1, 0.1), Intersect + new Point(0.1, 0.1));
                Group = new Obstacle(shape, makeRect:false, padding:1.0);
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify creating, adding to, and removing from GroupBoundaryCrossingsMap; no routing is done.")]
        public void GroupBoundaryCrossings_Test()
        {
            var pointsAndShapes = new PointAndShape[30];
            for (var ii = 0; ii < pointsAndShapes.Length; ++ii)
            {
                pointsAndShapes[ii] = new PointAndShape(new Point(ii, 0), PolylineFromRectanglePoints);
            }

            // Create the "crossings", loading in reverse of sorted order.
            var map = new GroupBoundaryCrossingMap();
            for (var ii = 30; ii > 0; --ii)
            {
                var pas = pointsAndShapes[ii - 1];
                map.AddIntersection(pas.Intersect, pas.Group, Direction.East);
            }

            var crossings = map.GetOrderedListBetween(pointsAndShapes[10].Intersect, pointsAndShapes[20].Intersect);
            VerifySortedCrossingList(crossings, 11, pointsAndShapes[10].Intersect, pointsAndShapes[20].Intersect);

            // Verify initial trim with no change.
            crossings.Trim(pointsAndShapes[10].Intersect, pointsAndShapes[20].Intersect);
            VerifySortedCrossingList(crossings, 11, pointsAndShapes[10].Intersect, pointsAndShapes[20].Intersect);

            // Prepend some items.
            for (var ii = 0; ii < 10; ++ii)
            {
                var newCrossings = map.GetOrderedListBetween(
                    pointsAndShapes[0].Intersect, pointsAndShapes[ii].Intersect);
                VerifySortedCrossingList(
                    newCrossings, ii + 1, pointsAndShapes[0].Intersect, pointsAndShapes[ii].Intersect);

                crossings.MergeFrom(newCrossings);
                VerifySortedCrossingList(
                    crossings, 11 + ii + 1, pointsAndShapes[0].Intersect, pointsAndShapes[20].Intersect);

                // Trim back to original.
                crossings.Trim(pointsAndShapes[10].Intersect, pointsAndShapes[20].Intersect);
                VerifySortedCrossingList(crossings, 11, pointsAndShapes[10].Intersect, pointsAndShapes[20].Intersect);
            }

            // Append some items.
            for (var ii = 0; (ii + 21) < 30; ++ii)
            {
                var newCrossings = map.GetOrderedListBetween(
                    pointsAndShapes[21].Intersect, pointsAndShapes[ii + 21].Intersect);
                VerifySortedCrossingList(
                    newCrossings, ii + 1, pointsAndShapes[21].Intersect, pointsAndShapes[ii + 21].Intersect);

                crossings.MergeFrom(newCrossings);
                VerifySortedCrossingList(
                    crossings, 11 + ii + 1, pointsAndShapes[10].Intersect, pointsAndShapes[ii + 21].Intersect);

                // Trim back to original.
                crossings.Trim(pointsAndShapes[10].Intersect, pointsAndShapes[20].Intersect);
                VerifySortedCrossingList(crossings, 11, pointsAndShapes[10].Intersect, pointsAndShapes[20].Intersect);
            }

            // Create a list with a gap in the middle.
            crossings = map.GetOrderedListBetween(pointsAndShapes[5].Intersect, pointsAndShapes[10].Intersect);
            crossings.MergeFrom(map.GetOrderedListBetween(pointsAndShapes[20].Intersect, pointsAndShapes[25].Intersect));
            VerifySortedCrossingList(crossings, 12, pointsAndShapes[5].Intersect, pointsAndShapes[25].Intersect);

            // Merge into that gap.
            crossings.MergeFrom(map.GetOrderedListBetween(pointsAndShapes[14].Intersect, pointsAndShapes[16].Intersect));
            VerifySortedCrossingList(crossings, 15, pointsAndShapes[5].Intersect, pointsAndShapes[25].Intersect);

            // Now merge them all, which will also verify the no-duplicates requirement.
            crossings.MergeFrom(map.GetOrderedListBetween(pointsAndShapes[0].Intersect, pointsAndShapes[29].Intersect));
            VerifySortedCrossingList(crossings, 30, pointsAndShapes[0].Intersect, pointsAndShapes[29].Intersect);

            // Test alternation.
            crossings = map.GetOrderedListBetween(pointsAndShapes[0].Intersect, pointsAndShapes[0].Intersect);
            var oddCrossings = map.GetOrderedListBetween(pointsAndShapes[1].Intersect, pointsAndShapes[1].Intersect);
            for (int ii = 2; ii < 30; ++ii)
            {
                var pas = pointsAndShapes[ii];
                ((0 == (ii & 0x1)) ? crossings : oddCrossings).MergeFrom(
                    map.GetOrderedListBetween(pas.Intersect, pas.Intersect));
            }
            crossings.MergeFrom(map.GetOrderedListBetween(pointsAndShapes[0].Intersect, pointsAndShapes[29].Intersect));
            VerifySortedCrossingList(crossings, 30, pointsAndShapes[0].Intersect, pointsAndShapes[29].Intersect);
        }

        private static void VerifySortedCrossingList(
            PointAndCrossingsList crossings, int count, Point first, Point last)
        {
            Validate.AreEqual(count, crossings.ListOfPointsAndCrossings.Count(), "count failed");
            Validate.AreEqual(first, crossings.ListOfPointsAndCrossings.First().Location, "first range failed");
            Validate.AreEqual(last, crossings.ListOfPointsAndCrossings.Last().Location, "last range failed");
            for (var ii = 1; ii < crossings.ListOfPointsAndCrossings.Count; ++ii)
            {
                var prev = crossings.ListOfPointsAndCrossings[ii - 1].Location.X;
                var current = crossings.ListOfPointsAndCrossings[ii].Location.X;
                Validate.IsTrue(prev < current, "list order is wrong");
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify that reflections do not create spurious crossings across a nonrectilinear group boundary.")]
        public void Group_NonRect_BlockedReflections()
        {
            // Reflections should be blocked because they would cross an obstacle boundary.
            var obstacles = new List<Shape>();

            // Add routing singles first.
            Shape s1, s2;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(220, 90), new Point(240, 110)));
            s1.UserData = "s1";
            obstacles.Add(s2 = PolylineFromRectanglePoints(new Point(130, 200), new Point(150, 220)));
            s2.UserData = "s2";

            // Add the reflectors.  Make the outside one big enough that we would go through the group on reflections
            // if they were not blocked.  These cause parallel non-reflected scansegments to cross the group border
            // and would generate perpendicular reflections from the obstacle inside the group which would cross the
            // group border. Instead we generate reflections on the group itself.
            Shape r1, r2;
            obstacles.Add(r1 = CurveFromPoints(
                    new[] { new Point(140, 70), new Point(80, 130), new Point(120, 170), new Point(180, 110) }));
            r1.UserData = "inner_r1";
            obstacles.Add(r2 = CurveFromPoints(new[]
                {
                    new Point(190, 130), new Point(150, 170), 
                    new Point(60 + (100 * ScanSegment.ReflectionWeight), 170),
                    new Point(100 + (100 * ScanSegment.ReflectionWeight), 130) 
                }));
            r2.UserData = "outer_r2";

            // Add the group.
            Shape g1;
            obstacles.Add(g1 = CurveFromPoints(
                    new[] { new Point(0, -100), new Point(-100, 0), new Point(100, 200), new Point(200, 100) }));
            g1.AddChild(r1);

            // More reflectors, to test that a newly opened group side handles reflection events against itself.
            var t1 = CurveFromPoints(new[] { new Point(-5, -75), new Point(-65, -15), new Point(-5, 45), new Point(55, -15) });
            obstacles.Add(t1);
            t1.UserData = "inner_t1";
            g1.AddChild(t1);

            var t2 = CurveFromPoints(new[] { new Point(-60, -180), new Point(-145, -95), new Point(-85, -35), new Point(-0, -120) });
            obstacles.Add(t2);
            t2.UserData = "outer_t2";

            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(s2, new Point());
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2) };

            var router = DoRouting(obstacles, routings, null /*freePorts*/);
            var group1 = router.ObsTree.GetAllGroups().First();
            VerifyReflectionSegmentsOutsideGroup(group1, router.GraphGenerator.HorizontalScanSegments.Segments, base.UseSparseVisibilityGraph ? 0 : 7);
            VerifyReflectionSegmentsOutsideGroup(group1, router.GraphGenerator.VerticalScanSegments.Segments, base.UseSparseVisibilityGraph ? 0 : 8);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simple routing around a non-orthogonal group for non-children, and into for children.")]
        public void Simple_NonRectangular_Group()
        {
            // Reflections should be blocked because they would cross an obstacle boundary.
            var obstacles = new List<Shape>();

            // Add routing singles first.
            Shape s1, s2;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(160, 10), new Point(180, 30)));
            s1.UserData = "s1";
            obstacles.Add(s2 = PolylineFromRectanglePoints(new Point(160, 170), new Point(180, 190)));
            s2.UserData = "s2";

            // Add the child.
            Shape c1;
            obstacles.Add(c1 = PolylineFromRectanglePoints(new Point(120, 90), new Point(140, 110)));
            c1.UserData = "c1";

            // Add the group.
            Shape g1;
            obstacles.Add(g1 = CurveFromPoints(new[] { new Point(100, 0), new Point(0, 100), new Point(100, 200), new Point(200, 100) }));
            g1.AddChild(c1);

            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(s2, new Point());
            var pc1 = MakeSingleRelativeObstaclePort(c1, new Point());
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2), this.CreateRouting(ps1, pc1) };

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        private static void VerifyReflectionSegmentsOutsideGroup(Obstacle group, IEnumerable<ScanSegment> scanSegments, int numExpected) 
        {
            var numFound = 0;
            foreach (var seg in scanSegments.Where(seg => seg.IsReflection))
            {
                var startIsInside = Curve.PointRelativeToCurveLocation(seg.Start, group.PaddedPolyline) == PointLocation.Inside;
                var endIsInside = Curve.PointRelativeToCurveLocation(seg.End, group.PaddedPolyline) == PointLocation.Inside;
                Validate.IsFalse(startIsInside || endIsInside, "No reflections should be inside the group for this test");
                ++numFound;
            }
            Validate.AreEqual(numExpected, numFound, "Did not find expected number of reflection ScanSegments");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify that reflections do not create spurious crossings across a flat group top boundary.")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "This is the two-word usage.")]
        public void Group_FlatTop_BlockedReflections()
        {
            var obstacles = new List<Shape>();

            // Add routing singles first.
            Shape s1, s2;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(0, 70), new Point(50, 80)));
            s1.UserData = "s1";
            obstacles.Add(s2 = PolylineFromRectanglePoints(new Point(60, 10), new Point(70, 20)));
            s2.UserData = "s2";

            // Add the reflector and blocker.
            Shape r1, b1;
            obstacles.Add(
                r1 = CurveFromPoints(new[] { new Point(10, 10), new Point(20, 30), new Point(30, 10) }));
            r1.UserData = "r1";
            obstacles.Add(b1 = PolylineFromRectanglePoints(new Point(32, 50), new Point(160, 60)));
            b1.UserData = "b1";

            // Add the group.
            Shape g1;
            obstacles.Add(g1 = PolylineFromRectanglePoints(new Point(0, 0), new Point(50, 40)));
            g1.AddChild(r1);

            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(s2, new Point());
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2) };

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Simplest group test possible; route from a single obstacle outside a group to a single obstacle inside a single group.")]
        public void Group_Simple_One_Obstacle_Inside_One_Group()
        {
            var obstacles = new List<Shape>();

            // Add routing singles first.
            Shape s1, s2;
            obstacles.Add(s1 = PolylineFromRectanglePoints(new Point(40, 40), new Point(50, 50)));
            s1.UserData = "s1";
            obstacles.Add(s2 = PolylineFromRectanglePoints(new Point(80, 40), new Point(90, 50)));
            s2.UserData = "s2";

            // Add the group.
            Shape g1;
            obstacles.Add(g1 = PolylineFromRectanglePoints(new Point(30, 30), new Point(60, 60)));
            g1.AddChild(s1);

            var ps1 = MakeSingleRelativeObstaclePort(s1, new Point());
            var ps2 = MakeSingleRelativeObstaclePort(s2, new Point());
            var routings = new List<EdgeGeometry> { CreateRouting(ps1, ps2) };

            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create a grid with aligned nodes and route.")]
        public void Grid_Neighbors_8_Aligned()
        {
            this.Grid_Neighbors_Worker(8, /*aligned:*/ true);
        }

        [TestMethod]
        [Timeout(6000)]
        [Description("Create a grid with unaligned nodes and route.")]
        public void Grid_Neighbors_8_Unaligned()
        {
            this.Grid_Neighbors_Worker(8, /*aligned:*/ false);
        }

        private void Grid_Neighbors_Worker(int numSquares, bool aligned)
        {
            // For consistent repro.
            var rng = new Random(0x41);
            const int Separation = 200;
            const int HalfSize = 10;
            var obstacleCornerOffset = new Point(HalfSize, HalfSize);
            var obstacles = new List<Shape>();
            var routings = new List<EdgeGeometry>();
            for (int row = 0; row < numSquares; ++row)
            {
                for (int col = 0; col < numSquares; ++col)
                {
                    // Row-major, so col changes fastest.
                    var center = new Point(Separation * col, Separation * row);
                    if (!aligned)
                    {
                        center.X += rng.NextDouble() * (Separation - (HalfSize * 2));
                        center.Y += rng.NextDouble() * (Separation - (HalfSize * 2));
                    }
                    var shape = PolylineFromRectanglePoints(center - obstacleCornerOffset, center + obstacleCornerOffset);
                    obstacles.Add(shape);

                    if (col > 0)
                    {
                        var prevShapeInRow = obstacles[obstacles.Count - 2];
                        routings.Add(CreateRouting(GetRelativePort(shape), GetRelativePort(prevShapeInRow)));
                    }
                    if (row > 0)
                    {
                        var shapeInRowBelow = obstacles[((row - 1) * numSquares) + col];
                        routings.Add(CreateRouting(GetRelativePort(shape), GetRelativePort(shapeInRowBelow)));
                    }
                }
            }
            DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles separately landlocked by overlapping rectangles.")]
        public void Route_Between_Two_Separately_Disconnected_Obstacles()
        {
            const int initialCenter = 120;
            const int shiftX = 300;
            const int shiftY = 100;
            var obstacles = new List<Shape> 
            {
                // The first two obstacles to route between.
                this.CreateSquare(new Point(initialCenter, initialCenter), 20),
                this.CreateSquare(new Point(initialCenter + shiftX, initialCenter + shiftY), 20)
            };

            AddAllShiftedRectangles(obstacles, shiftX, shiftY);
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }
        
        private static void AddAllShiftedRectangles(List<Shape> obstacles, int shiftX, int shiftY)
        {
            AddShiftedAngledRectangles(new[] { new Point(100, 10), new Point(80, 30), new Point(210, 160), new Point(230, 140) },
                    shiftX, shiftY, obstacles);
            AddShiftedAngledRectangles(new[] { new Point(30, 80), new Point(10, 100), new Point(140, 230), new Point(160, 210) },
                    shiftX, shiftY, obstacles);
            AddShiftedAngledRectangles(new[] { new Point(140, 10), new Point(10, 140), new Point(30, 160), new Point(160, 30) },
                    shiftX, shiftY, obstacles);
            AddShiftedAngledRectangles(new[] { new Point(210, 80), new Point(80, 210), new Point(100, 230), new Point(230, 100) },
                    shiftX, shiftY, obstacles);
        }

        private static void AddShiftedAngledRectangles(Point[] points, int shiftX, int shiftY, List<Shape> obstacles)
        {
            Validate.IsTrue(points.Length == 4, "Point count must be 4");
            obstacles.Add(CurveFromPoints(points));
            var shiftedPoints = points.Select(p => new Point(p.X + shiftX, p.Y + shiftY)).ToArray();
            obstacles.Add(CurveFromPoints(shiftedPoints));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles separately landlocked by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_Between_Two_NonOrthogonally_Disconnected_Obstacles_4Reflections() 
        {
            NonOrthogonally_Disconnected_Worker(4, 4, true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles separately almost-landlocked by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_Between_Two_NonOrthogonally_AlmostDisconnected_Obstacles_4Reflections()
        {
            NonOrthogonally_Disconnected_Worker(3, 4, true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles and landlock one by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_From_One_NonOrthogonally_Disconnected_Obstacle_4Reflections()
        {
            NonOrthogonally_Disconnected_Worker(4, 4, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles and almost-landlock one by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_From_One_NonOrthogonally_AlmostDisconnected_Obstacle_4Reflections()
        {
            NonOrthogonally_Disconnected_Worker(3, 4, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles separately landlocked by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_Between_Two_NonOrthogonally_Disconnected_Obstacles_2Reflections()
        {
            NonOrthogonally_Disconnected_Worker(4, 2, true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles separately almost-landlocked by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_Between_Two_NonOrthogonally_AlmostDisconnected_Obstacles_2Reflections()
        {
            NonOrthogonally_Disconnected_Worker(3, 2, true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles and landlock one by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_From_One_NonOrthogonally_Disconnected_Obstacle_2Reflections()
        {
            NonOrthogonally_Disconnected_Worker(4, 2, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles and almost-landlock one by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_From_One_NonOrthogonally_AlmostDisconnected_Obstacle_2Reflections()
        {
            NonOrthogonally_Disconnected_Worker(3, 2, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles separately landlocked by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_Between_Two_NonOrthogonally_Disconnected_Obstacles_1Reflection()
        {
            NonOrthogonally_Disconnected_Worker(4, 1, true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles separately almost-landlocked by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_Between_Two_NonOrthogonally_AlmostDisconnected_Obstacles_1Reflection()
        {
            NonOrthogonally_Disconnected_Worker(3, 1, true);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles and landlock one by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_From_One_NonOrthogonally_Disconnected_Obstacle_1Reflection()
        {
            NonOrthogonally_Disconnected_Worker(4, 1, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create two obstacles and almost-landlock one by non-overlapping non-orthogonal shapes (in the absence of reflections).")]
        public void Route_From_One_NonOrthogonally_AlmostDisconnected_Obstacle_1Reflection()
        {
            NonOrthogonally_Disconnected_Worker(3, 1, false);
        }

        private void NonOrthogonally_Disconnected_Worker(int numShapes, int numReflections, bool addShiftedObstacles)
        {
            const int initialCenter = 120;
            const int shiftX = 300;
            const int shiftY = 100;
            var obstacles = new List<Shape> 
            {
                    // The first two obstacles to route between.
                    this.CreateSquare(new Point(initialCenter, initialCenter), 20),
                    this.CreateSquare(new Point(initialCenter + shiftX, initialCenter + shiftY), 20)
            };

            // For fullVg, high reflection counts nudge into obstacles; for sparseVg full nonOrthogonal disconnection
            // forces  routing across obstacles.  So thus for both of these we want noVerify.
            if (((numShapes % 4) == 0) || (numReflections > 2))
            {
                this.WantVerify = false;
            }
            AddAllShiftedTriangles(obstacles, shiftX, shiftY, numShapes, numReflections, addShiftedObstacles);
            this.DoRouting(obstacles, this.CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        private static void AddAllShiftedTriangles(List<Shape> obstacles, int shiftX, int shiftY, int numShapes, int numReflections, bool addShift)
        {
            // If numShapes < 4, it's 3, and it means to omit the bottom obstacle of the original group
            // and the top obstacle of the shifted group, to illustrate the longer path taken.
            // If numReflections < 4, it's 2 (which means we only want a single reflection ScanSegment (for 2 bounces))
            // or 1 (for no reflecting scan segments, so there is only a single bounce where the vertex-derived
            // segments intersect in the channel).
            var offset = (numReflections == 4) ? 80 : ((numReflections == 2) ? 45 : 25);
            var apex = new Point(100, 120);
            AddShiftedTriangles(new[] { apex, apex + new Point(-offset, -offset), apex + new Point(-offset, offset)},
                    shiftX, shiftY, obstacles, true, addShift);
            apex = new Point(120, 140);
            AddShiftedTriangles(new[] { apex, apex + new Point(-offset, offset), apex + new Point(offset, offset) },
                    shiftX, shiftY, obstacles, true, addShift && (numShapes == 4));
            apex = new Point(140, 120);
            AddShiftedTriangles(new[] { apex, apex + new Point(offset, offset), apex + new Point(offset, -offset) },
                    shiftX, shiftY, obstacles, true, addShift);
            apex = new Point(120, 100);
            AddShiftedTriangles(new[] { apex, apex + new Point(-offset, -offset), apex + new Point(offset, -offset) },
                    shiftX, shiftY, obstacles, numShapes == 4, addShift);
        }

        private static void AddShiftedTriangles(Point[] points, int shiftX, int shiftY, List<Shape> obstacles, 
                                                bool addOriginalObstacle, bool addShiftedObstacle)
        {
            Validate.IsTrue(points.Length == 3, "Point count must be 3");
            if (addOriginalObstacle) 
            {
                obstacles.Add(CurveFromPoints(points));
            }
            if (addShiftedObstacle) 
            {
                var shiftedPoints = points.Select(p => new Point(p.X + shiftX, p.Y + shiftY)).ToArray();
                obstacles.Add(CurveFromPoints(shiftedPoints));
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Testcase where port visibility splices a non-overlapped edge across an obstacle border.")]
        public void Overlap_SpliceAcrossObstacle()
        {
            // Modified from Overlap_ExtremeSide_Lookahead.txt so that the intervening shape is an inverted triangle thus having
            // its top border forming a non-overlapped line.  Further, that triangle's bottom point is below the port, thus
            // its visibility extension to the left is below the port's VisibilityBorderIntersect and there is no scansegment
            // between it and the side of the intervening triangle.  Therefore, the splice from 16, 24 to 16, 33 crosses that
            // intervening triangle and the path takes it.  Because the intervening triangle overlaps the source or target obstacle,
            // there is no error on path verification.
            var obstacles = new List<Shape>
                {
                    CurveFromPoints(new[]
                            {
                                new Point(16.7338134, 20.9750246),
                                new Point(25.43848799, 20.9750246),
                                new Point(25.43848799, 23.35075164),
                                new Point(21.0861507, 25.72647869),
                                new Point(16.7338134, 23.35075164)
                            }),
                    CurveFromPoints(new[]
                            {
                                new Point(28.47089293, 49.38291107),
                                new Point(20.9739337, 49.38291107),
                                new Point(24.72241331, 44.1728704)
                            }),
                    CurveFromPoints(new[]
                            {
                                new Point(17.93230647, 25.63619442),
                                new Point(20.28198775, 30.30072606),
                                new Point(15.58262519, 30.30072606)
                            }),
                    CurveFromPoints(new[]
                            {
                                new Point(27.2511664, 24.82929237),
                                new Point(32.14393196, 28.94420406),
                                new Point(27.2511664, 33.05911576),
                                new Point(22.35840083, 28.94420406)
                            }),
                    this.PolylineFromRectanglePoints(new Point(15, 35), new Point(20, 40))
                };

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Testcase where port visibility splices a non-overlapped edge across an obstacle border.")]
        public void Overlap_ReflectionToken()
        {
            // This shows that even with the extreme-side vertex fix as in RectilinearFileTests.Overlap_ExtremeSide_Lookahead
            // it is possible for non-reflected portions of the non-rectilinear channel to exist, because a side belonging
            // to one obstacle is replaced by a side belonging to another obstacle, this invalidating the source->target->source
            // test for reflection continuation.  For angled overlapping sides in the scanline the solution would have been
            // to have a "reflection token" that is passed along as sides intersect, but ConvexHulls fixes the problem.
            var obstacles = new List<Shape>
                {
#if false
                    // The port is between the obstacle and padding and is spliced as overlapped.
                    CurveFromPoints(new[]
                            {
                                new Point(76.3204567, 69.0),
                                new Point(79.3204567, 72.5),
                                new Point(82.3204567, 69.0)
                            }),
#else
                    this.PolylineFromRectanglePoints(new Point(79.3204567, 69.0), new Point(82.3204567, 72.5)),
#endif
                    CurveFromPoints(new[]
                            {
                                new Point(100.94516394, 74.68593475),
                                new Point(90.98699004, 74.68593475),
                                new Point(95.96607699, 69.29660865)
                            }),
                    CurveFromPoints(new[]
                            {
                                new Point(94.3204567, 70.99269414),
                                new Point(103.27974054, 70.99269414),
                                new Point(103.27974054, 80.28837441),
                                new Point(94.3204567, 80.28837441)
                            }),
                    CurveFromPoints(new[]
                            {
                                new Point(89.3204567, 69.0),
                                new Point(89.3204567, 72.5),
                                new Point(92.3204567, 69.0)
                            }),
                };

            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test that port visibility splices do not cross when extending to a sloped triangle side.")]
        public void SpliceSourceToExtendPoint_ToTriangleSide()
        {
            var obstacles = new List<Shape> 
                {
                    PolylineFromRectanglePoints(new Point(65.0, 0.0), new Point(155.0, 20.0)),
                    PolylineFromRectanglePoints(new Point(30.0, 50.0), new Point(60.0, 80.0)),
                    CurveFromPoints(new[]
                            {
                                new Point(70.0, 40.0),
                                new Point(70.0, 90.0),
                                new Point(120.0, 65.0)
                            }),
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test that port visibility splices do not cross when extending to a sloped arrow side.")]
        public void SpliceSourceToExtend_ToArrowSide()
        {
            var obstacles = new List<Shape> 
                {
                    PolylineFromRectanglePoints(new Point(65.0, 0.0), new Point(155.0, 20.0)),
                    PolylineFromRectanglePoints(new Point(30.0, 50.0), new Point(60.0, 80.0)),
                    CurveFromPoints(new[]
                            {
                                new Point(70.0, 55.0),
                                new Point(70.0, 75.0),
                                new Point(95.0, 75.0),
                                new Point(95.0, 90.0),
                                new Point(120.0, 65.0),
                                new Point(95.0, 40.0),
                                new Point(95.0, 55.0)
                            }),
                };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test that a port that is outside its curve is treated as a FreePort rather than throwing an exception due to missing intersections.")]
        public void PortNotOnItsCurve()
        {
            var obstacles = new List<Shape> 
                {
                    PolylineFromRectanglePoints(new Point(10.0, 10.0), new Point(20.0, 20.0)),
                    PolylineFromRectanglePoints(new Point(110.0, 10.0), new Point(120.0, 20.0)),
                    PolylineFromRectanglePoints(new Point(50.0, 50.0), new Point(60.0, 60.0))
                };
            // Create the port directly, using the third shape's curve for the second shape's port.
            var abox = obstacles[0].BoundingBox;
            var sourcePort = MakeAbsoluteObstaclePort(obstacles[0], (abox.Center - new Point(abox.Width / 2, 0)));

            var bbox = obstacles[1].BoundingBox;
            var targetPort = MakeAbsoluteObstaclePort(obstacles[2], (bbox.Center + new Point(bbox.Width / 2, 0)));

            obstacles[2].Ports.Clear();
            obstacles[1].Ports.Insert(targetPort);

            var routings = new List<EdgeGeometry> { CreateRouting(sourcePort, targetPort) };

            DoRouting(obstacles, routings);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify that when an initially shorter path hits a vertex that is closed, and another path comes along that would score lower once the bend is accounted for, the new path wins")]
        public void ClosedVertexWithBends_8PortEntrances() 
        {
            // This has two PortEntrances going through {25, 29} so the correct one is selected.
            var obstacles = new List<Shape> 
            {
                PolylineFromRectanglePoints(new Point(8, 10), new Point(20, 20)),
                PolylineFromRectanglePoints(new Point(70, 40), new Point(80, 50)),
                PolylineFromRectanglePoints(new Point(30, 30), new Point(60, 100)),
                PolylineFromRectanglePoints(new Point(26, 24), new Point(34, -20))
            };
            DoRouting(obstacles, CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify that when an initially shorter path hits a vertex that is closed, and another path comes along that would score lower once the bend is accounted for, the new path wins")]
        public void ClosedVertexWithBends_2PortEntrances()
        {
            var obstacles = new List<Shape> 
            {
                PolylineFromRectanglePoints(new Point(10, 10), new Point(15, 20)),
                PolylineFromRectanglePoints(new Point(15, 30), new Point(40, 40)),
                PolylineFromRectanglePoints(new Point(25, 18), new Point(30, 25))
            };
            var sourcePort = MakeAbsoluteFreePort(new Point(20, 15));
            var targetPort = MakeAbsoluteObstaclePort(obstacles[1], new Point(40, 35));
            DoRouting(obstacles, new List<EdgeGeometry>{ CreateRouting(sourcePort, targetPort) });
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify that when an initially shorter path hits a vertex that is closed, and another path comes along that would score lower once the bend is accounted for, the new path wins")]
        public void ClosedVertexWithBends_2OffsetPortEntrances()
        {
            var obstacles = new List<Shape> 
            {
                PolylineFromRectanglePoints(new Point(10, 10), new Point(20, 20)),
                PolylineFromRectanglePoints(new Point(40, 30), new Point(50, 40))
            };
            var sourcePort = MakeAbsoluteObstaclePort(obstacles[0], new Point(20, 15));
            var targetPort = MakeAbsoluteObstaclePort(obstacles[1], new Point(40, 35));
            DoRouting(obstacles, new List<EdgeGeometry> { CreateRouting(sourcePort, targetPort) });
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Baseline to illustrate issues of using a single convex hull around overlapped obstacles")]
        public void Overlapping_Obstacles_With_NonOverlapped_Rectangle_Creating_Convex_Hull() 
        {
            Overlapping_Obstacle_With_NonOverlapped_Rectangle_Worker(false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Illustrate issues of using a single convex hull around overlapped obstacles")]
        public void Overlapping_Obstacles_With_NonOverlapped_Rectangle_Inside_Simulated_ConvexHull() 
        {
            // This makes it overlapping rather than convex hull so we get overlapped weight on the edges inside the 
            // simulated "convex hull".
            Overlapping_Obstacle_With_NonOverlapped_Rectangle_Worker(true);
        }

        private void Overlapping_Obstacle_With_NonOverlapped_Rectangle_Worker(bool wantConvexHull) 
        {
            var obstacles = new List<Shape> 
            {
                    this.PolylineFromRectanglePoints(new Point(5, 20), new Point(35, 30)),
                    this.PolylineFromRectanglePoints(new Point(45, 0), new Point(60, 50)),
                    PolylineFromPoints(new[] 
                    {
                            new Point(10.0, 40.0),
                            new Point(30.0, 80.0),
                            new Point(50.0, 40.0)
                    }),
            };

            if (wantConvexHull) 
            {
                // Create the wrapping convex hull's polyline.
                var points = obstacles.Select(shape => (Polyline)shape.BoundaryCurve).SelectMany(p => p);
                obstacles.Add(new Shape(new Polyline(ConvexHull.CalculateConvexHull(points)) { Closed = true }));
            }
            this.DoRouting(obstacles, this.CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiply-nested nonrectilinear obstacles with no overlaps - they should be in one convex hull")]
        public void Multiply_Nested_Nonrectilinear_Obstacles() 
        {
            var shapes = GetNestedAndOverlappedShapes(xDelta:0.0, makeRect:false);
            this.VerifyAllObstaclesInConvexHull(this.CreateRouter(shapes));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiply-nested rectilinear obstacles with no overlaps - they should be in one clump")]
        public void Multiply_Nested_Rectilinear_Obstacles()
        {
            var shapes = GetNestedAndOverlappedShapes(xDelta:0.0, makeRect:true);
            this.VerifyAllObstaclesInClump(this.CreateRouter(shapes));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiply-nested nonrectilinear obstacles with only the outer obstacles overlapping each other - all obstacles should be accreted into the same convex hull")]
        public void Multiply_Nested_Nonrectilinear_Obstacles_With_Outer_Overlap()
        {
            var shapes1 = GetNestedAndOverlappedShapes(xDelta:0.0, makeRect:false);
            var shapes2 = GetNestedAndOverlappedShapes(xDelta:75.0, makeRect:false);

            var router = this.CreateRouter(shapes1.Concat(shapes2));
            this.VerifyAllObstaclesInConvexHull(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiply-nested nonrectilinear obstacles with an obstacle that forces them all overlapped; they should all be in the same convex hull")]
        public void Multiply_Nested_Nonrectilinear_Obstacles_With_All_Overlap()
        {
            var shapes = GetNestedAndOverlappedShapes(xDelta:0.0, makeRect:false);
            shapes.AddRange(GetNestedAndOverlappedShapes(xDelta:90.0, makeRect:false));
            shapes.Add(PolylineFromPoints(new[]
                {
                    new Point(55.0, 40.0),
                    new Point(95.0, 60.0),
                    new Point(135.0, 40.0),
                    new Point(95.0, 20.0)
                }));

            var router = this.CreateRouter(shapes);
            this.VerifyAllObstaclesInConvexHull(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiply-nested rectilinear obstacles with only the outer obstacles overlapping each other - these should be in the same clump")]
        public void Multiply_Nested_Rectilinear_Obstacles_With_Outer_Overlap_Clump()
        {
            var shapes1 = GetNestedAndOverlappedShapes(xDelta:0.0, makeRect:true);
            var shapes2 = GetNestedAndOverlappedShapes(xDelta:75.0, makeRect:true);

            var router = this.CreateRouter(shapes1.Concat(shapes2));
            VerifyAllObstaclesInClump(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiply-nested nonrectilinear obstacles with a non-rectangular obstacle that forces them all overlapped; these should be put into a single convex hull")]
        public void Multiply_Nested_Rectilinear_Obstacles_With_Outer_Overlap_ConvexHull()
        {
            var shapes = GetNestedAndOverlappedShapes(xDelta:0.0, makeRect:true);
            shapes.AddRange(GetNestedAndOverlappedShapes(xDelta:90.0, makeRect:true));
            var shapeToAdd = PolylineFromPoints(new[] 
                {
                    new Point(85.0, 40.0),
                    new Point(95.0, 50.0),
                    new Point(105.0, 40.0),
                    new Point(95.0, 30.0)
                });
            shapes.Add(shapeToAdd);
            var router = this.CreateRouter(shapes);
            this.VerifyAllObstaclesInConvexHull(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiply-nested nonrectilinear obstacles with a rectangular obstacle that forces them all overlapped; these should be in the same clump")]
        public void Multiply_Nested_Rectilinear_Obstacles_With_All_Overlap_Clump()
        {
            var shapes = GetNestedAndOverlappedShapes(xDelta:0.0, makeRect:true);
            shapes.AddRange(GetNestedAndOverlappedShapes(xDelta:90.0, makeRect:true));
            var rectToAdd = PolylineFromPoints(new[] 
                {
                    new Point(55.0, 40.0),
                    new Point(95.0, 60.0),
                    new Point(135.0, 40.0),
                    new Point(95.0, 20.0)
                });
            rectToAdd.BoundaryCurve = Curve.PolyFromBox(rectToAdd.BoundingBox);
            shapes.Add(rectToAdd);

            var router = this.CreateRouter(shapes);
            VerifyAllObstaclesInClump(router);
        }
        private void VerifyAllObstaclesInClump(RectilinearEdgeRouterWrapper router, Shape[] siblingShapes = null, bool show = true)
        {
            router.CreateVisibilityGraph();
            var clump = router.ObsTree.GetAllObstacles().First().Clump;
            Validate.IsNotNull(clump, "Obstacles should be in a clump");
            foreach (var obstacle in router.ObsTree.GetAllObstacles())
            {
                var expectClump = IsObstacleInShapes(siblingShapes, obstacle);
                Validate.IsTrue(obstacle.IsRectangle, "Clumped obstacles should always be rectangles");
                Validate.AreEqual(obstacle.PaddedPolyline, obstacle.VisibilityPolyline, "Clumped obstacle's padded polyline should == visibility polyline");
                Validate.IsNull(obstacle.ConvexHull, "Overlapped obstacle should not have a convex hull");
                if (expectClump)
                {
                    Validate.IsTrue(obstacle.IsOverlapped, "Overlapped obstacle was not marked overlapped");
                    Validate.AreEqual(clump, obstacle.Clump, "Obstacles should be in the same clump");
                }
                else
                {
                    Validate.IsFalse(obstacle.IsOverlapped, "Non-overlapped obstacle was marked overlapped");
                }
            }
            if (show)
            {
                this.RunAndShowGraph(router);
            }
        }

        private static bool IsObstacleInShapes(Shape[] siblingShapes, Obstacle obstacle) 
        {
            return (siblingShapes == null) || siblingShapes.Any(sibShape => obstacle.InputShape == sibShape);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test multiply-nested nonrectilinear obstacles with a non-rectangular obstacle that forces them all overlapped; these should be put into a single convex hull")]
        public void Multiply_Nested_Rectilinear_Obstacles_With_All_Overlap_ConvexHull()
        {
            var shapes = GetNestedAndOverlappedShapes(xDelta:0.0, makeRect:true);
            shapes.AddRange(GetNestedAndOverlappedShapes(xDelta:90.0, makeRect:true));
            var shapeToAdd = PolylineFromPoints(new[] 
                {
                    new Point(55.0, 40.0),
                    new Point(95.0, 60.0),
                    new Point(135.0, 40.0),
                    new Point(95.0, 20.0)
                });
            shapes.Add(shapeToAdd);
            var router = this.CreateRouter(shapes);
            this.VerifyAllObstaclesInConvexHull(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test transitivity of ConvexHull creation")]
        public void Transitive_ConvexHull_Single_Accretion()
        {
            this.Transitive_Obstacles_Single_Accretion(false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test transitivity of ConvexHull creation")]
        public void Transitive_ConvexHull_Single_Accretion_Becomes_Clump_With_Rectilinear_Shapes()
        {
            this.Transitive_Obstacles_Single_Accretion(true);
        }

        private void Transitive_Obstacles_Single_Accretion(bool makeRect) 
        {
            var obstacles = new List<Shape> 
                {
                    this.PolylineFromRectanglePoints(new Point(0, 15), new Point(25, 35)),
                    this.PolylineFromRectanglePoints(new Point(10, 32), new Point(35, 55)),
                    this.PolylineFromRectanglePoints(new Point(0, 50), new Point(25, 85)),
                    this.PolylineFromRectanglePoints(new Point(15, 75), new Point(55, 100)),
                    this.PolylineFromRectanglePoints(new Point(70, 85), new Point(95, 100)),
                    PolylineFromPoints(new[] 
                    {
                            new Point(42.0, 65.0),
                            new Point(57.0, 80.0),
                            new Point(72.0, 65.0),
                            new Point(57.0, 50.0)
                    }),
                    PolylineFromPoints(new[] 
                    {
                            new Point(70.0, 60.0),
                            new Point(70.0, 70.0),
                            new Point(85.0, 75.0),
                            new Point(100.0, 70.0),
                            new Point(100.0, 60.0),
                            new Point(85.0, 55.0),
                    }),
                    this.PolylineFromRectanglePoints(new Point(62, 25), new Point(87, 45)),
                    this.PolylineFromRectanglePoints(new Point(35, 2), new Point(55, 17)),
                    this.PolylineFromRectanglePoints(new Point(120, 80), new Point(140, 100)),
                    this.PolylineFromRectanglePoints(new Point(120, 55), new Point(140, 75)),
                };
            if (makeRect)
            {
                obstacles.ForEach(shape => shape.BoundaryCurve = Curve.PolyFromBox(shape.BoundingBox));
            }
            var router = this.CreateRouter(obstacles);

            var routings = new List<EdgeGeometry>();
            routings.AddRange(this.CreateRoutingBetweenObstacles(obstacles, 1, 5));     // intra-hull
            routings.AddRange(this.CreateRoutingBetweenObstacles(obstacles, 1, 7));     // intra-hull
            routings.AddRange(this.CreateRoutingBetweenObstacles(obstacles, 1, 8));     // intra-hull
            routings.AddRange(this.CreateRoutingBetweenObstacles(obstacles, 2, 10));    // crosses truly overlapped obstacles
            routings.AddRange(this.CreateRoutingBetweenObstacles(obstacles, 3, 9));     // crosses transitively-overlapped obstacle
            this.DoRouting(obstacles, routings);
            
            if (makeRect) 
            {
                var siblingIndexes = new []{0, 1, 2, 3, 5, 6};
                this.VerifyAllObstaclesInClump(router, siblingIndexes.Select(idx => obstacles[idx]).ToArray(), show: false);
            }
            else 
            {
                this.VerifyAllObstaclesInConvexHull(router, obstacles.Where((obs, index) => index <= 8).ToArray(), show: false);
            }
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test transitivity of ConvexHull creation")]
        public void Transitive_ConvexHull_Multiple_Accretion()
        {
            this.Transitive_Obstacles_Multiple_Accretion(false, false, false);
            this.Transitive_Obstacles_Multiple_Accretion(true, false, false);
            this.Transitive_Obstacles_Multiple_Accretion(true, true, false);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test transitivity of ConvexHull creation")]
        public void Transitive_ConvexHull_Multiple_Accretion_Becomes_Separate_Clumps_With_Rectilinear_Shapes()
        {
            this.Transitive_Obstacles_Multiple_Accretion(false, false, true);
            this.Transitive_Obstacles_Multiple_Accretion(true, false, true);
            this.Transitive_Obstacles_Multiple_Accretion(true, true, true);
        }

        private void Transitive_Obstacles_Multiple_Accretion(bool makeBothHulls, bool blockAllAxis, bool makeRect)
        {
            var obstacles = new List<Shape> 
                {
                    this.PolylineFromRectanglePoints(new Point(30, 30), new Point(70, 50)),
                    this.PolylineFromRectanglePoints(new Point(140, 30), new Point(160, 50)),
                    PolylineFromPoints(new[] 
                    {
                            new Point(0.0, 20.0),
                            new Point(40.0, 20.0),
                            new Point(40.0, 0.0),
                    }),
                    this.PolylineFromRectanglePoints(new Point(0, 10), new Point(20, 70)),
                    PolylineFromPoints(new[] 
                    {
                            new Point(0.0, 60.0),
                            new Point(40.0, 80.0),
                            new Point(40.0, 60.0)
                    }),
                    PolylineFromPoints(new[] 
                    {
                            new Point(60.0, 60.0),
                            new Point(60.0, 80.0),
                            new Point(100.0, 60.0)
                    }),
                    this.PolylineFromRectanglePoints(new Point(80, 10), new Point(100, 70)),
                    PolylineFromPoints(new[] 
                    {
                            new Point(60.0, 20.0),
                            new Point(100.0, 20.0),
                            new Point(60.0, 0.0),
                    }),
                };
            if (!makeBothHulls) 
            {
                // Remove the first range, for path-drawing comparisons.
                obstacles.RemoveRange(2, 4);   
            }
            if (blockAllAxis)
            {
                obstacles.Add(this.PolylineFromRectanglePoints(new Point(48, 16), new Point(52, 20)));
                obstacles.Add(this.PolylineFromRectanglePoints(new Point(48, 60), new Point(52, 64)));
            }

            base.UseObstacleRectangles = makeRect;

            var routings = new List<EdgeGeometry>();
            routings.AddRange(this.CreateRoutingBetweenObstacles(obstacles, 0, 1));     // crosses transitively-overlapped obstacle
            var router = this.DoRouting(obstacles, routings);
            base.UseObstacleRectangles = false;

            // makeRect creates two clumps so it is only used for visual comparison.
            if (!makeRect)
            {
                this.VerifyAllObstaclesInConvexHull(router, obstacles.Where((obs, index) => index != 1).ToArray(), show: false);
            }
        }

        private void VerifyAllObstaclesInConvexHull(RectilinearEdgeRouterWrapper router, Shape[] hullShapes = null, bool show = true) 
        {
            router.CreateVisibilityGraph();
            var convexHull = router.ObsTree.GetAllObstacles().First().ConvexHull;
            Validate.IsNotNull(convexHull, "convex hull should have been created");
            foreach (var obstacle in router.ObsTree.GetAllObstacles())
            {
                var expectHull = IsObstacleInShapes(hullShapes, obstacle);
                if (expectHull) 
                {
                    Validate.IsFalse(obstacle.IsOverlapped, "objects in convex hulls should not be marked overlapped");
                    Validate.AreSame(obstacle.ConvexHull, convexHull, "All obstacles should be in the same convex hull");
                }
                else
                {
                    Validate.IsNull(obstacle.ConvexHull, "obstacles should not be in the convex hull");
                }
            }
            if (show)
            {
                this.RunAndShowGraph(router);
            }
        }

        private static List<Shape> GetNestedAndOverlappedShapes(double xDelta, bool makeRect) 
        {
            var obstacles = new List<Shape> 
            {
                    PolylineFromPoints(new[] 
                    {
                            new Point(50.0 + xDelta, 10.0),
                            new Point(10.0 + xDelta, 40.0),
                            new Point(50.0 + xDelta, 70.0),
                            new Point(90.0 + xDelta, 40.0)
                    }),
                    PolylineFromPoints(new[] 
                    {
                            new Point(50.0 + xDelta, 20.0),
                            new Point(20.0 + xDelta, 40.0),
                            new Point(50.0 + xDelta, 60.0),
                            new Point(80.0 + xDelta, 40.0)
                    }),
                    PolylineFromPoints(new[] 
                    {
                            new Point(50.0 + xDelta, 30.0),
                            new Point(30.0 + xDelta, 40.0),
                            new Point(50.0 + xDelta, 50.0),
                            new Point(70.0 + xDelta, 40.0)
                    }),
            };

            if (makeRect) 
            {
                obstacles.ForEach(shape => shape.BoundaryCurve = Curve.PolyFromBox(shape.BoundingBox));
            }
            return obstacles;
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Graph with no obstacles")]
        public void Zero_Obstacle_Graph() 
        {
            var router = this.CreateRouter(new List<Shape>());
            bool threw = false;
            try
            {
                router.CreateVisibilityGraph();
                this.ShowGraph(router);
            } 
            catch (Exception e) 
            {
                threw = true;

                // Don't use "exception" in the message to avoid the word appearing in the errorlog...
                base.WriteLine(e.Message + " (thrown/caught as expected)");
            }
            Validate.IsTrue(threw, "Empty graph did not throw");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Graph with a single obstacle")]
        public void One_Obstacle_Graph()
        {
            var shapes = new List<Shape> 
            {
                PolylineFromPoints(new[] 
                    {
                        new Point(55.0, 40.0),
                        new Point(95.0, 60.0),
                        new Point(135.0, 40.0),
                        new Point(95.0, 20.0)
                    })
            };
            var router = this.CreateRouter(shapes);
            router.CreateVisibilityGraph();
            this.ShowGraph(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("A simple group with two contained obstacles")]
        public void Group_With_Overlapping_Obstacles() 
        {
            GroupAndObstacleOverlapWorker(0);
            GroupAndObstacleOverlapWorker(1);
            GroupAndObstacleOverlapWorker(2);
        }

        private void GroupAndObstacleOverlapWorker(int numberOfOverlappedObstacles) 
        {
            List<Shape> obstacles = GetGroupAndObstacles();

            // Add any overlapped obstacles.
            if (numberOfOverlappedObstacles >= 1)
            {
                obstacles.Add(PolylineFromPoints(new[] 
                    {
                        new Point(130.0, 160.0),
                        new Point(120.0, 170.0),
                        new Point(170.0, 220.0),
                        new Point(180.0, 210.0)
                    }));
            }
            if (numberOfOverlappedObstacles >= 2)
            {
                obstacles.Add(PolylineFromRectanglePoints(new Point(180, 140), new Point(240, 160)));
            }

            var router = this.CreateRouter(obstacles);
            this.RunAndShowGraph(router);
        }

        private List<Shape> GetGroupAndObstacles()
        {
            var obstacles = new List<Shape>();

            // Add obstacles.
            Shape s1, s2;
            obstacles.Add(s1 = this.PolylineFromRectanglePoints(new Point(160, 90), new Point(180, 110)));
            s1.UserData = "s1";
            obstacles.Add(s2 = this.PolylineFromRectanglePoints(new Point(20, 90), new Point(40, 110)));
            s2.UserData = "s2";

            // Add the group.
            Shape g1;
            obstacles.Add(g1 = CurveFromPoints(
                    new[] { new Point(100, 0), new Point(0, 100), new Point(100, 200), new Point(200, 100) }));
            g1.AddChild(s1);
            return obstacles;
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Two overlapping groups")]
        public void Group_With_Overlapping_Groups() 
        {
            GroupAndObstacleOverlapWorker(0);

            var shapes = this.GetGroupAndObstacles();
            var g1 = shapes[2];
            Shape g2 = PolylineFromPoints(new[] 
                {
                    new Point(160.0, 130.0),
                    new Point(120.0, 170.0),
                    new Point(190.0, 240.0),
                    new Point(230.0, 200.0)
                });
            shapes.Add(g2);
            Shape s3 = this.PolylineFromRectanglePoints(new Point(160, 160), new Point(180, 180));
            shapes.Add(s3);
            g2.AddChild(s3);
            this.RunAndShowGraph(this.CreateRouter(shapes));

            Shape g3 = this.PolylineFromRectanglePoints(new Point(215, 60), new Point(315, 150));
            Shape s4 = this.PolylineFromRectanglePoints(new Point(255, 95), new Point(275, 115));
            shapes.Add(g3);
            shapes.Add(s4);
            g3.AddChild(s4);

            var router = this.CreateRouter(shapes);
            this.RunAndShowGraph(router);

            Obstacle group1 = null;
            Obstacle group2 = null;
            Obstacle group3 = null;
            foreach (var obstacle in router.ObsTree.GetAllObstacles()) 
            {
                // No obstacles should be clumped or convex-hulled.
                Validate.IsFalse(obstacle.IsOverlapped, "objects in convex hulls should not be marked overlapped");
                Validate.IsTrue(obstacle.IsPrimaryObstacle, "All the shapes and groups should be primary obstacles");
                if (obstacle.InputShape == g1) 
                {
                    group1 = obstacle;
                    Validate.IsTrue(obstacle.ConvexHull != null, "g1 should have a convex hull");
                }
                else
                {
                    if (obstacle.InputShape == g2) 
                    {
                        group2 = obstacle;
                    }
                    if (obstacle.InputShape == g3) 
                    {
                        group3 = obstacle;
                    }
                    Validate.IsTrue(obstacle.ConvexHull == null, "only g1 should have a convex hull");
                }
            }
            Validate.IsNotNull(group1, "g1 was not found");
            Validate.IsNotNull(group2, "g2 was not found");
            Validate.IsNotNull(group3, "g3 was not found");

            // g2 and g3 should be inside g1's convex hull but outside each other's.
            // R# doesn't recognize [ValidatedNotNull] attribute...
// ReSharper disable PossibleNullReferenceException
            Validate.IsTrue(!Curve.CurvesIntersect(group1.VisibilityPolyline, group2.VisibilityPolyline), "group2.VisibilityPolylines should not intersect group1");
            Validate.IsTrue(!Curve.CurvesIntersect(group1.VisibilityPolyline, group3.VisibilityPolyline), "group3.VisibilityPolylines should not intersect group1");
            Validate.IsTrue(!Curve.CurvesIntersect(group2.VisibilityPolyline, group3.VisibilityPolyline), "group2.VisibilityPolylines should not intersect group1");
// ReSharper restore PossibleNullReferenceException
            Validate.IsTrue(IsFirstPolylineEntirelyWithinSecond(group2.VisibilityPolyline, group1.VisibilityPolyline, false), "group2 is not inside group1");
            Validate.IsTrue(IsFirstPolylineEntirelyWithinSecond(group3.VisibilityPolyline, group1.VisibilityPolyline, false), "group3 is not inside group1");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("A group and obstacle that are both rectangular do not change.")]
        public void Group_Inside_Rectangular_Obstacle() 
        {
            Obstacle shape1, group1;
            Group_Inside_Obstacle_Worker(true, out group1, out shape1, show:true);
            Validate.IsFalse(group1.IsInConvexHull, "group1 should not have a convex hull");
            Validate.IsFalse(group1.IsOverlapped, "group1 should not have a clump");
            Validate.IsFalse(shape1.IsInConvexHull, "shape1 should not have a convex hull");
            Validate.IsFalse(shape1.IsOverlapped, "shape1 should not have a clump");
            Validate.IsTrue(IsFirstPolylineEntirelyWithinSecond(group1.VisibilityPolyline, shape1.VisibilityPolyline, false), "group1 should remain entirely within shape1");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("An obstacle inside a group inside an obstacle that are all rectangular do not change.")]
        public void Group_Inside_Rectangular_Obstacle_Contains_Rectangular_Obstacle()
        {
            Obstacle shape1, group1;
            var shapes = Group_Inside_Obstacle_Worker(true, out group1, out shape1, show:false);
            var s1 = shape1.InputShape;

            var s2 = this.PolylineFromRectanglePoints(new Point(140, 140), new Point(160, 160));
            s2.UserData = "s2";
            shapes.Add(s2);
            group1.InputShape.AddChild(s2);
            var router = this.CreateRouter(shapes);
            this.RunAndShowGraph(router);

            // Reget all obstacles as they have been re-created for the new router.
            group1 = router.ObsTree.GetAllGroups().First();
            shape1 = router.ObsTree.GetAllObstacles().Where(obs => obs.InputShape == s1).First();
            var shape2 = router.ObsTree.GetAllObstacles().Where(obs => obs.InputShape == s2).First();

            // Now the shapes should be in clumps.
            Validate.IsFalse(group1.IsInConvexHull, "group1 should not have a convex hull");
            Validate.IsFalse(group1.IsOverlapped, "group1 should not have a clump");
            Validate.IsFalse(shape1.IsInConvexHull, "shape1 should not have a convex hull");
            Validate.IsTrue(shape1.IsOverlapped, "shape1 should have a clump");
            Validate.IsTrue(IsFirstPolylineEntirelyWithinSecond(group1.VisibilityPolyline, shape1.VisibilityPolyline, false), "group1 should remain entirely within shape1");

            Validate.IsFalse(shape2.IsInConvexHull, "shape2 should not have a convex hull");
            Validate.IsTrue(shape2.IsOverlapped, "shape2 should have a clump");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("A group must grow to encompass all nonrectangular obstacles it overlaps with - even if it is initially entirely contained in that obstacle.")]
        public void Group_Inside_NonRectangular_Obstacle()
        {
            Obstacle shape1, group1;
            Group_Inside_Obstacle_Worker(false, out group1, out shape1, show:true);
            Validate.IsTrue(group1.IsInConvexHull, "group1 should have a convex hull");
            Validate.IsFalse(group1.IsOverlapped, "group1 should not have a clump");
            Validate.IsFalse(shape1.IsInConvexHull, "shape1 should not have a convex hull");
            Validate.IsFalse(shape1.IsOverlapped, "shape1 should not have a clump");
            Validate.IsTrue(IsFirstPolylineEntirelyWithinSecond(shape1.VisibilityPolyline, group1.VisibilityPolyline, false), "group1 did not grow to encompass shape1");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("A group must grow to encompass all nonrectangular obstacles it overlaps with - even if it is initially entirely contained in that obstacle.")]
        public void Group_Inside_NonRectangular_Obstacle_Contains_Rectangular_Obstacle()
        {
            Obstacle shape1, group1;
            var shapes = Group_Inside_Obstacle_Worker(false, out group1, out shape1, show:false);
            var s1 = shape1.InputShape;

            var s2 = this.PolylineFromRectanglePoints(new Point(140, 140), new Point(160, 160));
            s2.UserData = "s2";
            shapes.Add(s2);
            group1.InputShape.AddChild(s2);
            var router = this.CreateRouter(shapes);
            this.RunAndShowGraph(router);

            // Reget all obstacles as they have been re-created for the new router.
            group1 = router.ObsTree.GetAllGroups().First();
            shape1 = router.ObsTree.GetAllObstacles().Where(obs => obs.InputShape == s1).First();
            var shape2 = router.ObsTree.GetAllObstacles().Where(obs => obs.InputShape == s2).First();

            // Now we should have a convex hull for the nonrectangular obstacles.
            Validate.IsTrue(group1.IsInConvexHull, "group1 should have a convex hull");
            Validate.IsFalse(group1.IsOverlapped, "group1 should not have a clump");
            Validate.IsTrue(shape1.IsInConvexHull, "shape1 should have a convex hull");
            Validate.IsFalse(shape1.IsOverlapped, "shape1 should not have a clump");
            Validate.IsTrue(IsFirstPolylineEntirelyWithinSecond(shape1.VisibilityPolyline, group1.VisibilityPolyline, false), "group1 did not grow to encompass shape1");

            Validate.IsTrue(shape2.IsInConvexHull, "shape2 should have a convex hull");
            Validate.IsFalse(shape2.IsOverlapped, "shape2 should not have a clump");
            Validate.AreSame(shape1.ConvexHull, shape2.ConvexHull, "shape1 and shape2 should be in the same convex hull");
        }

        private List<Shape> Group_Inside_Obstacle_Worker(bool rectangularObstacle, out Obstacle group1, out Obstacle shape1, bool show) 
        {
            var obstacles = new List<Shape>();

            Shape s1;
            if (rectangularObstacle)
            {
                s1 = this.PolylineFromRectanglePoints(new Point(100, 100), new Point(200, 200));
            }
            else 
            {
                s1 = PolylineFromPoints(new[] 
                {
                    new Point(50.0, 150.0),
                    new Point(150.0, 250.0),
                    new Point(250.0, 150.0),
                    new Point(150.0, 50.0)
                });
            }
            s1.UserData = "s1";
            obstacles.Add(s1);

            // Add the group.
            Shape g1 = this.PolylineFromRectanglePoints(new Point(120, 120), new Point(180, 180));
            g1.AddChild(s1);
            obstacles.Add(g1);

            var router = this.CreateRouter(obstacles);
            router.Run();
            if (show) 
            {
                this.ShowGraph(router);
            }
            group1 = router.ObsTree.GetAllGroups().First();
            shape1 = router.ObsTree.GetAllObstacles().Where(obs => obs.InputShape == s1).First();
            return obstacles;
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate that convex hull transitivity is local so does not affect distant paths")]
        public void Transitive_ConvexHull_Is_Local_SingleReflection()
        {
            Transitive_ConvexHull_Is_Local_Worker(1);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate that convex hull transitivity is local so does not affect distant paths")]
        public void Transitive_ConvexHull_Is_Local_SingleReflection_SparseVg()
        {
            base.UseSparseVisibilityGraph = true;
            Transitive_ConvexHull_Is_Local_Worker(1);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate that convex hull transitivity is local so does not affect distant paths")]
        public void Transitive_ConvexHull_Is_Local_DoubleReflection()
        {
            Transitive_ConvexHull_Is_Local_Worker(2);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate that convex hull transitivity is local so does not affect distant paths")]
        public void Transitive_ConvexHull_Is_Local_TripleReflection()
        {
            Transitive_ConvexHull_Is_Local_Worker(3);
        }

        private void Transitive_ConvexHull_Is_Local_Worker(int numberOfReflections)
        {
            if (base.UseSparseVisibilityGraph && base.WantVerify)
            {
                base.WriteLine("Test ignored for SparseVisibilityGraph with WantVerify true, due to lack of reflections");
                return;
            }

            Validate.IsTrue(numberOfReflections < 4, "Currently only one to three reflections are supported");
            var adjustment = -5 * (numberOfReflections - 1);
            var obstacles = new List<Shape> 
            {
                // Source and target
                PolylineFromRectanglePoints(new Point(90.0, 40.0), new Point(100.0, 50.0)),
                PolylineFromRectanglePoints(new Point(120.0, 110.0), new Point(130.0, 120.0)),

                // This outside square will move from non-touching to overlapping.
                PolylineFromRectanglePoints(new Point(-10.0, 40.0), new Point(0.0, 50.0)),

                // Blocking obstacles
                PolylineFromPoints(new[] 
                {
                        new Point(10.0, 10.0),
                        new Point(10.0, 80.0),
                        new Point(30.0, 60.0),
                        new Point(30.0, 30.0)
                }),
                PolylineFromPoints(new[] 
                {
                        new Point(20.0, 90.0 + adjustment),
                        new Point(60.0, 90.0 + adjustment),
                        new Point(40.0, 70.0 + adjustment)
                }),
                PolylineFromPoints(new[] 
                {
                        new Point(70.0, 80.0),
                        new Point(90.0, 60.0),
                        new Point(50.0, 60.0)
                }),
                PolylineFromPoints(new[] 
                {
                        new Point(80.0, 90.0 + adjustment),
                        new Point(120.0, 90.0 + adjustment),
                        new Point(100.0, 70.0 + adjustment)
                }),
                PolylineFromPoints(new[] 
                {
                        new Point(130.0, 10.0),
                        new Point(110.0, 30.0),
                        new Point(110.0, 60.0),
                        new Point(130.0, 80.0)
                }),
                PolylineFromPoints(new[] 
                {
                        new Point(20.0, 10.0),
                        new Point(40.0, 30.0),
                        new Point(100.0, 30.0),
                        new Point(120.0, 10.0)
                }),
            };

            // This won't have any overlaps.
            this.DoRouting(obstacles, this.CreateRoutingBetweenObstacles(obstacles, 0, 1));

            // Make the outer object overlap and see how it affects the unrelated path.
            obstacles[2].BoundaryCurve.Translate(new Point(15, 0.0));
            this.DoRouting(obstacles, this.CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Handle all-rectangles with obstacles overlapping group sides and corners")]
        public void Rectangular_Obstacles_Overlapping_Rectangular_Group_Sides_And_Corners()
        {
            var obstacles = new List<Shape> 
            {
                PolylineFromRectanglePoints(new Point(100.0, 100.0), new Point(200.0, 200.0)),

                // Corners
                PolylineFromRectanglePoints(new Point(90.0, 90.0), new Point(110.0, 110.0)),
                PolylineFromRectanglePoints(new Point(90.0, 190.0), new Point(110.0, 210.0)),
                PolylineFromRectanglePoints(new Point(190.0, 190.0), new Point(210.0, 210.0)),
                PolylineFromRectanglePoints(new Point(190.0, 90.0), new Point(210.0, 110.0)),

                // Sides
                PolylineFromRectanglePoints(new Point(90.0, 140.0), new Point(110.0, 160.0)),
                PolylineFromRectanglePoints(new Point(140.0, 190.0), new Point(160.0, 210.0)),
                PolylineFromRectanglePoints(new Point(190.0, 140.0), new Point(210.0, 160.0)),
                PolylineFromRectanglePoints(new Point(140.0, 90.0), new Point(160.0, 110.0)),
            };

            this.RunAndShowGraph(this.CreateRouter(obstacles));
        }

        private void RunAndShowGraph(RectilinearEdgeRouterWrapper router) 
        {
            router.Run();
            this.ShowGraph(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("The nudger should remove staircases in the space between convex hulls if there are no obstacles")]
        public void NudgerSmoothingStaircasesAlongConvexHulls()
        {
            var obstacles = new List<Shape> 
            {
                // Source and target
                PolylineFromRectanglePoints(new Point(130.0, 140.0), new Point(140.0, 150.0)),
                PolylineFromRectanglePoints(new Point(160.0, 20.0), new Point(170.0, 30.0)),

                // The wide rectangles that make "going around" too expensive, forcing the reflection path.
                PolylineFromRectanglePoints(new Point(0.0, 40.0), new Point(160.0, 60.0)),
                PolylineFromRectanglePoints(new Point(140.0, 100.0), new Point(300.0, 120.0)),

                // The smaller squares that form the other corners of the convex hull.
                PolylineFromRectanglePoints(new Point(100.0, 80.0), new Point(120.0, 100.0)),
                PolylineFromRectanglePoints(new Point(180.0, 60.0), new Point(200.0, 80.0)),

                // The two trianges that force convex hull creation rather than clumps.
                PolylineFromPoints(new [] {new Point(110.0, 50.0), new Point(80.0, 70.0), new Point(110.0, 90.0) }),
                PolylineFromPoints(new [] {new Point(190.0, 70.0), new Point(190.0, 110.0), new Point(220.0, 90.0) }),
            };

            this.DoRouting(obstacles, this.CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("The nudger should remove staircases in the space between convex hulls if there are no obstacles")]
        public void Reflections_Taken_And_Skipped()
        {
            var obstacles = new List<Shape> 
            {
                // Source and target
                PolylineFromRectanglePoints(new Point(120.0, 140.0), new Point(130.0, 150.0)),
                PolylineFromRectanglePoints(new Point(170.0, 20.0), new Point(180.0, 30.0)),

                // The blocking parallelograms.
                PolylineFromPoints(new[] {new Point(160.0, 60.0), new Point(0.0, 60.0), new Point(-40.0, 100), new Point(120, 100) }),
                PolylineFromPoints(new[] {new Point(180.0, 60.0), new Point(140.0, 100.0), new Point(300.0, 100.0), new Point(340.0, 60.0) }),
            };

            this.DoRouting(obstacles, this.CreateRoutingBetweenObstacles(obstacles, 0, 1));

            // Now increase the size of the blockers so that it becomes cost-effective to take the shortcut.
            obstacles[2] = PolylineFromPoints(new [] {new Point(160.0, 60.0), new Point(100.0, 60.0), new Point(60.0, 100), new Point(120, 100) });
            obstacles[3] = PolylineFromPoints(new [] {new Point(180.0, 60.0), new Point(140.0, 100.0), new Point(200.0, 100.0), new Point(240.0, 60.0) });

            this.DoRouting(obstacles, this.CreateRoutingBetweenObstacles(obstacles, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify the correct removal of vertices that are ApproximateComparer.Close")]
        public void RemoveCloseVerticesFromPolyline() 
        {
            // Make an octagon so we have enough room for a midpoint.
            var origPoly = CurveFactory.CreateOctagon(100, 100, new Point(200, 200));
            Validate.AreEqual(8, origPoly.PolylinePoints.Count(), "Did not get 8 points in original octagon Polyline");

            var offset = new Point(ApproximateComparer.IntersectionEpsilon / 2, ApproximateComparer.IntersectionEpsilon / 2);
            VerifyCloseIndividualPolylinePoints(origPoly, offset);
            VerifyCloseAllPolylinePoints(origPoly, offset);
            VerifyCloseStartAndEndPolylinePoints(origPoly, offset);
        }

        private static void VerifyCloseIndividualPolylinePoints(Polyline origPoly, Point offset)
        {
            var origPoints = origPoly.PolylinePoints.Select(ppt => ppt.Point).ToArray();
            for (int indexToDup = 0; indexToDup < origPoints.Length; ++indexToDup)
            {
                var newPoly = new Polyline();
                for (int ii = 0; ii < origPoints.Length; ++ii)
                {
                    newPoly.AddPoint(origPoints[ii]);
                    if (ii == indexToDup)
                    {
                        newPoly.AddPoint(origPoints[ii] + offset);
                    }
                }
                Validate.AreEqual(origPoints.Length + 1, newPoly.PolylinePoints.Count(), "Did not add close point to newPolyline");
                var testPoly = Obstacle.RemoveCloseAndCollinearVerticesInPlace(ClonePolyline(newPoly));
                VerifyPolylinesAreClose(origPoly, testPoly);
            }
        }

        private static void VerifyCloseAllPolylinePoints(Polyline origPoly, Point offset)
        {
            var newPoly = new Polyline();
            foreach (var origPpt in origPoly.PolylinePoints)
            {
                newPoly.AddPoint(origPpt.Point);
                newPoly.AddPoint(origPpt.Point + offset);
            }
            Validate.AreEqual(origPoly.PolylinePoints.Count() * 2, newPoly.PolylinePoints.Count(), "Did not add all close points to newPolyline");
            var testPoly = Obstacle.RemoveCloseAndCollinearVerticesInPlace(ClonePolyline(newPoly));
            VerifyPolylinesAreClose(origPoly, testPoly);
        }

        private static void VerifyCloseStartAndEndPolylinePoints(Polyline origPoly, Point offset)
        {
            var newPoly = new Polyline();
            newPoly.AddPoint(origPoly.Start);
            newPoly.AddPoint(origPoly.Start + offset);
            foreach (var origPpt in origPoly.PolylinePoints.Skip(1))
            {
                newPoly.AddPoint(origPpt.Point);
            }
            newPoly.AddPoint(origPoly.End + offset);
            Validate.AreEqual(origPoly.PolylinePoints.Count() + 2, newPoly.PolylinePoints.Count(), "Did not add all close points to newPolyline");
            var testPoly = Obstacle.RemoveCloseAndCollinearVerticesInPlace(ClonePolyline(newPoly));
            VerifyPolylinesAreClose(origPoly, testPoly);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify the correct removal of vertices that are collinear with other vertices")]
        public void RemoveCollinearVerticesFromPolyline()
        {
            // Make an octagon so we have enough room for a midpoint.
            var origPoly = CurveFactory.CreateOctagon(100, 100, new Point(200, 200));
            Validate.AreEqual(8, origPoly.PolylinePoints.Count(), "Did not get 8 points in original octagon Polyline");

            VerifyCollinearIndividualPolylinePoints(origPoly);
            VerifyCollinearAllPolylinePoints(origPoly);
            VerifyCollinearStartAndEndPolylinePoints(origPoly);
        }

        private static void VerifyPolylinesAreClose(Polyline origPoly, Polyline testPoly)
        {
            var count = origPoly.PolylinePoints.Count();
            Validate.AreEqual(count, testPoly.PolylinePoints.Count(), "testPolyline.PolylinePoints.Count");
            var origPpt = origPoly.StartPoint;
            var testPpt = testPoly.StartPoint;
            for (int ii = 0; ii < count; ++ii)
            {
                Validate.IsTrue(ApproximateComparer.CloseIntersections(origPpt.Point, testPpt.Point), "testPoly point is not close to origPoly point");
                origPpt = origPpt.NextOnPolyline;
                testPpt = testPpt.NextOnPolyline;
            }
        }

        private static void VerifyCollinearIndividualPolylinePoints(Polyline origPoly)
        {
            var count = origPoly.PolylinePoints.Count();
            for (int indexToDup = 0; indexToDup < count; ++indexToDup)
            {
                var newPoly = new Polyline();
                var origPpt = origPoly.StartPoint;
                for (int ii = 0; ii < count; ++ii)
                {
                    newPoly.AddPoint(origPpt.Point);
                    if (ii == indexToDup)
                    {
                        var offset = GetCollinearOffset(origPpt, 0.5);
                        newPoly.AddPoint(origPpt.Point + offset);
                    }
                    origPpt = origPpt.NextOnPolyline;
                }
                Validate.AreEqual(count + 1, newPoly.PolylinePoints.Count(), "Did not add close point to newPolyline");
                var testPoly = Obstacle.RemoveCloseAndCollinearVerticesInPlace(ClonePolyline(newPoly));
                VerifyPolylinesAreClose(origPoly, testPoly);
            }
        }

        private static void VerifyCollinearAllPolylinePoints(Polyline origPoly)
        {
            var newPoly = new Polyline();
            foreach (var origPpt in origPoly.PolylinePoints)
            {
                var offset = GetCollinearOffset(origPpt, 0.5);
                newPoly.AddPoint(origPpt.Point);
                newPoly.AddPoint(origPpt.Point + offset);
            }
            Validate.AreEqual(origPoly.PolylinePoints.Count() * 2, newPoly.PolylinePoints.Count(), "Did not add all close points to newPolyline");
            var testPoly = Obstacle.RemoveCloseAndCollinearVerticesInPlace(ClonePolyline(newPoly));
            VerifyPolylinesAreClose(origPoly, testPoly);
        }

        private static void VerifyCollinearStartAndEndPolylinePoints(Polyline origPoly)
        {
            var newPoly = new Polyline();

            // Offset to create newPoly.Start/End so that the removal will leave newPoly matching up with origPoly at Start/End.
            var offsetToNewEnd = GetCollinearOffset(origPoly.EndPoint, 0.33);
            var offsetToNewStart = GetCollinearOffset(origPoly.EndPoint, 0.66);
            newPoly.AddPoint(origPoly.End + offsetToNewStart);
            newPoly.AddPoint(origPoly.Start);
            foreach (var origPpt in origPoly.PolylinePoints.Skip(1))
            {
                newPoly.AddPoint(origPpt.Point);
            }
            newPoly.AddPoint(origPoly.End + offsetToNewEnd);
            Validate.AreEqual(origPoly.PolylinePoints.Count() + 2, newPoly.PolylinePoints.Count(), "Did not add all close points to newPolyline");
            var testPoly = Obstacle.RemoveCloseAndCollinearVerticesInPlace(ClonePolyline(newPoly));
            VerifyPolylinesAreClose(origPoly, testPoly);
        }

        private static Point GetCollinearOffset(PolylinePoint polyPoint, double distance) 
        {
            return ((polyPoint.NextOnPolyline.Point - polyPoint.Point) * distance);
        }

        private static Polyline ClonePolyline(Polyline source) 
        {
            // Some tests clone the polyline to make it easier to debug if there is a failure on a call that modifies the test polyline.
            return new Polyline(source.PolylinePoints.Select(ppt => ppt.Point));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate reflection staircases stopping when going outside the reflecting obstacles' bounding boxes")]
        public void Reflection_Staircase_Stops_At_BoundingBox_Side_NorthWest()
        {
            this.Reflection_Staircase_Stops_Worker(0);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate reflection staircases stopping when going outside the reflecting obstacles' bounding boxes")]
        public void Reflection_Staircase_Stops_At_BoundingBox_Side_NorthEast()
        {
            this.Reflection_Staircase_Stops_Worker(1.5);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate reflection staircases stopping when going outside the reflecting obstacles' bounding boxes")]
        public void Reflection_Staircase_Stops_At_BoundingBox_Side_SouthEast()
        {
            this.Reflection_Staircase_Stops_Worker(1.0);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate reflection staircases stopping when going outside the reflecting obstacles' bounding boxes")]
        public void Reflection_Staircase_Stops_At_BoundingBox_Side_SouthWest()
        {
            this.Reflection_Staircase_Stops_Worker(0.5);
        }

        private void Reflection_Staircase_Stops_Worker(double rotation)
        {
            // This xOffset positions the final obstacle so it reflects the horizontal line from the first obstacle's
            // open vertex, such that the vertical reflection is along the second (nested) obstacle's vertical closeVertexEvent.
            // The magic number is the distance from the unpadded obstacle vertex to the corresponding padded obstacle vertex,
            // which is not 1 because of the angle.  This is then multiplied by the number of bounces-to-vertical (2) and
            // .5 for the bounce-to-horizontal.  Or something like that.
            const double xOffset = 2.5 * 1.414214;
            var shapes = new List<Shape> 
            {
                    PolylineFromPoints(new[] 
                    {
                            new Point(100.0, 100.0),
                            new Point(0.0, 200.0),
                            new Point(100.0, 300.0),
                            new Point(200.0, 200.0)
                    }),
                    PolylineFromPoints(new[] 
                    {
                            new Point(50.0 + xOffset, 0.0),
                            new Point(300.0 + xOffset, 250.0),
                            new Point(450.0 + xOffset, 100.0),
                            new Point(200.0 + xOffset, -150.0)
                    }),
            };

            if (rotation > 0.0) 
            {
                foreach (var shape in shapes)
                {
                    shape.BoundaryCurve = shape.BoundaryCurve.Transform(PlaneTransformation.Rotation(Math.PI * rotation));
                }
            }

            var router = this.CreateRouter(shapes);
            RunAndShowGraph(router);
            Validate.AreEqual(1, router.GraphGenerator.HorizontalScanSegments.Segments.Where(seg => seg.Weight == ScanSegment.ReflectionWeight).Count(),
                                "Number of Horizontal reflection segments");
            Validate.AreEqual(1, router.GraphGenerator.VerticalScanSegments.Segments.Where(seg => seg.Weight == ScanSegment.ReflectionWeight).Count(),
                                "Number of Vertical reflection segments");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate that pending reflection lookahead sites are detected when the side has been loaded before the site was stored")]
        public void ReflectionsDetectedByAlreadyLoadedSide()
        {
            var shapes = new List<Shape> 
            {
                    PolylineFromPoints(new[] { new Point(0.0, 0.0), new Point(0.0, 70.0), new Point(70.0, 70.0) }),
                    PolylineFromPoints(new[] { new Point(30.0, 10.0), new Point(25.0, 15.0), new Point(45.0, 35.0), new Point(50, 30) }),
                    PolylineFromPoints(new[] { new Point(60.0, 20.0), new Point(55.0, 25.0), new Point(75.0, 45.0), new Point(80, 40) }),
            };
            var router = this.CreateRouter(shapes);
            RunAndShowGraph(router);
            Validate.AreEqual(2, router.GraphGenerator.HorizontalScanSegments.Segments.Where(seg => seg.Weight == ScanSegment.ReflectionWeight).Count(),
                                "Number of Horizontal reflection segments");
            Validate.AreEqual(3, router.GraphGenerator.VerticalScanSegments.Segments.Where(seg => seg.Weight == ScanSegment.ReflectionWeight).Count(),
                                "Number of Vertical reflection segments");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate that closing an obstacle underneath another obstacle with rightward leaning sides does not create a reversed-direction lookahead")]
        public void ReflectionsSitedByLowSideAreNotLoadedByHighSide() {
            var shapes = new List<Shape> {
                    PolylineFromPoints(new[] { new Point(10.0, 0.0), new Point(0.0, 10.0), new Point(70.0, 80.0), new Point(80, 70)}),
                    PolylineFromPoints(new[] { new Point(50.0, 20.0), new Point(75.0, 45.0), new Point(75.0, 20.0) }),
                    PolylineFromPoints(new[] { new Point(-15.0, 25.0), new Point(-25.0, 35.0), new Point(25.0, 85.0), new Point(35, 75)}),
            };
            var router = this.CreateRouter(shapes);
            RunAndShowGraph(router);
            Validate.AreEqual(4, router.GraphGenerator.HorizontalScanSegments.Segments.Where(seg => seg.Weight == ScanSegment.ReflectionWeight).Count(),
                                "Number of Horizontal reflection segments");
            Validate.AreEqual(3, router.GraphGenerator.VerticalScanSegments.Segments.Where(seg => seg.Weight == ScanSegment.ReflectionWeight).Count(),
                                "Number of Vertical reflection segments");
            
            // The reflection site of interest is at (12, 23), generating an upward (North) reflection segment.
            Validate.IsTrue(router.GraphGenerator.VerticalScanSegments.Segments.Any(
                    seg => ApproximateComparer.CloseIntersections(seg.Start, new Point(12.171572, 23.585786))
                        && ApproximateComparer.CloseIntersections(seg.End, new Point(12.171572, 50.757358))
                        && (seg.Weight == ScanSegment.ReflectionWeight)), "Expected reflection segment was not found");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Demonstrate that a side which has loaded a reflection event will only remove exactly that event, in case it was intercepted by another obstacle")]
        public void ReflectionsRemoveInterceptedSite()
        {
            var shapes = new List<Shape> {
                    PolylineFromPoints(new[] { new Point(50.0, 50.0), new Point(40.0, 60.0), new Point(120.0, 140.0), new Point(130, 130)}),
                    PolylineFromPoints(new[] { new Point(90.0, 70.0), new Point(105.0, 85.0), new Point(105.0, 70.0) }),
                    PolylineFromPoints(new[] { new Point(90.0, 20.0), new Point(80.0, 30.0), new Point(140.0, 90.0), new Point(150, 80)}),
            };
            var router = this.CreateRouter(shapes);
            RunAndShowGraph(router);
            Validate.AreEqual(0, router.GraphGenerator.HorizontalScanSegments.Segments.Where(seg => seg.Weight == ScanSegment.ReflectionWeight).Count(),
                                "Number of Horizontal reflection segments");
            Validate.AreEqual(0, router.GraphGenerator.VerticalScanSegments.Segments.Where(seg => seg.Weight == ScanSegment.ReflectionWeight).Count(),
                                "Number of Vertical reflection segments");
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test splicing in FreePort visibility when the freeport is on a TransientVisibilityEdge")]
        public void FreePortLocationRelativeToTransientVisibilityEdges() {
            FreePortLocationsRelativeToTransientVE_Driver();
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test splicing in FreePort visibility when the freeport is on a TransientVisibilityEdge in sparseVg")]
        public void FreePortLocationRelativeToTransientVisibilityEdgesSparseVg()
        {
            base.UseSparseVisibilityGraph = true;
            FreePortLocationsRelativeToTransientVE_Driver();
        }

        private void FreePortLocationsRelativeToTransientVE_Driver() 
        {
            FreePortLocationsRelativeToTransientVE_Worker(true, new Point(0, 2));       // North of the midpoint between intersecting scan segments
            FreePortLocationsRelativeToTransientVE_Worker(true, new Point(2, 0));       // East of the midpoint between intersecting scan segments
            FreePortLocationsRelativeToTransientVE_Worker(true, new Point(0, -2));      // South of the midpoint between intersecting scan segments
            FreePortLocationsRelativeToTransientVE_Worker(true, new Point(-2, 0));      // West of the midpoint between intersecting scan segments
        }

        private void FreePortLocationsRelativeToTransientVE_Worker(bool isHorizontal, Point freePointOffset)
        {
            // The separation is so we can get testing with sparseVg skipping over some of the immediately adjacent intersections.
            var shapes = new List<Shape> {
                    PolylineFromRectanglePoints(new Point(10.0, 10.0), new Point(20.0, 20.0)),
                    PolylineFromRectanglePoints(new Point(10.0, 30.0), new Point(20.0, 40.0)),      // horizontal source
                    PolylineFromRectanglePoints(new Point(10.0, 50.0), new Point(20.0, 60.0)),
                    PolylineFromRectanglePoints(new Point(10.0, 70.0), new Point(20.0, 80.0)),

                    PolylineFromRectanglePoints(new Point(30.0, 90.0), new Point(40.0, 100.0)),
                    PolylineFromRectanglePoints(new Point(50.0, 90.0), new Point(60.0, 100.0)),
                    PolylineFromRectanglePoints(new Point(70.0, 90.0), new Point(80.0, 100.0)),     // vertical source
                    PolylineFromRectanglePoints(new Point(90.0, 90.0), new Point(100.0, 100.0)),
            };

            const int horizSourceId = 1;
            const int verticalSourceId = 6;
            var sourceId = isHorizontal ? horizSourceId : verticalSourceId;
            var sourceObstacle = shapes[sourceId];

            var freePortMidpoint = new Point(shapes[verticalSourceId].BoundingBox.Center.X, shapes[horizSourceId].BoundingBox.Center.Y);
            var freePortLoc = freePortMidpoint + freePointOffset;

            var freePort = MakeAbsoluteFreePort(freePortLoc);
            var sourcePort = MakeAbsoluteObstaclePort(sourceObstacle, sourceObstacle.BoundingBox.Center);

            var router = CreateRouter(shapes);

            // Add the simple obstacle-to-obstacle port.
            AddRoutingPorts(router, sourcePort, freePort);
            this.RunAndShowGraph(router);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Handle the case where the VisibilityBorderIntersect is on an incoming ScanSegment.")]
        public void PaddedBorderIntersectMeetsIncomingScanSegment()
        {
            var shapes = CreateTwoTestSquaresWithSentinels();

            // This tests the splice, so add a shape blocker1 in the middle to avoid direct visibility intersection.
            shapes.Add(this.PolylineFromRectanglePoints(new Point(150, 10), new Point(160, 110)));

            // Modify the source to have a near-vertical right side.  This means we'll have floating-point
            // rounding on it, and will see how that agrees with the scansegment coordinate from blocker2 below.
            // leftBottom avoids the conversion from almost-rectangle to rectangle.  rightTop creates the shallow-angled side.
            {
                var a = shapes[0];
                var abox = a.BoundingBox;
                var leftBottom = abox.LeftBottom / 2;
                var rightTop = abox.RightTop - new Point(2.5, 0);
                var leftTop = abox.LeftTop;
                var rightBottom = abox.RightBottom;
                shapes[0] = PolylineFromPoints(new[] { leftBottom, leftTop, rightTop, rightBottom });
            }

            // +		this.VisibilityBorderIntersect	(98.90625 87.015618)	Microsoft.Msagl.Core.Geometry.Point
            shapes.Add(this.PolylineFromRectanglePoints(new Point(115, 40), new Point(130, 86.015618)));
            this.DoRouting(shapes, this.CreateRoutingBetweenObstacles(shapes, 0, 1));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Collinear obstacles (and PortEntrances) in Convex Hulls may create collinear unpadded-to-paddedBorderIntersect Transient edges.")]
        public void RoutingBetweenCollinearObstaclesInConvexHull()
        {
            // Overlap these to create a convex hull.  We verify that the fact that the edges are extended by
            // collinear creation does not create difficulties; we do not care that the weights are not
            // symmetrical (the first one wins).
            var shapes = new List<Shape> {
                    PolylineFromPoints(new[] { new Point(50.0, 50.0), new Point(60.0, 80.0), new Point(70.0, 50.0) }),
                    PolylineFromPoints(new[] { new Point(65.0, 50.0), new Point(75.0, 80.0), new Point(85.0, 50.0) })
            };
            this.DoRouting(shapes, this.CreateRoutingBetweenObstacles(shapes, 0, 1));
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("Test to produce document illustration; may be run with or without SparseVg.")]
        public void Document_Illustration1()
        {
            var shapes = new List<Shape> {
                    PolylineFromPoints(new[] { new Point(10.0, 50.0), new Point(0.0, 70.0), new Point(20.0, 70.0) }),
                    PolylineFromPoints(new[] { new Point(50.0, 50.0), new Point(35.0, 80.0), new Point(65.0, 80.0) }),
                    PolylineFromPoints(new[] { new Point(90.0, 50.0), new Point(80.0, 70.0), new Point(100.0, 70.0) }),
                    PolylineFromPoints(new[] { new Point(130.0, 50.0), new Point(120.0, 70.0), new Point(140.0, 70.0) }),
                    PolylineFromRectanglePoints(new Point(160, 40), new Point(180, 80)),
                    PolylineFromRectanglePoints(new Point(200, 60), new Point(220, 75)),
            };
            this.DoRouting(shapes, null);
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("Test to produce document illustration; may be run with or without SparseVg.")]
        public void Document_Illustration2()
        {
            this.WantPaths = false;
            InterOverlapShortCircuit_Worker(0, true /*wantMiddle*/);
        }
    }
}
