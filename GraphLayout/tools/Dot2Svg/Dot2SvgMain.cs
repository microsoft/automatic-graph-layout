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
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.GraphViewerGdi;
using System.IO;
﻿using Microsoft.Msagl.Core.Geometry.Curves;
﻿using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using DrawingNode=Microsoft.Msagl.Drawing.Node;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using Label = Microsoft.Msagl.Drawing.Label;
using Node = Microsoft.Msagl.Core.Layout.Node;

namespace Agl {
    class Dot2SvgMain {
        readonly ArgsParser.ArgsParser argsParser;
        const string FileOption="-f";
        const string MsaglOutputOption="-msagl";
        const string PrintOutOption="-printOut";
        const string HelpOption="-?";
        const string PrecisionOption="-precision";
        const string VssParserOption = "-vssParser";
        const string NoLabelsOption = "-nolabels";
        const string PrintProcessedFileNameOption = "-printFileNames";
        const string OutputDirOption = "-outdir";
        const string NoArrowheads = "-noarrows";
        const string NoUrls = "-nourls";

        string helpString;
        bool printOutToConsole;
        bool msaglOutput;
        int precision=3;
     
        Dot2SvgMain(string[] args) {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            argsParser = new ArgsParser.ArgsParser(args);
            argsParser.AddAllowedOptionWithHelpString("-nolayout", "do not run layout if the geometry is there");
            argsParser.AddOptionWithAfterString(FileOption);
            argsParser.AddAllowedOption(MsaglOutputOption);
            argsParser.AddAllowedOption(PrintOutOption);
            argsParser.AddAllowedOption(HelpOption);
            argsParser.AddOptionWithAfterString(PrecisionOption);
            argsParser.AddAllowedOption(VssParserOption);
            argsParser.AddAllowedOption(NoLabelsOption);
            argsParser.AddAllowedOption(PrintProcessedFileNameOption);
            argsParser.AddOptionWithAfterString(OutputDirOption);
            argsParser.AddAllowedOption(NoArrowheads);
            argsParser.AddAllowedOption(NoUrls);
            argsParser.AddOptionWithAfterStringWithHelp("-orient", "one of options  TB, LR, BT, RL");
            argsParser.AddAllowedOptionWithHelpString("-bw", "black white colors in SVG");
            argsParser.AddAllowedOption("-noedges");
            argsParser.AddOptionWithAfterStringWithHelp("-scaleNodesBy", "scale node only if the geometry is given");
            argsParser.AddOptionWithAfterStringWithHelp("-nblw", "node boundary line width");
        }


        static int Main(string[] args) {
            var p = new Dot2SvgMain(args);
            return p.DoJob();
        }

        int DoJob() {
            if (!argsParser.Parse()) {
                var s = String.Format("{2}. Wrong arguments. Usage \"graphRendererSample foo.dot  bar.dot [-f listOfDotFile] [-printOut] [-svg] [-xml] [-precision number] [{0}] [{1}] ", 
                    NoLabelsOption, PrintProcessedFileNameOption, argsParser.ErrorMessage);

                return -1;
            }
            if (argsParser.OptionIsUsed(HelpOption))
                return PrintHelpAndExit();

            var precisionStr = argsParser.GetStringOptionValue(PrecisionOption);
            if (precisionStr != null) {
                var prec=int.Parse(precisionStr);
                if(prec!=0)
                    precision=prec;
            }

            msaglOutput = argsParser.OptionIsUsed(MsaglOutputOption);
            printOutToConsole = argsParser.OptionIsUsed(PrintOutOption);
            foreach (var file in argsParser.FreeArgs) {
                int r = ProcessFile(file);
                if (r != 0)
                    return r;
            }
            var listFile = argsParser.GetStringOptionValue(FileOption);
            if (listFile != null)
                return ProccessFileList(listFile);
            return 0;
        }

        int PrintHelpAndExit() {
            var exeName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            var usageString =
                String.Format("Usage: {0} [* filename] [-f listOfDotFile] [-printOut] [-msagl] [-precision number]", exeName);
            helpString = usageString + "\n" +
                         "This utility reads a text file representing a graph, lays the graph out,\n" +
                         "and outputs the graph with the layout information.\n" +
                         "The input can be in the Dot format or in the XML format of MSAGL.\n" +
                         "The output can be in the SVG format or in the XML format of MSAGL.\n" +
                         "The output in the MSAGL format will be pure geometry.";
            System.Diagnostics.Debug.WriteLine(helpString);
            return 0;
        }

        int ProccessFileList(string listFile) {
            bool success = true;
            try {
                var path = Path.GetDirectoryName(listFile);
                using (var stream = new StreamReader(listFile)) {
                    do {
                        string filename = stream.ReadLine();
                        if (filename == null)
                            break;
                        if (!File.Exists(filename))
                            if (path != null)
                                filename = Path.Combine(path, filename);

                        if (!File.Exists(filename)) {
                            System.Diagnostics.Debug.WriteLine("File does not exist \"{0}\"", filename);
                            return 1;
                        }
                        int r = ProcessFile(filename);
                        if (r != 0)
                            success=false;
                    } while (true);
                }
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                success=false;
            }
            return success ? 0 : 1;
        }
        
        
        
