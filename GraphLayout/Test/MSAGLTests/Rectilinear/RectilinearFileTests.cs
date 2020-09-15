// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectilinearFileTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;

namespace Microsoft.Msagl.UnitTests.Rectilinear
{
    /// <summary>
    /// Rectilinear edge routing tests from data files that carry the test definition and verification information.
    /// </summary>
    // The DeploymentItem for MSAGLGeometryGraphs is into the flat directory to match other module tests. 
    [TestClass]
    [DeploymentItem(@"Resources\Rectilinear\Data", @"Rectilinear\Data")]
    [DeploymentItem(@"Resources\MSAGLGeometryGraphs")]
    [Ignore]
    public class RectilinearFileTests : RectilinearVerifier
    {
        private RectilinearEdgeRouterWrapper RunRectFile(string pathAndFileSpec)
        {
            var rfp = new RectFileProcessor(Console.WriteLine, this.LogError, CheckResult,
                                            TestPointComparer.DefaultNumberOfDigitsToRound,
                                            verbose: false, quiet: false);
            rfp.ProcessFiles(pathAndFileSpec);
            this.VerifyTestFileExisted(rfp.NumberOfFilesProcessed, pathAndFileSpec);
            return rfp.Router;
        }

        private void RunGeomFile(string pathAndFileSpec)
        {
            var gfp = new GeomFileProcessor(Console.WriteLine, this.LogError, CreateRouterForGeomFile,
                                            verbose: false, quiet: false);
            gfp.ProcessFiles(pathAndFileSpec);
            this.VerifyTestFileExisted(gfp.NumberOfFilesProcessed, pathAndFileSpec);
        }

        private void VerifyTestFileExisted(int numberOfFilesProcessed, string pathAndFileSpec)
        {
            if (0 == numberOfFilesProcessed)
            {
                this.WriteLine("Could not find file(s): {0}", pathAndFileSpec);
            }
            Validate.IsTrue(numberOfFilesProcessed > 0, "Could not find file");
        }

        private RectilinearEdgeRouterWrapper CheckResult(RectFileReader reader)
        {
            // RectilinearVerifier members; must explicitly reinitialize members from the reader.
            this.InitializeMembers(reader);

            var router = DoRouting(reader);
            reader.VerifyRouter(router);
            return router;
        }

        [TestInitialize]
        public override void Initialize()
        {
            // Individual tests may set overrides; this ensures they are cleared.
            base.ClearOverrideMembers();
            base.Initialize();
        }

        private RectilinearEdgeRouterWrapper CreateRouterForGeomFile(IEnumerable<Shape> obstacles)
        {
            // Need this call for Geom files because there is no Initialize that propagates them to the overridden property.
            base.OverrideMembers();
            return CreateRouter(obstacles);
        }

        private string GetTestFileName(string nameBase, FileAccess fileAccess)
        {
            string outputDir = (null != this.TestContext) ? this.TestContext.DeploymentDirectory : Path.GetTempPath();
            return GetFullTestFileName(fileAccess, outputDir, nameBase);
        }

        private string GetCurrentMethodTestFileName(FileAccess fileAccess)
        {
            string outputDir;
            string methodName = GetMethodName(out outputDir);
            return GetFullTestFileName(fileAccess, outputDir, methodName);
        }

        private string GetCurrentMethodDotTestFileName(FileAccess fileAccess)
        {
            string outputDir;
            string methodName = GetMethodName(out outputDir);
            string convertedMethodName = methodName.Replace("_Dot", ".Dot");
            return GetFullTestFileName(fileAccess, outputDir, convertedMethodName);
        }

        private string GetMethodName(out string outputDir)
        {
            string methodName;
            if (null != this.TestContext)
            {
                methodName = this.TestContext.TestName;
                outputDir = this.TestContext.DeploymentDirectory;
            }
            else
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);  // skip current frame
                methodName = stackFrame.GetMethod().Name;
                outputDir = Path.GetTempPath();
            }
            return methodName;
        }

        private string GetFullTestFileName(FileAccess fileAccess, string outputDir, string methodName)
        {
            // For tests that create files use the Out dir; for those that only read, use the file-DeploymentItem dir.
            var subDir = (0 == (fileAccess & FileAccess.Write)) ? @"Rectilinear\Data" : string.Empty;
            var fullName = Path.Combine(outputDir, subDir, methodName + ".txt");
            this.WriteLine(fullName);
            return fullName;
        }

        private string GetGeomGraphFileName(string graphName)
        {
            var dirName = (null != this.TestContext) ? this.TestContext.DeploymentDirectory : Path.GetTempPath();
            return Path.Combine(dirName, graphName);
        }

        private void WriteRectFile(RectilinearEdgeRouterWrapper router, string fileName)
        {
            // For these small tests we write everything and have no freeOports.
            using (var writer = new RectFileWriter(router, this))
            {
                writer.WriteFile(fileName);
            }
        }

        // ReSharper disable InconsistentNaming

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify geometry graph file loading and routing with overlaps")]
        public void RouteEdgesB466MsaglGeometryGraph()
        {
            this.RunGeomFile(this.GetGeomGraphFileName("b466.msagl.geom"));
        }

        [TestMethod]
#if TEST_MSAGL
        [Timeout(30 * 1000)]
#else 
        [Timeout(12 * 1000)]
