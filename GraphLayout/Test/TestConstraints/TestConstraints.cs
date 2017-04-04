//-----------------------------------------------------------------------
// <copyright file="TestConstraints.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.UnitTests;
using Microsoft.Msagl.UnitTests.Constraints;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestConstraints
{
    class TestConstraints : MsaglTestBase
    {
        // Common static members.
        private uint totalTestsRun;
        internal static readonly List<String> ListOfFailedTestAndFileNames = new List<string>();
        private string errorLog;
        private bool quietOutput;

        private readonly List<string> testFileNames = new List<string>();
        private readonly List<Tuple<string, MethodInfo>> testMethodInfos = new List<Tuple<string, MethodInfo>>();
        private readonly List<string> runallSpecs = new List<string>();
        private readonly List<string> showallSpecs = new List<string>();
        private bool ignoredTestsOk;
        private bool ignoredTestsOnly;
        
        // Shared between ProjectionSolverTester and SolverFoundationTester.
        internal static double ProcessFilesFakeScale;
        private const double DefaultWeightAndScale = 1000.0;
        
        #region CreateFile variables

        internal static double MaxWeightToGenerate;         // Set >0 in arg list to generate random weights in that range
        internal static int MaxNodePosition = 10;           // Set in arg list for larger coordinate range

        #endregion // CreateFile variables

        // For reproducibility and precision management of randomly-generated numbers.
        // We output to files with {N:F5} so keep that precision level constant.
        internal const int FileOutputDigitsOfPrecision = 5; // Not customizable
        internal static int RandomSeed;                // Set in arg list if reproducibility desired
        internal static double RoundRand(Random rng, int iMax)
        {
            // Returns 0.0 if iMax == 0
            double dblRand = rng.NextDouble() * iMax;
            return Math.Round(dblRand, FileOutputDigitsOfPrecision);
        }
        internal static double RoundRand(Random rng, double dblMax)
        {
            // Returns 0.0 if dblMax == 0
            double dblRand = rng.NextDouble() * dblMax;
            return Math.Round(dblRand, FileOutputDigitsOfPrecision);
        }
        internal static Random NewRng()
        {
            if (0 == RandomSeed)
            {
                // Get a known seed value so we can print it to a file.  This will get the low
                // (most changing) int32 from the int64.
                RandomSeed = (int)DateTime.Now.Ticks;
            }
            return new Random(RandomSeed);
        }

        // This is the object implementing the tester interface.
        private ITestConstraints testerInstance;

        //
        // When adding a new arg:
        //  1. Add to appropriate Show*Usage.
        //  2. Add to CheckArgForTest.
        //  3. Add to the actual argument processing.
        // The same locations, of course, must be changed when modifying arguments.
        //
        static void ShowUsage()
        {
            Console.WriteLine();
            Console.WriteLine("TestConstraints Options (case-insensitive):");
            Console.WriteLine("  <testtype>: required first argument; the Test type to run; one of:");
            Console.WriteLine("    Proj | ProjectionSolver");
            Console.WriteLine("    Olap | OverlapRemoval");
            Console.WriteLine("    SF | SolverFoundation");
            Console.WriteLine("  -Perf N: Run N loops to test performance");
            Console.WriteLine("  -Recursive: Recurse into subdirectories for filespec");
            Console.WriteLine("  -ReGap N:  Before Solve(), make the gap in every N constraints different; if -ReSolve, then restore");
            Console.WriteLine("     those constraints to the original value, re-Solve(), and verify against the expected results.");
            Console.WriteLine("  -ReSolve:  Rerun Solve(), usually after ReGap.");
            Console.WriteLine("  -CreateFile: Create a test file; use args '/? -CreateFile' for more info");
            Console.WriteLine("  -RecreateFile oldfile newfile: Load oldfile and output results to newfile");
            Console.WriteLine("  -UpdateFile oldfile newfile: Load oldfile and update it in place with output results");
            Console.WriteLine("  -DumpRects: Print rectangles of clusters/nodes as 'left top right bottom'");
            Console.WriteLine("  -ShowRects: Display rectangles of clusters/nodes");
            Console.WriteLine("  -ShowInitial: Display rectangles of clusters/nodes");
            Console.WriteLine();
            Console.WriteLine("  -quiet:                    Do not display any dialogs or filenames; useful for batched perf runs.");
            Console.WriteLine("  -ignoredOk:                Include tests with or without [Ignore] in the run (for debugging).");
            Console.WriteLine("  -ignoredOnly:              Include only tests marked with [Ignore] in the run (for debugging).");
            Console.WriteLine("  -errorLog filename:        Log errors to this file; delete previous contents.");
            Console.WriteLine("  -appendErrorLog filename:  Log errors to this file; preserve previous contents (for batch runs).");
            Console.WriteLine("  -interactive:              Run interactively; put up dialogs for Assert failures.");
            Console.WriteLine();
            Console.WriteLine("  -showall [regex]:          Shows all no-parameter test names. Optional filter regex may be just a prefix.");
            Console.WriteLine("  -runall [regex]:           Runs all no-parameter tests. Optional filter regex may be just a prefix.");
            Console.WriteLine("  -file <file name>:         Load a Test datafile and run it.");
            Console.WriteLine();
            Console.WriteLine("  Solver.Parameters:");
            Console.WriteLine("    -GapTol D: (GapTolerance) Allowance for gap violation.");
            Console.WriteLine("    -QpscEps D: (QpscConvergenceEpsilon) Absolute difference in QPSC function value");
            Console.WriteLine("    -QpscQuo D: (QpscConvergenceQuotient) Relative difference in QPSC function value");
            Console.WriteLine("    -QpscNoScale: (ScaleInQpsc) Turns off scaling in QPSC");
            Console.WriteLine("    -OuterIterLimit N: (OuterProjectIterationsLimit) Outer Project/Split loop");
            Console.WriteLine("    -InnerIterLimit N: (InnerProjectIterationsLimit) Inner Project loop");
            Console.WriteLine("    -TimeLimit N: (TimeLimit) Timeout value in milliseconds");
            Console.WriteLine("    -QPSC: (ForceQPSC) Force ProjectionSolver to use QPSC even with no neighbour pairs");
            Console.WriteLine("  Advanced Parameters: ");
            Console.WriteLine("    -MinSplitLag D: (MinSplitLagrangianThreshold) Split blocks if Lagrangian multiplier is less than this");
            Console.WriteLine("    -CacheVioBlocks N N: (CacheVioMinBlocksDivisor CacheBlocksMinVioMin) Determines the minimum number");
            Console.WriteLine("      of blocks to enable the violation cache.  Passing these as -1 deactivates WantCacheVio.");
            Console.WriteLine();
            Console.WriteLine("  OverlapRemoval.Parameters:");
            Console.WriteLine("    -NoDeferToV: (AllowDeferToVertical) Do not allow deferral of constraints from H to V");
            Console.WriteLine("      (which may result in more H than V movement).  Default: Allow this.");
            Console.WriteLine();
            Console.WriteLine("  -Verbose [N]: Show actual results and a few other things; build with VERBOSE for more");
            Console.WriteLine("    detailed output (from within ProjectionSolver or OverlapRemoval).  If specified, N is");
            Console.WriteLine("    the level of detail.");
            Console.WriteLine();
            Console.WriteLine("  The following options identify the test(s) to be run and may be repeated in any sequence:");
            Console.WriteLine("    -Test<regex>:  Runs all matching static Test routines, e.g. Test2, Test.*Eq.*");
            Console.WriteLine("    <testname>: Runs the single static Test routine named");
            Console.WriteLine("    <filespec>: Runs all matching test files");
            Console.WriteLine();
        } // end ShowUsage()

        static void ShowCreateFileUsage()
        {
            Console.WriteLine();
            Console.WriteLine("TestConstraints <testtype> -CreateFile <options> <filename>.  Options (case-insensitive):");
            Console.WriteLine("  N: Required first argument - number of variables to create");
            Console.WriteLine("  N: Required second argument - number of constraints to create");
            Console.WriteLine("  PosMax N:  Maximum initial variable coordinate; default is {0}", MaxNodePosition);
            Console.WriteLine("  Weight def|D: Randomly assign weights up to D; default is no weights; def is {0}", DefaultWeightAndScale);
            Console.WriteLine("  Seed [0x]N: Seed for the random sizes, counts, etc.; hex if preceded by 0x.");
            Console.WriteLine("  ToFailure: Continuously re-create the file to the specified output filename, until failure.");
            Console.WriteLine("    The file remains for investigation.  Do not use the Seed option because that will re-create");
            Console.WriteLine("    the same file.");
            Console.WriteLine();
            Console.WriteLine("  The following options are for ProjectionSolver only:");
            Console.WriteLine("    Scale def|D: Randomly assign scales up to D; default is no scales; def is {0}", DefaultWeightAndScale);
            Console.WriteLine("    FakeScale D: When reading from a file, force this to be the scale for all variables.");
            Console.WriteLine("    N(eigh)bours N: Average number of neighbours per variable (Proj only); default is 0");
            Console.WriteLine("    GapMax N: Maximum constraint gap; default is {0}", ProjectionSolverTester.MaxGapToGenerate);
            Console.WriteLine("    StartAtZero: Start all randomly-generated variables at position 0.0");
            Console.WriteLine("    Eq: Generate equality constraints (currently randomly at 25% rate)");
            Console.WriteLine("  -CreateCycles: Create cycles in constraints");
            Console.WriteLine();
            Console.WriteLine("  The following options are for OverlapRemoval only:");
            Console.WriteLine("    SizeMax N: Maximum variables size (Olap only); default is {0}", OverlapRemovalTester.MaxSize);
            Console.WriteLine("    Padding Dx Dy: Randomly assign padding up to Dx or Dy; default is none");
            Console.WriteLine("    MinClusterSize Dx Dy: Randomly assign minimum sizes up to Dx or Dy; default is none");
            Console.WriteLine("    Clusters: Randomly create clusters (Olap only).  Options:");
            Console.WriteLine("      def: Create numvariables / 10 clusters.");
            Console.WriteLine("      rand def|N: Create random number of clusters up to either numvariables/10 or N");
            Console.WriteLine("      singleRoot: Follows either of the foregoing; creates only one root cluster hierarchy");
            Console.WriteLine("    Margins N: Create margins for clusters, with random width up to N");
            Console.WriteLine("    Fixed [one] rand|LRTB: Generate clusters with fixed borders.");
            Console.WriteLine("      one: if specified, only one such cluster is created");
            Console.WriteLine("      rand: randomly determine which sides are fixed");
            Console.WriteLine("      LRTB: any permutation of these letters is allowed, for Left/Right/Top/Bottom sides");
            Console.WriteLine();
            Console.WriteLine("  The final argument must be:");
            Console.WriteLine("    <filename>: Output filename; must be last argument");
            Console.WriteLine();
        } // end ShowCreateFileUsage()

        void CheckArgForTest(string strArg)
        {
            // Currently, just print a warning message if it's not applicable to this test type.
            if (testerInstance is ProjectionSolverTester)
            {
                if ((0 == string.Compare("SizeMax", strArg, true /*ignorecase*/))
                    || (0 == string.Compare("Padding", strArg, true /* ignoreCase */))
                    || (0 == string.Compare("MinClusterSize", strArg, true /* ignoreCase */))
                    || (0 == string.Compare("Clusters", strArg, true /* ignoreCase */))
                    || (0 == string.Compare("Margins", strArg, true /* ignoreCase */))
                    || (0 == string.Compare("Fixed", strArg, true /* ignoreCase */))
                    )
                {
                    Console.WriteLine("  Arg '{0}' ignored for ProjectionSolver", strArg);
                }
            }
            else if (testerInstance is OverlapRemovalTester)
            {
                if (((0 == string.Compare("neighbours", strArg, true)) || (0 == string.Compare("nbours", strArg, true)))
                    || (0 == string.Compare("GapMax", strArg, true /* ignoreCase */))
                    || (0 == string.Compare("StartAtZero", strArg, true /* ignoreCase */))
                    || (0 == string.Compare("Eq", strArg, true /* ignoreCase */))
                    )
                {
                    Console.WriteLine("  Arg '{0}' ignored for OverlapRemoval", strArg);
                }
            }
            else if (testerInstance is SolverFoundationTester)
            {
                // This supports nothing except testnames or filenames.
                Console.WriteLine("  Arg '{0}' ignored for SolverFoundation", strArg);
            }
            else
            {
                throw new InvalidOperationException("What is the ITester implementation?!");
            }
        }

        static int Main(string[] args)
        {
#if TEST_MSAGL
            Microsoft.Msagl.GraphViewerGdi.DisplayGeometryGraph.SetShowFunctions();
#endif
            return (new TestConstraints()).Run(args);
        }

        int Run(string[] args)
        {
            // Initialize to stricter termination accuracy.
            TestGlobals.InitialSolverParameters.QpscConvergenceQuotient = 1e-15;
            bool recursiveFiles = false;

            try
            {
                if (args.Length > 0)
                {
                    // For right now just assume args are the names of specific tests to run.
                    for (int ii = 0; ii < args.Length; ++ii)
                    {
                        String strTestName = args[ii];

                        // Check for usage message.
                        if (("-?" == strTestName) || ("/?" == strTestName))
                        {
                            // If the second argument is a known command, display usage for it.
                            if (ii < args.Length - 1)
                            {
                                strTestName = args[ii + 1];
                                if (0 == string.Compare("-CreateFile", strTestName, true /* ignoreCase */))
                                {
                                    ShowCreateFileUsage();
                                    return 0;
                                }
                                // Not a known command.  Fall through to top-level help.
                                Console.WriteLine("Unknown option: {0}", strTestName);
                            }
                            ShowUsage();
                            return 0;
                        }

                        // Require the first arg to identify the test.
                        if (null == testerInstance)
                        {
                            if ((0 == string.Compare("ProjectionSolver", strTestName, true /* fIgnoreCase */))
                                || (0 == string.Compare("Proj", strTestName, true /* fIgnoreCase */))
                                )
                            {
                                testerInstance = new ProjectionSolverTester();
                                continue;
                            }
                            if ((0 == string.Compare("OverlapRemoval", strTestName, true /* fIgnoreCase */))
                                || (0 == string.Compare("Olap", strTestName, true /* fIgnoreCase */)))
                            {
                                testerInstance = new OverlapRemovalTester();
                                TestGlobals.IsTwoDimensional = true;
                                continue;
                            }
                            if ((0 == string.Compare("SolverFoundation", strTestName, true /* fIgnoreCase */))
                                || (0 == string.Compare("SF", strTestName, true /* fIgnoreCase */)))
                            {
                                testerInstance = new SolverFoundationTester();
                                continue;
                            }
                            throw new ApplicationException("The first arg to TestConstraints must identify the Tester");
                        }

                        // Special stuff first.
                        if (0 == string.Compare("-TestStartAtZero", strTestName, true))
                        {
                            if (args.Length <= (ii + 2))
                            {
                                throw new ApplicationException(string.Format("'{0}' requires numVarsPerBlock and numBlocks", strTestName));
                            }
                            var numVarsPerBlock = int.Parse(args[++ii]);
                            var numBlocks = int.Parse(args[++ii]);
                            var projTester = this.testerInstance as ProjectionSolverTester;
                            if (projTester == null) 
                            {
                                throw new ApplicationException(string.Format("'{0}' requires a ProjectionSolver tester", strTestName));
                            }
                            projTester.StartAtZeroWorker(numVarsPerBlock, numBlocks);
                            break;
                        }

                        if (GetArg("-FakeScale", args, ref ii, ref ProcessFilesFakeScale))
                        {
                            continue;
                        }

                        // Common stuff
                        if (0 == string.Compare("-Perf", strTestName, true /* fIgnoreCase */))
                        {
                            if (args.Length < (ii + 2))
                            {
                                throw new ApplicationException("Perf requires a count argument");
                            }
                            TestGlobals.TestReps = uint.Parse(args[++ii]);
                            Console.WriteLine("Perf testing: {0} reps", TestGlobals.TestReps);
                            continue;
                        }
                        if (0 == string.Compare("-Recursive", strTestName, true /* ignoreCase */))
                        {
                            recursiveFiles = true;
                            continue;
                        }

                        int temp = 0;   // Can't pass autoprop as ref
                        if (GetArg("-ReGap", args, ref ii, ref temp))
                        {
                            ProjectionSolverVerifier.ReGapInterval = temp;
                            continue;
                        }
                        if (0 == string.Compare("-ReSolve", strTestName, true /* ignoreCase */))
                        {
                            ProjectionSolverVerifier.RestoreGapsAndReSolve = true;
                            continue;
                        }
                        if (0 == string.Compare("-DumpRects", strTestName, true /* ignoreCase */))
                        {
                            ResultVerifierBase.DumpRectCoordinates = true;
                            continue;
                        }
                        if (0 == string.Compare("-ShowRects", strTestName, true /* ignoreCase */))
                        {
                            ResultVerifierBase.ShowRects = true;
                            continue;
                        }
                        if (0 == string.Compare("-ShowInitial", strTestName, true /* ignoreCase */))
                        {
                            ResultVerifierBase.ShowInitialRects = true;
                            continue;
                        }
                        if (0 == string.Compare("-Verbose", strTestName, true /* ignoreCase */))
                        {
                            if (ii < (args.Length - 1))
                            {
                                if (int.TryParse(args[ii + 1], out TestGlobals.VerboseLevel))
                                {
                                    ++ii;
                                }
                                else
                                {
                                    TestGlobals.VerboseLevel = 1;
                                }
                            }
                            continue;
                        }

                        // Solver Parameters.
                        if (GetArg("-GapTol", args, ref ii, x => TestGlobals.InitialSolverParameters.GapTolerance = double.Parse(x)))
                        {
                            continue;
                        }
                        if (GetArg("-QpscEps", args, ref ii, x => TestGlobals.InitialSolverParameters.QpscConvergenceEpsilon = double.Parse(x)))
                        {
                            continue;
                        }
                        if (GetArg("-QpscQuo", args, ref ii, x => TestGlobals.InitialSolverParameters.QpscConvergenceQuotient = double.Parse(x)))
                        {
                            continue;
                        }
                        if (GetArg("-OuterIterLimit", args, ref ii, x => TestGlobals.InitialSolverParameters.OuterProjectIterationsLimit = int.Parse(x)))
                        {
                            continue;
                        }
                        if (GetArg("-InnerIterLimit", args, ref ii, x => TestGlobals.InitialSolverParameters.InnerProjectIterationsLimit = int.Parse(x)))
                        {
                            continue;
                        }
                        if (GetArg("-TimeLimit", args, ref ii, x => TestGlobals.InitialSolverParameters.TimeLimit = int.Parse(x)))
                        {
                            continue;
                        }

                        if (0 == string.Compare("-QPSC", strTestName, true /* ignoreCase */))
                        {
                            TestGlobals.InitialSolverParameters.Advanced.ForceQpsc = true;
                            ResultVerifierBase.ForceQpsc = true;
                            continue;
                        }
                        if (0 == string.Compare("-QPSCNoScale", strTestName, true /* ignoreCase */))
                        {
                            TestGlobals.InitialSolverParameters.Advanced.ScaleInQpsc = false;
                            continue;
                        }

                        // Advanced Solver Parameters.
                        if (GetArg("-MinSplitLag", args, ref ii, x => TestGlobals.InitialSolverParameters.Advanced.MinSplitLagrangianThreshold = double.Parse(x)))
                        {
                            continue;
                        }
                        if (0 == string.Compare("-CacheVioBlocks", strTestName, true /*ignoreCase*/))
                        {
                            if (args.Length <= (ii + 2))
                            {
                                throw new ApplicationException(string.Format("Missing values for '{0}' arg", strTestName));
                            }
                            int cacheViolationMinBlocksDivisor = int.Parse(args[++ii]);
                            int cacheViolationMinBlocksMin = int.Parse(args[++ii]);
                            if ((-1 == cacheViolationMinBlocksDivisor) || (-1 == cacheViolationMinBlocksMin))
                            {
                                TestGlobals.InitialSolverParameters.Advanced.UseViolationCache = false;
                            }
                            else
                            {
                                TestGlobals.InitialSolverParameters.Advanced.ViolationCacheMinBlocksDivisor = cacheViolationMinBlocksDivisor;
                                TestGlobals.InitialSolverParameters.Advanced.ViolationCacheMinBlocksCount = cacheViolationMinBlocksMin;
                            }
                            continue;
                        }

                        // OverlapRemoval parameters.
                        if (0 == string.Compare("-NoDeferToV", strTestName, true /* ignoreCase */))
                        {
                            OverlapRemovalVerifier.InitialAllowDeferToVertical = false;
                            continue;
                        }

                        if ((0 == string.Compare("-errorLog", strTestName, true /*ignorecase*/))
                                || (0 == string.Compare("-appendErrorLog", strTestName, true /*ignorecase*/)))
                        {
                            ++ii;
                            if (ii >= args.Length)
                            {      
                                // require one value
                                throw new ApplicationException("Missing filename for -" + strTestName);
                            }
                            errorLog = args[ii];
                            try
                            {
                                if (0 != string.Compare("-appendErrorLog", strTestName, true /*ignorecase*/))
                                {
                                    File.Delete(errorLog);
                                }
                                else
                                {
                                    FileStream fs = File.Open(errorLog, FileMode.OpenOrCreate, FileAccess.Write);
                                    var len = fs.Length;
                                    fs.Close();
                                    if (0 == len)
                                    {
                                        File.Delete(errorLog);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error removing existing '{0}': {1}", errorLog, ex.Message);
                            }
                            continue;
                        }

                        if (0 == string.Compare("-quiet", strTestName, true /* ignoreCase */))
                        {
                            quietOutput = true;
                            continue;
                        }
                        if (0 == string.Compare("-ignoredOk", strTestName, true /*ignorecase*/))
                        {
                            ignoredTestsOk = true;
                            continue;
                        }
                        if (0 == string.Compare("-ignoredOnly", strTestName, true /*ignorecase*/))
                        {
                            ignoredTestsOnly = true;
                            continue;
                        }
                        if (0 == string.Compare("-interactive", strTestName, true /*ignorecase*/))
                        {
                            Validate.InteractiveMode = true;
                            continue;
                        }

                        // Create a test file.
                        if (0 == string.Compare("-CreateFile", strTestName, true /* ignoreCase */))
                        {
                            // This creates a random test file with the commandline "syntax" as in ShowCreateFileUsage.
                            // No arguments after these are processed; we return immediately after generating the file.
                            if (args.Length < (ii + 4))
                            {
                                throw new ApplicationException("CreateFile requires args: NumVars NumConstraintsPerVar [weight dblMax]"
                                                              + "[scale dblMax] [padding dblX dblY] [MinClusterSize dblX dblY] [clusters N] OutFileName");
                            }

                            // Use uint to get >= verification.
                            uint cVars, cConstraintsPerVar;
                            ++ii;
                            string strArg = args[ii];
                            if (!uint.TryParse(strArg, out cVars))
                            {
                                throw new ApplicationException(string.Format("Cannot convert arg '{0}' into uint NumVars", strArg));
                            }

                            // NumConstraintsPerVar doesn't mean exactly the same thing for OverlapRemoval
                            // that it does for ProjectionSolver; see the code.
                            ++ii;
                            strArg = args[ii];
                            if (!uint.TryParse(strArg, out cConstraintsPerVar))
                            {
                                throw new ApplicationException(string.Format("Cannot convert arg '{0}' into uint NumConstraintsPerVar", strArg));
                            }

                            // The optional weight and cluster args (and probably others that were added)
                            // require a bit of care with the arg count.
                            bool fToFailure = false;
                            bool fOneFixed = false;
                            string strFileName;
                            for (++ii; /* termination tested in loop */; ++ii)
                            {
                                if (args.Length == ii)
                                {
                                    throw new ApplicationException("CreateFile missing filename arg");
                                }
                                strArg = args[ii];
                                CheckArgForTest(strArg);

                                // Any option that has args must increment ii *other than* the increment to the next option,
                                // which is done by the for loop.
                                if (0 == string.Compare("eq", strArg, true))
                                {
                                    ProjectionSolverTester.WantEqualityConstraints = true;
                                    continue;
                                }
                                if (0 == string.Compare("StartAtZero", strArg, true))
                                {
                                    ProjectionSolverTester.WantStartAtZero = true;
                                    continue;
                                }
                                if (GetArg("PosMax", args, ref ii, ref MaxNodePosition))
                                {
                                    continue;
                                }
                                if (GetArg("GapMax", args, ref ii, ref ProjectionSolverTester.MaxGapToGenerate))
                                {
                                    continue;
                                }
                                if (GetArg("SizeMax", args, ref ii, ref OverlapRemovalTester.MaxSize))
                                {
                                    continue;
                                }
                                if (GetArg("Margins", args, ref ii, ref OverlapRemovalTester.MaxMargin))
                                {
                                    continue;
                                }
                                if (GetArg("Nbours", args, ref ii, ref ProjectionSolverTester.NumberOfNeighboursPerVar))
                                {
                                    continue;
                                }
                                if (GetArg("CreateCycles", args, ref ii, ref temp))
                                {
                                    ProjectionSolverVerifier.NumberOfCyclesToCreate = temp;
                                    continue;
                                }
                                if (0 == string.Compare("weight", strArg, true)) {
                                    ParseDoubleArgWithDefault(args, ref ii, ref MaxWeightToGenerate, DefaultWeightAndScale);
                                }
                                else if (0 == string.Compare("scale", strArg, true)) {
                                    ParseDoubleArgWithDefault(args, ref ii, ref ProjectionSolverTester.MaxScaleToGenerate, DefaultWeightAndScale);
                                }
                                else if (0 == string.Compare("padding", strArg, true))
                                {
                                    if (args.Length <= (ii + 2))
                                    {
                                        throw new ApplicationException("Missing values for 'padding' arg");
                                    }
                                    OverlapRemovalVerifier.InitialMinPaddingX = double.Parse(args[++ii]);
                                    OverlapRemovalVerifier.InitialMinPaddingY = double.Parse(args[++ii]);
                                }
                                else if (0 == string.Compare("MinClusterSize", strArg, true))
                                {
                                    if (args.Length <= (ii + 2))
                                    {
                                        throw new ApplicationException("Missing values for 'MinClusterSize' arg");
                                    }
                                    OverlapRemovalTester.MinClusterSizeX = double.Parse(args[++ii]);
                                    OverlapRemovalTester.MinClusterSizeY = double.Parse(args[++ii]);
                                }
                                else if (0 == string.Compare("clusters", strArg, true))
                                {
                                    if (++ii >= args.Length)
                                    {
                                        throw new ApplicationException("Missing value for 'clusters' arg");
                                    }
                                    strArg = args[ii];
                                    if (0 == string.Compare("def", strArg, true))
                                    {
                                        OverlapRemovalTester.MaxClusters = (int)cVars / 10;
                                    }
                                    else
                                    {
                                        if (0 == string.Compare("rand", strArg, true))
                                        {
                                            OverlapRemovalTester.WantRandomClusters = true;
                                            ++ii;
                                            if (args.Length == ii)
                                            {
                                                throw new ApplicationException("CreateFile missing clusters rand arg");
                                            }
                                            strArg = args[ii];
                                        }
                                        if (0 == string.Compare("def", strArg, true))
                                        {
                                            OverlapRemovalTester.MaxClusters = (int)cVars / 10;
                                        }
                                        else
                                        {
                                            // Use uint to get >= verification.
                                            OverlapRemovalTester.MaxClusters = (int)uint.Parse(args[ii]);
                                        }
                                    }
                                    if (OverlapRemovalTester.MaxClusters < 0)
                                    {
                                        throw new ApplicationException("Value for 'clusters' arg must be >= 0.0");
                                    }
                                    if (ii < (args.Length - 1))
                                    {
                                        strArg = args[ii + 1];
                                        if (0 == string.Compare("singleRoot", strArg, true))
                                        {
                                            ++ii;
                                            OverlapRemovalTester.WantSingleClusterRoot = true;
                                        }
                                    }
                                }
                                else if (0 == string.Compare("seed", strArg, true))
                                {
                                    ++ii;
                                    if (args.Length == ii)
                                    {
                                        throw new ApplicationException("Missing value for 'margins' arg");
                                    }
                                    strArg = args[ii];
                                    System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer;
                                    if (strArg.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // For some reason the 0x prefix is not allowed for hex strings.
                                        strArg = strArg.Substring(2);
                                        style = System.Globalization.NumberStyles.HexNumber;
                                    }
                                    RandomSeed = int.Parse(strArg, style);
                                }
                                else if (0 == string.Compare("fixed", strArg, true))
                                {
                                    ++ii;
                                    if (args.Length == ii)
                                    {
                                        throw new ApplicationException("Missing value for 'fixed' arg");
                                    }
                                    strArg = args[ii];

                                    if (0 == string.Compare("one", strArg, true /* ignoreCase */))
                                    {
                                        fOneFixed = true;
                                        ++ii;
                                        if (args.Length == ii)
                                        {
                                            throw new ApplicationException("Missing value for 'fixed' arg");
                                        }
                                        strArg = args[ii];
                                    }

                                    if (0 == string.Compare("rand", strArg, true /* ignoreCase */))
                                    {
                                        // Let's just "randomly" pick the letters.  I don't know how good the distribution
                                        // of bits is at the lower end so just pick one in the middle.
                                        Random rng = NewRng();      // This will honor RandomSeed
                                        const int TestMask = 0x1 << 4;
                                        OverlapRemovalTester.WantFixedLeftBorder = (0 != (rng.Next() & TestMask));
                                        OverlapRemovalTester.WantFixedRightBorder = (0 != (rng.Next() & TestMask));
                                        OverlapRemovalTester.WantFixedTopBorder = (0 != (rng.Next() & TestMask));
                                        OverlapRemovalTester.WantFixedBottomBorder = (0 != (rng.Next() & TestMask));
                                    }
                                    else
                                    {
                                        if (strArg.IndexOf("L", StringComparison.InvariantCultureIgnoreCase) >= 0)
                                        {
                                            OverlapRemovalTester.WantFixedLeftBorder = true;
                                        }
                                        if (strArg.IndexOf("R", StringComparison.InvariantCultureIgnoreCase) >= 0)
                                        {
                                            OverlapRemovalTester.WantFixedRightBorder = true;
                                        }
                                        if (strArg.IndexOf("T", StringComparison.InvariantCultureIgnoreCase) >= 0)
                                        {
                                            OverlapRemovalTester.WantFixedTopBorder = true;
                                        }
                                        if (strArg.IndexOf("B", StringComparison.InvariantCultureIgnoreCase) >= 0)
                                        {
                                            OverlapRemovalTester.WantFixedBottomBorder = true;
                                        }
                                    }
                                }
                                else if (0 == string.Compare("ToFailure", strArg, true))
                                {
                                    fToFailure = true;
                                }
                                else
                                {
                                    // Unknown option so assume it's the filename, and we're done.
                                    strFileName = strArg;
                                    if (ii < (args.Length - 1))
                                    {
                                        throw new ApplicationException(string.Format("Unexpected arguments following filename '{0}'", strFileName));
                                    }
                                    break;
                                }
                            } // endforever

                            // Postprocess args.
                            if (fOneFixed && (OverlapRemovalTester.MaxClusters > 0))
                            {
                                Random rng = NewRng();
                                OverlapRemovalTester.IndexOfOneFixedCluster = (rng.Next() >> 4) % OverlapRemovalTester.MaxClusters;
                            }

                            if (fToFailure && (0 != RandomSeed))
                            {
                                throw new InvalidOperationException("Cannot specify Seed with ToFailure");
                            }

                            // Do it once if !fToFailure, else until failure (or until ctrl-c if everything's
                            // working fine).
                            bool succeeded;
                            uint cReps = 0;
                            uint cMod = 1;
                            string strSuffix = "";
                            bool fFirstCol = true;
                            do
                            {
                                succeeded = testerInstance.CreateFile(cVars, cConstraintsPerVar, strFileName);
                                if (fToFailure && succeeded)
                                {
                                    ++cReps;
                                    if (100000 == cReps)
                                    {
                                        cMod = 1000;
                                        strSuffix = "k";
                                    }
                                    RandomSeed = 0;    // This makes it reset with Date.Time.Now for each run.

                                    // Minimize scrolling.
                                    if (0 == (cReps % cMod))
                                    {
                                        if (!fFirstCol)
                                        {
                                            Console.Write(" ");
                                        }
                                        fFirstCol = false;
                                        Console.Write("{0}{1}", cReps / cMod, strSuffix);

                                        if (0 == cReps % (20 * cMod))
                                        {
                                            Console.WriteLine();
                                            fFirstCol = true;
                                        }
                                    }
                                    Reset();
                                } // endif fToFailure
                            } while (fToFailure && succeeded);
                            if (succeeded)
                            {
                                Console.WriteLine("Random testfile successfully created");
                                return 0;
                            }
                            Console.WriteLine("Random testfile creation failed : " + strArg);
                            return 1;
                        } // endif -CreateFile

                        if (0 == string.Compare("-RecreateFile", strTestName, true /* ignoreCase */))
                        {
                            // As with -CreateFile, this processes no args after the file specs.
                            if (args.Length < (ii + 2))
                            {
                                throw new ApplicationException("RecreateFile requires args: ExistingFileName NewFileName");
                            }
                            if (testerInstance.ReCreateFile(args[ii + 1], args[ii + 2]))
                            {
                                Console.WriteLine("Random testfile successfully re-created");
                                return 0;
                            }
                            Console.WriteLine("Random testfile re-creation failed!");
                            return 1;
                        }

                        if (0 == string.Compare("-UpdateFile", strTestName, true /* ignoreCase */))
                        {
                            // As with -CreateFile, this processes no args after the file specs.
                            if (args.Length < (ii + 1))
                            {
                                throw new ApplicationException("UpdateFile requires args: ExistingFileNameToBeUpdated");
                            }
                            if (testerInstance.ReCreateFile(args[ii + 1], args[ii + 1]))
                            {
                                Console.WriteLine("Random testfile successfully updated");
                                return 0;
                            }
                            Console.WriteLine("Random testfile re-creation failed!");
                            return 1;
                        }

                        if (0 == string.Compare("-showall", strTestName, true /*ignorecase*/))
                        {
                            // We use anything after the arg name as a prefix/regex unless it's a switch.
                            string strItem = string.Empty;
                            if (((ii + 1) < args.Length) && !IsSwitchArg(args[ii + 1]))
                            {
                                ++ii;
                                strItem = args[ii];
                            }
                            this.showallSpecs.Add(strItem);
                            continue;
                        }
                        if (0 == string.Compare("-runall", strTestName, true /*ignorecase*/))
                        {
                            // We use anything after the arg name as a prefix/regex unless it's a switch.
                            string strItem = string.Empty;
                            if (((ii + 1) < args.Length) && !IsSwitchArg(args[ii + 1]))
                            {
                                ++ii;
                                strItem = args[ii];
                            }
                            this.runallSpecs.Add(strItem);
                            continue;
                        }
                        if (0 == string.Compare("-file", strTestName, true /*ignorecase*/))
                        {
                            if ((ii + 1) >= args.Length)
                            {
                                // require one value
                                throw new ApplicationException("Missing filename for -" + strTestName);
                            }
                            ++ii;
                            this.testFileNames.Add(args[ii]);
                            continue;
                        }

                        // Not a known option so see if it's the name of a static test.
                        if (FindTestName(strTestName)) {
                            continue;
                        }

                        Console.WriteLine();
                        Console.WriteLine(" *** Error - cannot find TestConstraints method or files matching '{0}'", strTestName);
                        ListOfFailedTestAndFileNames.Add(strTestName);
                    } // end foreach strTestName
                }

                // If !InteractiveMode, this sets up Debug.Assert bypass to logging or console so we don't hang the process on a dialog.
                RedirectDefaultTraceListener();

                // If multiple classifications were specified, this execution order makes the most sense:
                // display, single tests, bulk tests.
                foreach (string spec in this.showallSpecs)
                {
                    AllTests(spec, false /*fRun*/);
                }
                foreach (Tuple<string, MethodInfo> testMethodInfo in this.testMethodInfos)
                {
                    Console.WriteLine("( {0} )", testMethodInfo.Item1);
                    InvokeTestMethod(testMethodInfo.Item2);
                }
                foreach (string fileSpec in this.testFileNames)
                {
                    ProcessFiles(fileSpec, recursiveFiles);
                }
                foreach (string spec in this.runallSpecs)
                {
                    AllTests(spec, true /*fRun*/);
                }
                RestoreDefaultTraceListener();

            }
            catch (Exception exc)
            {
                Console.Error.WriteLine("TestConstraints exception: {0}", exc);
                return 1;
            }

            // Made it to the end, now let's see how we did.
            Console.WriteLine();
            if (0 == ListOfFailedTestAndFileNames.Count)
            {
                Console.WriteLine("TestConstraints: all {0} test(s) succeeded :)", totalTestsRun);
                return 0;
            }
            
            Console.WriteLine("MSAGL_Test: {0} of {1} test(s) failed :(", ListOfFailedTestAndFileNames.Count, totalTestsRun);
            foreach (String strTestName in ListOfFailedTestAndFileNames)
            {
                Console.WriteLine("  {0}", strTestName);
            }
            return 1;
        }

        private static void ParseDoubleArgWithDefault(string[] args, ref int ii, ref double result, double defaultValue)
        {
            string argKey = args[ii];
            if ((args.Length - 1) == ii)
            {
                throw new ApplicationException(string.Format("Missing max value for '{0}' arg", argKey));
            }
            string strArg = args[++ii];
            if (0 == string.Compare("def", strArg, true))
            {
                result = defaultValue;
            }
            else
            {
                result = double.Parse(args[ii]);
                if (result < 0.0)
                {
                    throw new ApplicationException(string.Format("Max value for '{0}' arg must be >= 0.0", argKey));
                }
            }
        }

        private static bool IsArg(string[] args, ref int iarg, string strName)
        {
            string strArg = args[iarg];
            if (0 != string.Compare(strName, strArg, true))
            {
                return false;
            }
            if (++iarg >= args.Length)
            {
                throw new ApplicationException(string.Format("Missing value for '{0}'", strName));
            }
            return true;
        }

        private static bool GetArg(string strName, string[] args, ref int iarg, ref double value)
        {
            if (!IsArg(args, ref iarg, strName))
            {
                return false;
            }
            value = double.Parse(args[iarg]);
            return true;
        }

        private static bool GetArg(string strName, string[] args, ref int iarg, ref int value)
        {
            if (!IsArg(args, ref iarg, strName))
            {
                return false;
            }
            value = int.Parse(args[iarg]);
            return true;
        }

        // Overload because we can't pass autoproperties as refs.
        private static bool GetArg(string strName, string[] args, ref int iarg, Action<string> assigner)
        {
            if (!IsArg(args, ref iarg, strName))
            {
                return false;
            }
            assigner(args[iarg]);
            return true;
        }

        private void Reset()
        {
            if (null != testerInstance)
            {
                testerInstance.Reset();
            }
        }

        private void ProcessFiles(string pathAndFileSpec, bool recursiveFiles)
        {
            var tfp = new TestFileProcessor(Console.WriteLine, LogError, this.ProcessFile, TestGlobals.VerboseLevel >= 1, quietOutput)
            {
                Recursive = recursiveFiles
            };

            tfp.ProcessFiles(pathAndFileSpec);
        }

        private void ProcessFile(string fileName)
        {
            Reset();
            ++totalTestsRun;
            testerInstance.ProcessFile(fileName);
        }

        private void InvokeTestMethod(MethodInfo methodInfo)
        {
            try
            {
                Reset();
                ++totalTestsRun;
                methodInfo.Invoke(testerInstance, null);
            }
            catch (Exception ex)
            {
                HandleException(ex, methodInfo.Name);
            }
        }

        private void HandleException(Exception ex, string name)
        {
            ListOfFailedTestAndFileNames.Add(name);
            string exType = (ex is UnitTestAssertException) ? "Assert Failure" : "Exception";
            Exception exToUse = ex.InnerException ?? ex;
            LogError(string.Format("*** {0} in Test method ***   {1}: {2}", exType, name, exToUse));
        }

        static bool IsSwitchArg(string strArg)
        {
            return (strArg.StartsWith("-") || strArg.StartsWith("/"));
        }

        private bool LogError(string message)
        {
            bool ret = false;
            if (null != errorLog)
            {
                // Always prepend the full commandline in case this is part of a batch.
                ret = true;
                File.AppendAllLines(errorLog, new[] { DateTime.Now.ToString(), Environment.CommandLine, message, "", "********", "" });
            }
            if (Validate.InteractiveMode)
            {
                // We can be both interactive and to-file.  Since we did not call ClassInitialize, we will never be here 
                // for Debug.Assert, only for Assert.<whatever> failures that we did not replace by Validate<whatever>.
                // Unfortunately by this time the exception-throwing stack has been unwound (that's why we have the Validate
                // class, to avoid this), so we'll have to rerun again in the debugger to break on the exception throw.
                ret = true;
                Validate.RaiseInteractiveAssert(message);
            }
            if (!ret)
            {
                // No errorlog and not interactive so just write the full exception to console and continue.
                Console.WriteLine(message);
            }

            // We handled the error so return true.
            return true;
        }

        private bool IsTestMethod(MethodInfo mi)
        {
            if ((null == mi) || (0 != mi.GetParameters().Count()))
            {
                return false;
            }
            if (mi.GetCustomAttributes(typeof(TestMethodAttribute), true /*inherit*/).Length == 0)
            {
                return false;
            }
            bool isIgnored = (mi.GetCustomAttributes(typeof(IgnoreAttribute), true /*inherit*/).Length > 0);
            if ((isIgnored && !(ignoredTestsOk || ignoredTestsOnly)) || (!isIgnored && ignoredTestsOnly))
            {
                return false;
            }
            return true;
        }

        private bool FindTestName(string strTestName)
        {
            var methodInfo = testerInstance.GetType().GetMethod(strTestName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
            if (IsTestMethod(methodInfo))
            {
                this.testMethodInfos.Add(new Tuple<string, MethodInfo>(strTestName, methodInfo));
                return true;
            }
            return false;
        }

        void AllTests(string strPrefix, bool fRun)
        {
            // Run all tests starting with this prefix.
            Regex rgx = null;

            // If they specified a regex, use it to filter the returned names.
            if (!string.IsNullOrEmpty(strPrefix) && strPrefix.IndexOfAny(".*?+()".ToCharArray()) >= 0)
            {
                rgx = new Regex(strPrefix, RegexOptions.IgnoreCase);
            }

            // Get all public static methods.
            var methodInfos = testerInstance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(IsTestMethod);
            int count = 0;
            foreach (var methodInfo in methodInfos)
            {
                if (0 == methodInfo.GetParameters().Count())
                {
                    bool wantThisMethod = true;
                    if (null != rgx)
                    {
                        var m = rgx.Match(methodInfo.Name);
                        wantThisMethod = m.Success;
                    }
                    else if (null != strPrefix)
                    {
                        wantThisMethod = methodInfo.Name.StartsWith(strPrefix, true /* ignoreCase */
                                            , System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (wantThisMethod)
                    {
                        ++count;
                        Console.WriteLine("( {0} )", methodInfo.Name);
                        if (fRun)
                        {
                            InvokeTestMethod(methodInfo);
                        }
                    } // endif fWantThisMethod
                } // endif methInfo.IsStatic
            }
            Console.WriteLine("{0} tests", count);
        }
    }
}