        int ProcessFile(string inputFile) {
            if(argsParser.OptionIsUsed(PrintProcessedFileNameOption))
                System.Diagnostics.Debug.WriteLine(inputFile);
            try {
                var inputType = FigureOutFileType(inputFile);
                if (inputType == FileType.Dot)
                    return ProcessDotFile(inputFile);
                if (inputType == FileType.Msagl)
                    return ProcessMsaglFile(inputFile);
                return -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return -1;
            }
        }

        int ProcessMsaglFile(string inputFile) {
            try {
                LayoutAlgorithmSettings ls;
                var geomGraph = GeometryGraphReader.CreateFromFile(inputFile, out ls);
                if (ls == null)
                    ls = PickLayoutAlgorithmSettings(geomGraph.Edges.Count, geomGraph.Nodes.Count);
                LayoutHelpers.CalculateLayout(geomGraph, ls, null);                
                inputFile = SetMsaglOutputFileName(inputFile);
                WriteGeomGraph(inputFile, geomGraph);
                DumpFileToConsole(inputFile);

            }
            catch(Exception e) {
                //System.Diagnostics.Debug.WriteLine(e.ToString());
                System.Diagnostics.Debug.WriteLine(e.Message);
                return -1;
            }
            return 0;
        }

        static string SetMsaglOutputFileName(string inputFile) {
            Path.ChangeExtension(inputFile, "msagl.geom");
            if (!inputFile.EndsWith("msagl.geom"))
                inputFile = inputFile + ".msagl.geom";
            return inputFile;
        }

        int ProcessDotFile(string inputFile) {
            Graph graph;
            int i = CreateGraphFromDotFile(inputFile, out graph);
            graph.Attr.LayerDirection = GetLayerDirection();

            if (i != 0)
                return i;
            if (argsParser.OptionIsUsed("-nolayout") && GeometryIsPresent(graph)) {
                double nodeScale;
                bool scaling = argsParser.GetDoubleOptionValue("-scaleNodesBy", out nodeScale);
                if (scaling) {
                    foreach (var node in graph.GeometryGraph.Nodes) {
                       node.BoundaryCurve = node.BoundaryCurve.Transform(PlaneTransformation.ScaleAroundCenterTransformation(nodeScale,
                            node.Center));
                    }
                    graph.GeometryGraph.UpdateBoundingBox();
                }
                double nodeLineWidth;
                if (argsParser.GetDoubleOptionValue("-nblw", out nodeLineWidth)) {
                    foreach (var node in graph.Nodes) {
                        node.Attr.LineWidth = nodeLineWidth;
                    }
                    graph.GeometryGraph.UpdateBoundingBox();
                }
            }
            else
                PrepareForOutput(graph);

            var outputFile = Path.ChangeExtension(inputFile, ".svg");
            string outputDir = argsParser.GetStringOptionValue(OutputDirOption);
            if (outputDir != null) {
                var name = Path.GetFileName(outputFile);
                if (name != null)
                    outputFile = Path.Combine(outputDir, name);
            }


            SetConsolasFontAndSize(graph, 11);
            if (argsParser.OptionIsUsed(NoLabelsOption))
                RemoveLabelsFromGraph(graph);
            using (var stream = File.Create(outputFile)) {
                var svgWriter = new SvgGraphWriter(stream, graph) {
                    BlackAndWhite = argsParser.OptionIsUsed("-bw"),
                    Precision = precision,
                    AllowedToWriteUri = !argsParser.OptionIsUsed(NoUrls),
                    IgnoreEdges = argsParser.OptionIsUsed("-noedges")
                };
                svgWriter.Write();
                DumpFileToConsole(outputFile);
            }

            if(msaglOutput) {
                outputFile = SetMsaglOutputFileName(inputFile);
                var geomGraph = graph.GeometryGraph;
                WriteGeomGraph(outputFile, geomGraph);
            }
            return 0;
        }

        private bool GeometryIsPresent(Graph graph) {
            return graph.BoundingBox.Width > 0;
        }

        private void PrepareForOutput(Graph graph) {
            graph.LayoutAlgorithmSettings = PickLayoutAlgorithmSettings(graph.EdgeCount, graph.NodeCount);
            if (argsParser.OptionIsUsed(NoArrowheads))
                RemoveArrowheadsFromGraph(graph);
            EnlargeLabelMargins(graph);
            SetConsolasFontAndSize(graph, 13);
            // rendering
            var renderer = new GraphRenderer(graph);
            renderer.CalculateLayout();
            SetBoxRadiuses(graph);
        }