#endif
        [TestCategory("LayoutPerfTest")]
        [TestCategory("NonRollingBuildTest")]
        [Description("Simple timed test of KDTree routing over a large graph")]
        public void RouteEdgesKDTree1138Bus()
        {
            base.OverrideUseSparseVisibilityGraph = true;
            this.RunGeomFile(this.GetGeomGraphFileName("GeometryGraph_1138bus.msagl.geom"));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create and run a simple test file")]
        public void Create_And_Run_Simple_Test()
        {
            var fileName = this.GetCurrentMethodTestFileName(FileAccess.ReadWrite);
            
            // Without groups this is a simple routing.
            var router = this.GroupTest_Simple_Worker(wantGroup: false);
            this.WriteRectFile(router, fileName);
            this.RunRectFile(fileName);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create and run a simple group test file")]
        public void Create_And_Run_Simple_GroupTest()
        {
            var fileName = this.GetCurrentMethodTestFileName(FileAccess.ReadWrite);

            // Add a simple single group.
            var router = this.GroupTest_Simple_Worker(wantGroup: true);
            this.WriteRectFile(router, fileName);
            this.RunRectFile(fileName);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Create and run a simple waypoint test file")]
        public void Create_And_Run_Simple_WaypointTest()
        {
            var fileName = this.GetCurrentMethodTestFileName(FileAccess.ReadWrite);

            // Add a simple single group.
            var router = this.RunSimpleWaypoints(numPoints: 4, multiplePaths: false, wantTopRect: true);
            this.WriteRectFile(router, fileName);
            this.RunRectFile(fileName);
        }

        [TestMethod]
        [Timeout(3000)]
        [Description("Create and run an E-R PortEntry test file")]
        public void Create_And_Run_ER_PortEntryTest()
        {
            var fileName = this.GetCurrentMethodTestFileName(FileAccess.ReadWrite);

            // Add a simple single group.
            var router = this.Run_PortEntry_ERSource_FullSideTarget();
            this.WriteRectFile(router, fileName);
            this.RunRectFile(fileName);
        }

        [TestMethod]
        [Timeout(3000)]
        [Description("Create and run a simple test with a freePort and two multiPorts")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Known bug in CA/FxCop won't recognize 'multi' in CustomDictionary.")]
        public void Create_And_Run_Free_And_MultiLocPortTest()
        {
            var fileName = this.GetCurrentMethodTestFileName(FileAccess.ReadWrite);

            // Add a simple single group.
            const int FirstMultiOffsetIndex = 2;
            var routerWritten = this.CreateAndRouteTwoObstaclesWithFreeAndMultiLocPorts(FirstMultiOffsetIndex);
            var firstMultiPortWritten = (MultiLocationFloatingPort)routerWritten.Obstacles.First().Ports.First();
            Validate.AreEqual(firstMultiPortWritten.ActiveOffsetIndex, FirstMultiOffsetIndex, "FirstMultiOffsetIndex differ");

            this.WriteRectFile(routerWritten, fileName);
            var routerRead = this.RunRectFile(fileName);
            var firstMultiPortRead = (MultiLocationFloatingPort)routerRead.Obstacles.First().Ports.First();
            Validate.AreEqual(firstMultiPortWritten.ActiveOffsetIndex, firstMultiPortRead.ActiveOffsetIndex, "ActiveOffsetIndexes differ");
            Validate.AreEqual(firstMultiPortWritten.Location, firstMultiPortRead.Location, "Locations differ");
            Validate.AreEqual(firstMultiPortWritten.LocationOffsets.Count(), firstMultiPortRead.LocationOffsets.Count(), "LocationOffsets counts differ");
            var enumWritten = firstMultiPortWritten.LocationOffsets.GetEnumerator();
            var enumRead = firstMultiPortRead.LocationOffsets.GetEnumerator();
            while (enumWritten.MoveNext())
            {
                Validate.IsTrue(enumRead.MoveNext(), "enumRead.MoveNext failed");
                Validate.IsTrue(PointComparer.Equal(enumWritten.Current, enumRead.Current), "LocationOffset differs");
            }
        }

        private RectilinearEdgeRouterWrapper CreateAndRouteTwoObstaclesWithFreeAndMultiLocPorts(int firstMultiOffsetIndex)
        {
            List<Shape> obstacles = CreateTwoTestSquaresWithSentinels();
            var a = obstacles[0]; // left square
            var b = obstacles[1]; // right square
            var abox = a.BoundingBox;
            var bbox = b.BoundingBox;

            // Create one freeport between the obstacles.
            var freePort1 = MakeAbsoluteFreePort(new Point(bbox.Left - 20, bbox.Center.Y));
            
            // Create MultiPorts (which are relative) for the obstacles.
            var portA = MakeMultiRelativeObstaclePort(a, OffsetsFromRect(abox));
            var portB = MakeMultiRelativeObstaclePort(b, OffsetsFromRect(bbox));

            // Force the first obstacle's active index to be the desired one.  We'll return its location
            // so the save/restore logic can be verified.
            var multiPort = (MultiLocationFloatingPort)portA;
            multiPort.SetClosestLocation(multiPort.CenterDelegate() + multiPort.LocationOffsets.ElementAt(firstMultiOffsetIndex));
            Validate.AreEqual(multiPort.ActiveOffsetIndex, firstMultiOffsetIndex, "Failure trying to set ActiveOffsetIndex");

            var routings = new List<EdgeGeometry>
                {
                    CreateRouting(portA, portB),
                    CreateRouting(portA, freePort1),
                    CreateRouting(portB, freePort1)
                };
            return DoRouting(obstacles, routings, null /*freePorts*/);
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify intersections in scanline with overlaps")]
        public void Overlap_Random_ScanLine_Intersections_Equal()
        {
            // Fixed:  Scanline firstIntersection != secondIntersection due to rounding hence sides ordered incorrectly in scanline.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify intersections in scanline with overlaps")]
        public void Overlap_Random_ScanLine_Intersections_Equal2()
        {
            // Fixed:  Some non-overlapped lines at top that should be overlapped - Curve.CloseIntersections in the scanline.
            // Also an example of needing to use the sideNode rather than the event.Site for flat bottoms, for consistent overlap.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify handling of ScanSegments with no visibility vertices with overlaps")]
        public void Overlap_RandRect_Remove_Empty_ScanSegments()
        {
            // Fixed:  One or more ScanSegments were fragments at the end of the obstacle (in this case, starting at the
            // bottom sentinel, ending at first obstacle, and chopped off by an overlapped segment inside the obstacle).
            // Empty ScanSegments are now removed from the VG.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify sides in scanline with overlaps")]
        public void Overlap_SideAlreadyInScanLine()
        {
            // Fixed:  Attempted duplicate insertion into scanline.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify scan segment reflections from overlapped obstacles are drained.")]
        public void Overlap_Reflection_ScanSegments_Drained()
        {
            // Fixed:  ScanSegment reflections from overlapped obstacles were not being drained.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify scan segment reflections from overlapped obstacles are drained.")]
        public void Overlap_Reflection_ScanSegments_Drained2()
        {
            // Fixed:  ScanSegment reflections from overlapped obstacles were not being drained.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify lookahead events with overlapped obstacles.")]
        public void Overlap_LookAhead_Event()
        {
            // Fixed:  Lookahead event was ahead of site but below LastAddedSegment.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify adjacent overlapped and non-overlapped segments are aligned")]
        public void Overlap_Adjacent_Segments()
        {
            // Fixed:  mismatched overlapped-ness with segments overlapping by more than start/end.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify adjacent overlapped and non-overlapped segments are aligned")]
        public void Overlap_Adjacent_Segments2()
        {
            // Fixed:  mismatched overlapped-ness with segments overlapping by more than start/end.
            // This one looks like it might have been due to reflections.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Verify adjacent overlapped and non-overlapped segments are aligned for only rectangular obstacles")]
        public void Overlap_RandRect_Adjacent_Segments()
        {
            // Fixed:  mismatched overlapped-ness with segments overlapping by more than start/end.
            // This one has only rectangular obstacles.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test a Port whose unpadded border extension extends internally past the end of an exterior scansegment")]
        public void Port_With_Interior_Extension_Past_Exterior_ScanSegment()
        {
            // Unpadded border intersect at 34,21; South extension passes end of external
            // horizontal ScanSegment at y==17 and should not create a VisibilityEdge from that ScanSegment.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Overlap test with different combinations of events")]
        public void Overlap_Combinations_Of_Overlap_Events()
        {
            // Exercises multiple aspects of overlaps:
            //  - Coincident intersection events; need to check for side already in scanline.
            //  - Cases that must check outer/inner edge overlaps for both low and high,
            //    or the both-outer overlap if there is no inner, for a flat top
            //  - Cases that require skipping to lowESTlowSideNode (or highest)
            //  - Also tests flat bottom side absorption of reflection and has an embedded
            //    non-flat side that catches it if we fail to absorb.
            //  - Fixed a rounding error that caused an overlap event cycle.
            // These are all on ScanSegment generation side so the file contains no VG or paths.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests ordering of sides in the scanline")]
        public void Overlap_ScanLine_Side_Ordering()
        {
            // Fixed:  Sides in scanline were not strictly increasing.
            // This is all on ScanSegment generation side so the file contains no VG or paths.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests ordering of sides in the scanline")]
        public void Overlap_ScanLine_Side_Ordering2()
        {
            // Fixed:  Side not found in scanline (due to ordering).
            // This is all on ScanSegment generation side so the file contains no VG or paths.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests ordering of sides in the scanline")]
        public void Overlap_ScanLine_Side_Ordering3()
        {
            // Fixed:  Side not found in scanline (due to ordering).
            // This is all on ScanSegment generation side so the file contains no VG or paths.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests ordering of sides in the scanline")]
        public void Overlap_ScanLine_Side_Ordering4()
        {
            // Fixed:  HighNeighbor is not LowObstacleSide (on Vertical scan).
            // This is all on ScanSegment generation side so the file contains no VG or paths.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests ordering of sides in the scanline")]
        public void Overlap_ScanLine_Side_Ordering5()
        {
            // Fixed:  Sides not strictly increasing in scanline; start is exactly on another side.
            //  VscanEvents, HighBendVertexEvent (20.355524 139.678927)
            // Force order by slope going forward if scanline intersections are the same.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests ordering of events that include reflections")]
        public void Rotate_Reflection_Ordering()
        {
            // Fixed:  Nullrefs because of bad reflection ordering in EventQueue
            //   (also had ScanSegments with no VisibilityVertex).
            // This is all on ScanSegment generation side so the file contains no VG or paths.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom")]
        public void Overlap_Rotate_AlmostFlatBottom()
        {
            // Fixed:  Scanline ordering problem due to almost-flat obstacle bottom.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom")]
        public void Overlap_Rotate_AlmostFlatBottom2()
        {
            // Fixed:  Scanline ordering problem due to almost-flat obstacle bottom.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom")]
        public void Overlap_Rotate_AlmostFlatBottom3()
        {
            // Fixed:  Scanline ordering problem due to almost-flat obstacle bottom.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom")]
        public void Overlap_Rotate_AlmostFlatBottom4()
        {
            // Fixed:  Scanline ordering problem due to almost-flat obstacle bottom.
            // This has two obstacles almost completely covering each other.
            // The vertical-sweep overlapped OpenVertexEvent demonstrates scanline inversion handling.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat bottom")]
        public void Overlap_Rounding_Vertex_Intersects_Side()
        {
            // Fixed:  Neighbor-intersection calculation rounding issue causes "off by one
            // DistanceEpsilon" mismatch with the scanline to Subsume an overlapped line
            // by a non-overlapped one.  The latter is correct so Subsume uses the non-overlapped.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of adjacent scan segments")]
        public void Overlap_Rotate_Adjacent_ScanSegments()
        {
            // Fixed:  Overlap-mismatched scansegments overlap by more than start/end
            // (in this case, an Overlapped ScanSegment coming up just before a Reflection ScanSegment). 
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of adjacent scan segments")]
        public void Overlap_Rotate_Adjacent_ScanSegments2()
        {
            // Fixed:  Overlap-mismatched scansegments overlap by more than start/end
            // (in this case, an Overlapped ScanSegment from a Close event coming up
            // just before the Segment put in by an Open event; Open events are evaluated first).
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test scanline handling of side-comparisons where the difference is small enough that swapping the operand positions would yield a different answer")]
        public void Overlap_NonCommutative_ScanLine_Comparison()
        {
            // Fixed:  Due to rounding, make sure that when two sides are compared in the scanline,
            // they are always compared from the same position, for consistency in cases where the
            // difference is small enough that the error of double-precision operations will yield
            // a different answer if their positions are swapped.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test fine-grained comparisons due to overlaps")]
        public void Overlap_Fine_Grained_Comparison()
        {
            // Fixed:  Impure dir found - GetTriangleOrientation isn't good for this fine a change.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test non-interior flat intersections")]
        public void Overlap_NonInterior_Flat_Intersection()
        {
            // Fixed:  Non-interior flat intersection should not yield a zero (equal) comparison result.
            // Problem was due to rounding error in Point.IntervalIntersectsInterval isInterior calculations
            // (this function is no longer used).
            // Fixed:  If the two-intersection approach is used this gets a scanline-ordering problem and 
            // cannot find a side it's trying to remove).
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test non-interior flat intersections")]
        public void Overlap_NonInterior_Flat_Intersection2()
        {
            // Fixed:  Do not use the Two-Intersection approach for internal nonflat intersections in scanline.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of an almost-flat top")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "This is the two-word usage.")]
        public void Overlap_Rotate_AlmostFlatTop()
        {
            // Fixed:  This is The "Original Almost-Flat Repro". The problem is that because the almost-flat 
            // topside says it's to the right of the side that passes through it, it never queues an overlap
            // event for the side that is actually below it.  There are a few variations of this.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test queuing of overlap events for obstacle sides")]
        public void Overlap_Rotate_SideOverlapEvents()
        {
            // Fixed:  Side-sequencing issue because MaybeGenOlapEvent was not ordering sides for intersection.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of ScanSegments that share an endpoint in overlap situations")]
        public void Overlap_With_Touching_ScanSegment_Endpoints()
        {
            // Fixed:  StaticGraphUtility::SegmentsOverlap was getting Dir.None on touching endpoints,
            // leading to a NullRef.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of a CloseVertexEvent on a slanting obstacle side")]
        public void Overlap_Rotate_CloseVertexEvent_On_Slanting_ObstacleSide()
        {
            // Fixed:  Non-ascending ScanSegment.  CloseVertexEvent is exactly on a slanting line that
            // led to AlmostFlat symptoms in conjunction with scanline inversion of highObstacleSide/LowObstacleSide
            // ordering at their CloseVertex intersection.  No longer repros with MergeSegments and the 
            // inversion-check fix in FindNeighbor.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test overlapped obstacles that lead to overlapping ScanSegments")]
        public void Overlap_Rotate_ScanSegment_Overlaps()
        {
            // Fixed:  Overlapping scansegment insertion into ScanSegments.  The fix is to allow this
            // and then fixup in MergeSegments.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test that reflections are drained by sides coming out of a flat top")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "This is the two-word usage.")]
        public void Overlap_Rotate_DrainReflectionsBySidesLeavingFlatTop()
        {
            // Fixed:  Reflections were not drained by sides coming out of a flat top.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Edge case for reflection segment start")]
        public void Overlap_Rotate_ReflectionSegmentStart()
        {
            // Fixed:  This may have been a bogus assert failure; the Reflection segment was reported as starting
            // before the PerpCoord of LastAddedSegment.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of reflections into dead-ends of overlapped obstacle sides")]
        public void Overlap_ReflectionsIntoDeadEnd()
        {
            // Fixed:  This has a LastAddedSegment ahead of the intersection which converted in a dead-end alley
            // of overlapped edges - tests LookaheadScan.RemoveStaleSites.
            // This also generates a skip due to another reflection event triggering the lowside "trying to steal"
            // a reflection on its high side.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of reflection events from a dead end at lower right")]
        public void Overlap_Rotate_ReflectionsOutOfLowRightDeadEnd()
        {
            // Exercises ProcessOverlapAdd adding reflection events from low right.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test consistency of rounding on both axes when encountering reflections")]
        public void Overlap_Rotate_Reflections_Rounding_On_Different_Axes()
        {
            // Fixed:  "IsOverlapped-mismatched segments differ by more than just start-end".  This was because the
            // intersect is on the Horizontal axis on the Vertical pass for the lookahead, but when the scanline catches
            // up to it it's intersected from the Vertical axis, so different rounding in the Vertical direction.
            // Fixed to have use ScanLineIntersectSide in downFlect calculation.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of ports with overlapped rectangles")]
        public void Overlap_RandRect_Ports()
        {
            // Fixed:  Assert failed "Obstacle encountered between prevPoint and startVertex".
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test handling of ports with rotated obstacles of all shapes")]
        public void Overlap_Rotate_Ports()
        {
            // Fixed:  Assert failed "Vertex already exists at this location" in ObstaclePortE.AddToAdjacentVertex;
            // tightened up directionality check.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Splice ports into rotated obstacles")]
        public void Random_Rotate_SplicePort()
        {
            // Fixed:  AssertFail "splitVertex is not on edge"; needed to add splitVertex to FindOrAddEdge.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Splice ports into rotated obstacles")]
        public void Random_Rotate_SplicePort2()
        {
            // Fixed:  AssertFails "splitVertex is not on edge", "obstacle encountered between prevPoint and startVertex"
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Splice ports into rotated obstacles")]
        public void Random_Rotate_SplicePort3()
        {
            // Fixed:  AssertFail "edgeIntersect should not be segsegVertex"
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Splice ports into rotated obstacles")]
        public void Random_Rotate_SplicePort4()
        {
            // Fixed:  AssertFail "edgeIntersect should not be segsegVertex"
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Splice ports into rotated obstacles")]
        public void Random_Rotate_SplicePort5()
        {
            // Fixed:  Impure direction in ScanSegTree.FindHighestIntersector: No collinear segments exist for this
            // point that equals ScanSeg.Start. so don't use IsOnSegment which does PureDirection.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(7000)]
        [Description("Splice ports into rotated obstacles")]
        public void Random_Rotate_SplicePort6()
        {
            // Fixed:  Inconsistent RestrictRayToObstacles with previous RestrictSeg approach
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
#if TEST_MSAGL
        [Timeout(12000)]
#else
        [Timeout(4000)]
#endif
        [Description("Splice ports into rotated obstacles")]
        public void Random_Rotate_SplicePort7()
        {
            // Fixed:  Inconsistent RestrictRayToObstacles with previous RestrictSeg approach
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Splice ports into rectangular obstacles")]
        public void RandRect_SplicePort()
        {
            // Fixed:  Inconsistent RestrictRayToObstacles with previous RestrictSeg approach
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("ScanSegments crossing the middle of an obstacle with intervening boundaries")]
        public void Overlap_PortVisibilityCrossesObstacle_FreeObstaclePorts()
        {
            // Fixed:  NullRef
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("ScanSegments crossing the middle of an obstacle with intervening boundaries")]
        public void Overlap_Rotate_PortVisibilityCrossesObstacle_FreeObstaclePorts()
        {
            // Fixed:  NullRef
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(15 * 1000)]
        [Description("Splice ports into rotated overlapped obstacles using freeports for obstacleports")]
        public void Overlap_Rotate_SplicePort_FreeObstaclePorts()
        {
            // Fixed:  Impure Direction on edgeIntersect == pointLocation in FindOrCreateNearestPerpEdgeFromNearestPerpSegment.
            // Add the pointLoc == edgeInt outside the "!edgeIntVertex exists" check.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Ignore]    // TODOperf: Currently takes too long
        [Timeout(90 * 1000)]
        [Description("Paths with rotated overlapped obstacles using freeports for obstacleports")]
        public void Overlap_Rotate_SplicePort_FreeObstaclePorts_GetPaths()
        {
            // Same as the test identified by the filename here, but setting WantPaths true.  Previously 
            // failed with a lot of overlaps unless StraightTolerance and/or CornerTolerance were set to about 1.0.
            // This file has no paths; if the time issue is fixed then re-create it with paths written to it
            // for verification (and just consolidate this test method with the one identified by the filename).
            base.OverrideWantPaths = true;
            this.RunRectFile(GetTestFileName("Overlap_Rotate_SplicePort_FreeObstaclePorts", FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("ScanSegments crossing the middle of an obstacle with intervening boundaries")]
        public void Overlap_PortVisibilityCrossesObstacle()
        {
            // Fixed:  NullRef
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("ScanSegments crossing the middle of an obstacle with intervening boundaries")]
        public void Overlap_Rotate_PortVisibilityCrossesObstacle()
        {
            // Fixed:  NullRef
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Verify nudger handling of many paths in a channel")]
        public void Nudger_Many_Paths_In_Channel()
        {
            // Fixed:  Nudger was nudging paths into obstacles.
            // Also, there was an AF in ComputeDfDv return value due to immediate cycles.
            // The file has no VG or paths; verification is by checking for paths into obstacles.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
#if TEST_MSAGL
        [Timeout(40 * 1000)]
#else 
        [Timeout(10 * 1000)]
#endif
        [Description("Verify nudger/solver handling of overlapped paths")]
        public void Nudger_Overlap_SingleConstraintVariable_InCyclePath()
        {
            // Fixed:  Nonzero ComputeDfDv final value due to single-constraint variable in cycle path.
            // This should not be an issue with the null-minLagrangian check now catching all cycles.
            base.OverrideWantVerify = false;    // Nudger nudges into at (13.2640670007 79.7512867344) narrow channel between obstacle and convexhull
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Ignore]    // TODOperf: Currently takes too long
        [Timeout(60 * 1000)]
        [Description("Test that has a lot of loops in SolverShell; currently takes about 10x the amount of time that comparables do")]
        public void Nudger_Overlap_MultipleSolverShellLoop()
        {
            // Problem:  After 30 or so iterations in SolverShell, it ends up with a couple
            // blocks in the solver that have a lot of variables with multiple candidate active-constraint
            // trees, so that when one set of active constraints is re-gapped, another set with the default
            // gap is created.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
#if TEST_MSAGL
        [Timeout(35 * 1000)]
#else
        [Timeout(5 * 1000)]
#endif
        [Description("Test of Nudger with an obstacle that close to another")]
        public void Nudger_Overlap_Rotate()
        {
            // Fixed: AssertFailure in ShortestCycleRemover.CleanDisappearedPiece followed by a NullRef in 
            // ShortestCycleRemover.GetPointsInBetween.  The file contains no paths; the test verifies
            // the NullRef does not occur (nor does the AssertFail).
            base.OverrideWantVerify = false;        // Nudger nudges into at (63.6642088641 6.8918875216) (cuts off the corner going around a triangle)
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(15 * 1000)]
        [Description("Test of Nudger with an obstacle that close to another")]
        public void Nudger_Overlap_Rotate_ClosePointRounding()
        {
            base.OverrideWantVerify = false;        // Nudger nudges into at  (57.7649665353 53.5083937741) (overlapped confusion)
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test of nudger with overlapped obstacles")]
        public void Nudger_Overlap()
        {
            // Fixed:  Nudger nudged into an obstacle.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Test of nudger with overlapped obstacles")]
        public void Nudger_Overlap2()
        {
            // Fixed:  Nudger nudges into an obstacle where the path bends.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(15 * 1000)]
        [Description("Test of nudger with overlapped obstacles")]
        public void Nudger_Overlap3()
        {
            // Fixed:  Nudger nudges into an obstacle where the path bends.
            base.OverrideWantVerify = false;    // Nudger nudges into at (61.0626191097 57.523175415) (flips a bend into an overlapped obstacle)
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(15 * 1000)]
        [Description("Test of nudger with overlapped obstacles")]
        public void Nudger_Overlap4()
        {
            // Fixed:  Nudger nudges into an obstacle where the path bends (this test contains groups but the problem repro'd without them).
            base.OverrideWantVerify = false;            // Nudger nudges into at (66.7129846184 15.1049512188)
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Handle a case where a portion of the graph is heavily overlapped and disconnected")]
        public void Nudger_Overlap_Rotate_FreeObstaclePorts_Disconnected()
        {
            // Fixed:  A portion of the graph is heavily overlapped and the interior scansegments are
            // not fully connected.  Fix: If no path, just force a one-bend path between them.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Handle a case where a portion of the graph is heavily overlapped and disconnected")]
        public void Nudger_Overlap_Rotate_FreeObstaclePorts_Disconnected2()
        {
            // Problem:  A portion of the graph is heavily overlapped and the interior scansegments are
            // not fully connected.  Fix: If no path, just force a one-bend path between them.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Ignore]    // TODOperf: Currently takes too long
#if TEST_MSAGL
        [Timeout(45 * 1000)]
#else
        [Timeout(15 * 1000)]
#endif
        [Description("Paths where multiple high-weight fixed variables push each other around")]
        public void Nudger_Sumo()
        {
            // Fixed:  Paths where multiple high-weight fixed variables push each other around were not converging.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(15 * 1000)]
        [Description("Paths where multiple high-weight fixed variables push each other around")]
        public void Nudger_Sumo2()
        {
            // Fixed:  Paths where multiple high-weight fixed variables push each other around were not converging.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Groups that are on adjacent, DistanceEpsilon-separated vertices can go both forward (from the lower vertex) and backward (from the higher).")]
        public void Groups_Forward_Backward_Between_Same_Vertices()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Groups (that may overlap) across obstacles that do not overlap")]
        public void Random_Groups_NoOverlappingObstacles()
        {
            // Fixed:  Was taking 30s (vs. 7 now).  Removal of non-spatial ancestors.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Groups (that may overlap) across obstacles that do not overlap")]
        public void Random_Groups_NoOverlappingObstacles_PortSplice_LimitRect()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Groups (that may overlap) across obstacles that do not overlap")]
        public void Random_Groups_NoOverlappingObstacles2()
        {
            // In addition to being a basic Group test, this is a testcode fix:  was checking 
            // for port location inside group, added test for source/target obstacle overlaps.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Groups (that may overlap) across obstacles that do not overlap")]
        public void Random_Groups_NoOverlappingObstacles2_PortSplice_LimitRect()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Groups (that may overlap) across obstacles that overlap")]
        public void Random_Groups_OverlappingObstacles()
        {
            // Fixed:  Fixed-variable loop (sumo).
            // This file is currently -noWritePaths because tiny differences between original generation of obstacles
            // vs. reading definitions from the file result in slightly different nudging.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Routing across a road-blocking cluster inside a group")]
        public void Random_Groups_Roadblock_Cluster_Inside_Group()
        {
            // Fixed:  Allow routing through overlapping obstacles in this case.
            base.OverrideWantVerify = false;        // Nudger nudges into at (16.2353551 36.069167)
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Groups with a routed obstacle outside a group")]
        public void Random_Groups_Obstacle_Outside_Group()
        {
            // Fixed:  Was taking 30s (vs. 7 now).  Removal of non-spatial ancestors.
            // This file is currently -noWritePaths because tiny differences between original generation of obstacles
            // vs. reading definitions from the file result in slightly different nudging.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Groups with a routed obstacle outside a group")]
        public void Random_Groups_Obstacle_Outside_Group_PortSplice_LimitRect()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test testcode handling of source or target obstacle landlocked inside its group by a clump of obstacles")]
        public void TestCode_Obstacle_Landlock()
        {
            // Fixed:  Clumps were not joined together correctly.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test testcode handling of source or target obstacle landlocked inside its group by a clump containing obstacles and groups")]
        public void TestCode_ObstacleAndGroup_Landlock()
        {
            // Fixed:  Clumps were not joined together correctly.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test testcode handling of clump joining")]
        public void TestCode_Clump_Joining()
        {
            // Fixed:  Clumps were not joined together correctly.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Test testcode handling of groups that overlap source and/or target obstacle")]
        public void TestCode_CrossedGroup_Overlaps_SourceOrTarget()
        {
            // Fixed:  Clumps were not joined together correctly.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests selection of path among overlapped alternatives with port visibility")]
        public void Overlapped_Path_Selection()
        {
            // Fixed:  The path here had an extra cutback because the overlapped port visibility
            // edges were not weighted; hence the lowest-weight path was longer.  The cost of a path is
            // determined by the weighted length (*not* number of edges) and the number of turns so
            // preserving the overlapped weight would leads to a shorter and more correct path.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests selection of path among overlapped alternatives with port visibility")]
        public void Overlapped_Path_Selection2()
        {
            // Fixed:  The third path here was short-circuiting, going from the second-down obstacle
            // in the left column through the third-down obstacle and ending at the fourth-down obstacle.
            // Now it selects the side path, because the overlapped weights are preserved on visibility
            // splicing of overlapped ports; the longer path is desired to avoid unnecessarily crossing
            // the intervening obstacle, even though it partially overlaps the endpoint obstacle, because
            // there is a non-obstacle-crossing path between the two obstacles.
            // Also: the interior edge to the North to repro this problem was not spliced in; this was
            // caused by not being able to find a nextSpliceSource (N) on the immediate-W vertex, and
            // TransientGraphUtility.ExtendSpliceWorker was modified to move out W from spliceSource
            // until it could find a N edge or runs out of W edges.
            this.RunGeomFile(this.GetGeomGraphFileName("SimpleOverlappedObstacle.msagl.geom"));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests visibility splice of a port that is collinear with a group border")]
        public void Splice_Along_Group_Border()
        {
            // Fixed:  This was leaving TransientVisibilityEdges in the graph because vertices already
            // existed due to horizontal visibility crossing the vertical group boundary; we spliced
            // vertically along that boundary so did not create the vertices.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests selection of path among overlapped alternatives with port visibility")]
        public void Reflection_FarSide()
        {
            // The ObstacleSide at {88.232, 66.25727} -> {93.73412 71.75939} was not propagating reflections.
            // This shows that on obstacle close we can't optimize the Reflection-loading range by the near
            // obstacle side because the neighbor may span the close vertex (or flat top) and include ranges
            // across the far side of the closing obstacle, which may have reflected upward.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Tests that a group reflects when it reflects upward and is before a neighbor that reflects downward")]
        public void Reflection_InterveningGroup_Between_Site_And_Downward_Neighbor()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        [Description("Tests selection of path among overlapped alternatives with port visibility")]
        public void Reflection_FarSide2()
        {
            base.OverrideWantVerify = false;    // Nudger nudges into group at (21.4582723076 48.677179) (flips bend into group)
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify that visibility is restricted when a neighbor is within intersection calculations")]
        public void RestrictVisibility_WithCloseNeighbor()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify validation allows crossing of non-spatial ancestors when subsequent paths cause those to be removed form AncestorSets.")]
        public void TestCode_Groups_NonSpatial_Ancestor_Crossed_Before_AdjustSpatialAncestors()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Abp_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Abp_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Abp_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Abp_Dot_RouteToCenter()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Abstract_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Abstract_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Abstract_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Abstract_Dot_RouteToCenter()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Jsort_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Jsort_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Jsort_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Jsort_Dot_RouteToCenter()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Mike_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Mike_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Mike_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Mike_Dot_RouteToCenter()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void FsmNoLabels_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void FsmNoLabels_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void FsmNoLabels_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void FsmNoLabels_Dot_RouteToCenter()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Lovett_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Lovett_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Lovett_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Lovett_Dot_RouteToCenter()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void TrapeziumLR_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void TrapeziumLR_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void TrapeziumLR_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(4 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void TrapeziumLR_Dot_RouteToCenter()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void World_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void World_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void World_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(6 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Stuff2_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(6 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Stuff2_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(6 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Stuff2_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(6 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Stuff2_Dot_RouteToCenter()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(6 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void NaN_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(6 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void NaN_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(6 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void NaN_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void QiangDai_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void QiangDai_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void QiangDai_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void B124_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void B124_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void B124_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Smlred_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Smlred_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Smlred_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Channel_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Channel_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void Channel_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void B69_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void B69_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        public void B69_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Dirs_Dot()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Dirs_Dot_HighBendPenalty()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(12 * 1000)]
        [Description("Verify paths when routing obstacles from given .dot file.")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "This refers to a filename.")]
        public void Dirs_Dot_SparseVg()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("When splicing port visibility into a group from outside it, do not join to reflection segments as they do not have GroupCrossing logic.")]
        public void DoNotSpliceToExternalReflectionEdgeWhenCrossingGroup()
        {
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("When splicing port visibility, stop when we find an existing vertex we want to extend to and there is no target vertex in that direction.")]
        public void StopSplicingAtExistingExtendVertexWithNullTargetVertex()
        {
            // spliceTarget is null at:
	        //  nextSpliceSource (76.923345 45.302176)
            //  nextExtendPoint (76.923345 37.921772)
            // Continuing to splice would cross the intervening obstacle.
            this.RunRectFile(GetCurrentMethodDotTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Ensure that group crossing interior vertices that are at ScanSegment intersections are included in the intersection vertices.")]
        public void AddSparseIntersectionForGroupCrossingInteriorVertex()
        {
            // The vertex at (16.020844 32.847449) is created as the interior vertex of a group crossing, which happens to be at the intersection
            // of two scansegments, but that intersection is not added by normal sparse intersection logic; so we add that vertex specifically.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Nudger was nudging a bend into an angled rectangle.")]
        public void NudgingIntoAngledRectangle()
        {
            // Fixed: path section [(17.8117662651 4.861822) -> (22.5598845003 9.8869778083)] crosses obstacle
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("Splicing was using a Normal rather than Overlapped weight.")]
        public void SparseVgCreatingVertexAtOverlappedSegmentPreservesWeight()
        {
            // Fixed: Crossed an obstacle from [(41, 2.34)] Eastbound because it was creating the segsegVertex
            // and wasn't using the overlapped intSegBefore's weight.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(3000)]
        [Description("A point that is CloseIntersections after the current segment should extend it.")]
        public void SparseVgCloseIntersectionAfterCurrentSegmentShouldExtendIt()
        {
            // Fixed: Point at (89.253807 116.51715) is CloseIntersections after CurrentSegment.End and much further than
            // that from NextSegment.Start, thus should make CurrentSegment.End up rather than NextSegment.Start down.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("A series of waypoints that do not have backtracks.")]
        public void Waypoints_NoBacktrack1()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("A series of waypoints that do not have backtracks.")]
        public void Waypoints_NoBacktrack2()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("A series of waypoints that have backtracks.")]
        public void Waypoints_Backtrack1()
        {
            // Nudger introduces out-and-back segments.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("A series of waypoints that have backtracks.")]
        public void Waypoints_Backtrack2()
        {
            // Nudger introduces out-and-back segments.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("Two waypoints are out of bounds vertically and oriented such that the path loops through them on the way to the target.")]
        public void Waypoints_OobVerticalLoop()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("Four waypoints are out of bounds horizontally and oriented such that the path loops through them on the way to the target.")]
        public void Waypoints_OobHorizontalLoop()
        {
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(1000)]
        [Description("A waypoint that is out of bounds both horizontally and vertically.")]
        public void Waypoints_OobCorner()
        {
            // Error invalid direction oobWaypoint = {(3.372285 38.543351)} - waypoint oob in two dirs, so do separate
            // attachments for horizontal and vertical.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("A reflection and extreme-vertex-derived segment are close enough that they converge during the reflection staircase.")]
        public void ReflectionStaircasesConverge() 
        {
            // Reflection sites at (27.090006 66.027992) and (27.090008 65.85906) are close enough that the bounce
            // for the second is at the same Y coordinate as that of the first, resulting in subsumption.  This
            // closeness results from the angle of the Left obstacle side being quite shallow so the initial
            // reflection segment is parallel and very close to the vertex-derived segment (and the reflecting
            // side of the obstacle above it is at a fairly shallow angle as well; either or both of these are
            // sufficient to display the symptoms, as is a reflection off 45-degree obstacle side at a point
            // just inside the bounding box). (The obstacles in this test are actually convex hulls.)
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }

        [TestMethod]
        [Timeout(2000)]
        [Description("A reflection segment is subsumed by a scansegment that leaves the group.")]
        public void ReflectionSubsumedBySegmentExitingGroup() 
        {
            // At (8.856536 84.716026) we have a scansegment exiting a group at the same scanline coordinate as
            // a reflection segment.
            this.RunRectFile(GetCurrentMethodTestFileName(FileAccess.Read));
        }
    }
}
