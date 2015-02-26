/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.Linq;
using FindOverlapSample.Statistics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Node = Microsoft.Msagl.Core.Layout.Node;

namespace FindOverlapSample {
    /// <summary>
    /// This class is used to run some tests with different node overlap removal methods.
    /// </summary>
    internal class Program {

        private static void Main(string[] args) {
#if DEBUG && !SILVERLIGHT
                        DisplayGeometryGraph.SetShowFunctions();
//                        ProximityOverlapRemoval.DebugMode = true;
#endif

            //          OverlapRemovalTestSuite.ComparisonSuite(@"C:\dev\GraphLayout\graphs\overlapSamples\debugOnly\", "DebugOnlyTestSuite1.csv", false);
            //          OverlapRemovalTestSuite.ComparisonSuite(@"C:\dev\GraphLayout\graphs\overlapSamples\", "ResultsOverlapRemovalTestSuite1.csv", false);
            //            OverlapRemovalTestSuite.ComparisonSuite(@"C:\dev\GraphLayout\graphs\overlapSamples\net50comp1\", "ResultsNet50comp1TestSuite1.csv", false);
            //          OverlapRemovalTestSuite.ComparisonSuite(@"C:\dev\GraphLayout\graphs\overlapSamples\large\", "ResultsLargeGraphsTestSuite1.csv", false);
            OverlapRemovalTestSuite.ComparisonSuite(
                @"C:\dev\GraphLayout\graphs\overlapSamples\prism-original-dataset\",
                "ResultsPrism-original-datasetTestSuite1.csv", false);


            //            Console.ReadLine();
            //            var rootGraph = DotLoader.LoadFile(@"C:\dev\GraphLayout\graphs\overlapSamples\root.dot");

            ////          var rootGraph = DotLoader.LoadFile(@"C:\dev\GraphLayout\graphs\overlapSamples\net50comp1\net50comp_1.gv.dot");
            ////          var rootGraph = DotLoader.LoadFile(@"C:\dev\GraphLayout\graphs\overlapSamples\badvoro.gv.dot");
            ////          var rootGraph = DotLoader.LoadFile(@"C:\dev\GraphLayout\graphs\large\twittercrawl-sfdp.dot");
            ////          var oldPositions = rootGraph.Nodes.Select(v => v.Center).ToList();
            ////          LayoutAlgorithmSettings.ShowGraph(rootGraph);
            //            ProximityOverlapRemoval prism=new ProximityOverlapRemoval(rootGraph);
            //            prism.Settings.WorkInInches = true;
            //            prism.Settings.StressSettings.ResidualTolerance = 0.06;
            //#if DEBUG
            //            ProximityOverlapRemoval.DebugMode = false;
            //#endif 
            //            prism.RemoveOverlap();
            ////            var newPositions = rootGraph.Nodes.Select(v => v.Center).ToList();
            ////            var procrustes = Statistics.Statistics.ProcrustesStatistics(oldPositions, newPositions);
            ////            Console.WriteLine("ProcrustesStatistics: {0}",procrustes);
            //            
            //#if DEBUG
            //
            //            LayoutAlgorithmSettings.ShowGraph(rootGraph);
            //#endif
            //            Console.ReadLine();
        }

     
    }
}