        private LayerDirection GetLayerDirection()
        {
            string orientOption = argsParser.GetStringOptionValue("-orient");
            if (orientOption == null)
                return LayerDirection.TB;
            switch (orientOption)
            {
                case "LR":
                    return LayerDirection.LR;
                case "TB":
                    return LayerDirection.TB;
                case "BT":
                    return LayerDirection.BT;
                case "RL":
                    return LayerDirection.RL;
                default:return LayerDirection.TB;
            }

        }

        static void RemoveArrowheadsFromGraph(Graph graph) {
            foreach (var edge in graph.Edges)
                RemoveArrowheadFromEdge(edge);
        }

        static void RemoveArrowheadFromEdge(DrawingEdge edge) {
           edge.Attr.ArrowheadAtSource=ArrowStyle.None;
           edge.Attr.ArrowheadAtTarget = ArrowStyle.None;
        }

        void WriteGeomGraph(string outputFile, GeometryGraph geomGraph) {
            using (var stream = File.Create(outputFile))
            {
                var ggr = new GeometryGraphWriter(stream, geomGraph, null) {Precision = precision};
                ggr.Write();
            }
        }

        static void SetBoxRadiuses(Graph graph) {
            foreach (var node in graph.Nodes) {
                var attr = node.Attr;
                var r = Math.Min(node.Width, node.Height)/10;
                attr.XRadius = r;
                attr.YRadius = r;
            }
        }

        void DumpFileToConsole(string outputFile) {
            if (printOutToConsole) {
                using (var outStream = new StreamReader(outputFile)) {
                    do {
                        string s = outStream.ReadLine();
                        if (s != null)
                            Console.WriteLine(s);
                        else break;
                    } while (true);
                }
            }
        }

        int CreateGraphFromDotFile(string inputFile, out Graph graph) {
            int line, column;
            string msg = "syntax error";
            try {
                graph = Dot2Graph.Parser.Parse(inputFile, out line, out column, out msg);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine(ex.Message);
                graph = null;
                return -1;
            }
            if (graph == null) {
                System.Diagnostics.Debug.WriteLine("{0}({1},{2}): error: {3}", inputFile, line, column, msg);
                return -1;
            }
            return 0;
        }

        static void RemoveLabelsFromGraph(Graph graph) {
            foreach (var node in graph.Nodes)
                node.Label = null;
            foreach (var edge in graph.Edges)
                edge.Label = null;
        }

        static void EnlargeLabelMargins(Graph graph) {
            foreach (var node in graph.Nodes)
                node.Attr.LabelMargin = 8;
        }

        static void SetConsolasFontAndSize(Graph graph, int size) {
            var labels = GetAllLabels(graph);
            foreach (var label in labels) {
                label.FontName = "Consolas";
                label.FontSize = size;
            }
        }

        static IEnumerable<Label> GetAllLabels(Graph graph) {
            foreach (DrawingNode node in graph.Nodes)
                if (node.Label != null)
                    yield return node.Label;
            foreach (DrawingEdge edge in graph.Edges)
                if (edge.Label != null)
                    yield return edge.Label;
        }

        static LayoutAlgorithmSettings PickLayoutAlgorithmSettings(int edges, int nodes) {
            LayoutAlgorithmSettings settings;
            const int sugiaymaTreshold = 200;
            const double bundlingTreshold = 3.0;
            bool bundling = nodes != 0 && ((double)edges / nodes >= bundlingTreshold || edges>100);
            if (nodes < sugiaymaTreshold && edges<sugiaymaTreshold) {
                settings = new SugiyamaLayoutSettings();

                if (bundling)
                    settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
            } else {
                MdsLayoutSettings mdsSettings;
                settings = mdsSettings = new MdsLayoutSettings {
                    EdgeRoutingSettings = {
                        EdgeRoutingMode
                            =
                            bundling
                                ? EdgeRoutingMode.SplineBundling
                                : EdgeRoutingMode.Spline
                    }                   
                };
                if (bundling)
                    settings.EdgeRoutingSettings.BundlingSettings = new BundlingSettings();
                double scale = FigureOutScaleForMdsLayout(nodes);
                mdsSettings.ScaleX = scale;
                mdsSettings.ScaleY = scale;
            }
            return settings;
        }
        static double FigureOutScaleForMdsLayout(int nodes) {
            const int maxScale = 900;
            const int minScale = 400;
            return Math.Min(nodes + minScale, maxScale);
        }
        static FileType FigureOutFileType(string file) {
            try {
                if (file.EndsWith(".dot")) return FileType.Dot;
                using (var stream=new StreamReader(file) ) {
                    for (var str = stream.ReadLine(); str!=null; str=stream.ReadLine() ) {
                        if( str.TrimStart().StartsWith("digraph"))
                            return FileType.Dot;
                        if (str.TrimStart().StartsWith("graph"))
                            return FileType.Dot;
                        if(!string.IsNullOrWhiteSpace(str))
                            return FileType.Msagl;
                    }
                    return FileType.Unknown;
                }
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                throw;
            }
            
        }
    }
}


