using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Dot2Graph;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.Msagl.Routing.Spline.Bundling;
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.Msagl.UnitTests;
using TestFormForGViewer;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Node = Microsoft.Msagl.Core.Layout.Node;

namespace Test01 {
    internal class Program {
        static bool bundling;
        const string SvgFileNameOption = "-svgout";

        const string QuietOption = "-quiet";
        const string FileOption = "-file";
        const string BundlingOption = "-bundling";
        const string MdsOption = "-mds";
        const string FdOption = "-fd";
        const string EdgeSeparationOption = "-es";
        const string InkImportanceOption = "-ink";
        const string TightPaddingOption = "-tpad";
        const string LoosePaddingOption = "-lpad";
        const string CapacityCoeffOption = "-cc";
        const string AsyncLayoutOption = "-async";


        [STAThread]
        static void Main(string[] args) {
#if TEST_MSAGL
            DisplayGeometryGraph.SetShowFunctions();
#endif
            ArgsParser.ArgsParser argsParser = SetArgsParser(args);
            if (argsParser.OptionIsUsed("-help")) {
                Console.WriteLine(argsParser.UsageString());
                Environment.Exit(0);
            }
            
            bundling = argsParser.OptionIsUsed(BundlingOption);

            var gviewer = new GViewer();
            if (argsParser.OptionIsUsed(FdOption)) {
                gviewer.CurrentLayoutMethod = LayoutMethod.IcrementalLayout;
            }
            Form form = CreateForm(null, gviewer);
            if (argsParser.OptionIsUsed(AsyncLayoutOption))
                gviewer.AsyncLayout = true;

            string fileName = argsParser.GetStringOptionValue(FileOption);
            if (fileName != null) {
                string ext = Path.GetExtension(fileName);
                if (ext != null) {
                    ext = ext.ToLower();
                    if (ext == ".dot") {
                        ProcessDotFile(gviewer, argsParser, fileName);
                    }
                }
                else {
                    Console.WriteLine("do not know how to process {0}", fileName);
                    Environment.Exit(1);
                }
            }
            if (!argsParser.OptionIsUsed(QuietOption))
                Application.Run(form);

        }

        

        static BundlingSettings GetBundlingSettings(ArgsParser.ArgsParser argsParser) {
            if (!argsParser.OptionIsUsed(BundlingOption))
                return null;
            var bs = new BundlingSettings();
            string ink = argsParser.GetStringOptionValue(InkImportanceOption);
            double inkCoeff;
            if (ink != null && double.TryParse(ink, out inkCoeff)) {
                bs.InkImportance = inkCoeff;
                BundlingSettings.DefaultInkImportance = inkCoeff;
            }

            string esString = argsParser.GetStringOptionValue(EdgeSeparationOption);
            if (esString != null) {
                double es;
                if (double.TryParse(esString, out es)) {
                    BundlingSettings.DefaultEdgeSeparation = es;
                    bs.EdgeSeparation = es;
                }
                else {
                    Console.WriteLine("cannot parse {0}", esString);
                    Environment.Exit(1);
                }
            }

            string capacityCoeffString = argsParser.GetStringOptionValue(CapacityCoeffOption);
            if (capacityCoeffString != null) {
                double capacityCoeff;
                if (double.TryParse(capacityCoeffString, out capacityCoeff)) {
                    bs.CapacityOverflowCoefficient = capacityCoeff;
                }
                else {
                    Console.WriteLine("cannot parse {0}", capacityCoeffString);
                    Environment.Exit(1);
                }
            }


            return bs;
        }

        static void ProcessDotFile(GViewer gviewer, ArgsParser.ArgsParser argsParser, string dotFileName) {
            Graph graph = Parser.Parse(dotFileName, out int line, out int col, out string msg);
            if (graph == null) {
                Console.WriteLine("{0}({1},{2}): error: {3}", dotFileName, line, col, msg);
                Environment.Exit(1);
            }
            

            if (argsParser.OptionIsUsed(MdsOption))
                graph.LayoutAlgorithmSettings = new MdsLayoutSettings();
            else if (argsParser.OptionIsUsed(FdOption))
                graph.LayoutAlgorithmSettings = new FastIncrementalLayoutSettings();

            if (argsParser.OptionIsUsed(BundlingOption)) {
                graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
                BundlingSettings bs = GetBundlingSettings(argsParser);
                graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings = bs;
                string ink = argsParser.GetStringOptionValue(InkImportanceOption);
                if (ink != null) {
                    if (double.TryParse(ink, out double inkCoeff)) {
                        bs.InkImportance = inkCoeff;
                        BundlingSettings.DefaultInkImportance = inkCoeff;
                    }
                    else {
                        Console.WriteLine("cannot parse {0}", ink);
                        Environment.Exit(1);
                    }
                }

                string esString = argsParser.GetStringOptionValue(EdgeSeparationOption);
                if (esString != null) {
                    if (double.TryParse(esString, out double es)) {
                        BundlingSettings.DefaultEdgeSeparation = es;
                        bs.EdgeSeparation = es;
                    }
                    else {
                        Console.WriteLine("cannot parse {0}", esString);
                        Environment.Exit(1);
                    }
                }
            }

            gviewer.Graph = graph;
            string svgout = argsParser.GetStringOptionValue(SvgFileNameOption);
            try {
                if (svgout != null) {
                    SvgGraphWriter.Write(gviewer.Graph, svgout, null, null, 4);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
        static void ProcessMsaglFile(string fileName, ArgsParser.ArgsParser argsParser, GViewer gviewer) {
            Graph graph = Graph.Read(fileName);
            if (graph == null) {
                Console.WriteLine("cannot read " + fileName);
                return;
            }

            if (graph.GeometryGraph != null && graph.BoundingBox.Width > 0) {
                //graph does not need a layout
                if (argsParser.OptionIsUsed(BundlingOption)) {
                    RouteBundledEdges(graph.GeometryGraph, argsParser);
                }
            }
        }

        static Form CreateForm(Graph graph, GViewer gviewer) {
            Form form = FormStuff.CreateOrAttachForm(gviewer, null);
            form.SuspendLayout();
            SetEdgeSeparationBar(form);

            gviewer.GraphChanged += GviewerGraphChanged;

            if (graph != null)
                gviewer.Graph = graph;
            return form;
        }

        static void GviewerGraphChanged(object sender, EventArgs e) {
            var gviewer = (GViewer)sender;
            Graph drawingGraph = gviewer.Graph;
            if (drawingGraph != null) {
                var form = (Form)gviewer.Parent;
                foreach (object c in form.Controls) {
                    if (c is CheckBox)
                        break;
                }
                if (bundling) {
                    drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode =
                        EdgeRoutingMode.SplineBundling;
                    SetTransparency(drawingGraph);

                    if (drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings == null)
                        drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings =
                            new BundlingSettings();
                }
            }
        }

        static TrackBar edgeSeparationTrackBar;

        static void SetEdgeSeparationBar(Form form) {
            edgeSeparationTrackBar = new TrackBar();
            form.Controls.Add(edgeSeparationTrackBar);
            edgeSeparationTrackBar.Location = new System.Drawing.Point(form.MainMenuStrip.Location.X + 400,
                                                                       form.MainMenuStrip.Location.Y);
            edgeSeparationTrackBar.Maximum = 20;
            edgeSeparationTrackBar.Value = (int)(0.5 * (edgeSeparationTrackBar.Minimum + edgeSeparationTrackBar.Maximum));
            edgeSeparationTrackBar.ValueChanged += EdgeSeparationTrackBarValueChanged;
            ToolTip tt = new ToolTip();
            tt.SetToolTip(edgeSeparationTrackBar, "Set edge separation for bundling edges");

            edgeSeparationTrackBar.BringToFront();
            form.ResumeLayout();
        }

        static void EdgeSeparationTrackBarValueChanged(object sender, EventArgs e) {
            var edgeSeparationTruckBar = (TrackBar)sender;
            GViewer gviewer = GetGviewer(edgeSeparationTruckBar);

            Graph drawingGraph = gviewer.Graph;
            if (drawingGraph == null)
                return;


            EdgeRoutingSettings edgeRoutingSettings = drawingGraph.LayoutAlgorithmSettings.EdgeRoutingSettings;
            edgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
            if (edgeRoutingSettings.BundlingSettings == null)
                edgeRoutingSettings.BundlingSettings = new BundlingSettings();
            edgeRoutingSettings.BundlingSettings.EdgeSeparation = GetEdgeSeparation(edgeSeparationTruckBar);
            var br = new SplineRouter(drawingGraph.GeometryGraph, 1, 1, Math.PI / 6, edgeRoutingSettings.BundlingSettings);
            br.Run();

            IViewer iv = gviewer;
            foreach (IViewerObject edge in iv.Entities) {
                if (edge is IViewerEdge)
                    iv.Invalidate(edge);
            }
        }

        static void SetTransparency(Graph drawingGraph) {
            foreach (Microsoft.Msagl.Drawing.Edge edge in drawingGraph.Edges) {
                Color color = edge.Attr.Color;
                edge.Attr.Color = new Color(100, color.R, color.G, color.B);
            }
        }

        static double GetEdgeSeparation(TrackBar edgeSeparationTruckBar) {
            double max = edgeSeparationTruckBar.Maximum;
            double min = edgeSeparationTruckBar.Minimum;
            double val = edgeSeparationTruckBar.Value;
            double alpha = (val - min) / (max - min);
            const double sepMaxMult = 2;
            const double sepMinMult = 0.1;
            const double span = sepMaxMult - sepMinMult;
            return (alpha - 0.5) * span + 0.5; //0.5 is the default edge separation
        }

        static GViewer GetGviewer(Control edgeSeparationTruckBar) {
            Control form = edgeSeparationTruckBar.Parent;
            return GetGViewerFromForm(form);
        }

        static GViewer GetGViewerFromForm(Control form) {
            GViewer gv = null;
            foreach (object g in form.Controls) {
                gv = g as GViewer;
                if (gv != null)
                    break;
            }
            return gv;
        }

        static void RouteBundledEdges(GeometryGraph geometryGraph, ArgsParser.ArgsParser argsParser) {
            double loosePadding;
            double tightPadding = GetPaddings(argsParser, out loosePadding);

            var br = new SplineRouter(geometryGraph, tightPadding, loosePadding, Math.PI / 6, new BundlingSettings());
            br.Run();
        }

        static double GetPaddings(ArgsParser.ArgsParser argsParser, out double loosePadding) {
            double tightPadding = 0.5;
            if (argsParser.OptionIsUsed(TightPaddingOption)) {
                string tightPaddingString = argsParser.GetStringOptionValue(TightPaddingOption);
                if (!double.TryParse(tightPaddingString, out tightPadding)) {
                    Console.WriteLine("cannot parse {0} {1}", TightPaddingOption, tightPaddingString);
                    Environment.Exit(1);
                }
            }
            loosePadding = 2.25;
            if (argsParser.OptionIsUsed(LoosePaddingOption)) {
                string loosePaddingString = argsParser.GetStringOptionValue(LoosePaddingOption);
                if (!double.TryParse(loosePaddingString, out loosePadding)) {
                    Console.WriteLine("cannot parse {0} {1}", LoosePaddingOption, loosePaddingString);
                    Environment.Exit(1);
                }
            }
            return tightPadding;
        }

        static ArgsParser.ArgsParser SetArgsParser(string[] args) {
            var argsParser = new ArgsParser.ArgsParser(args);
            argsParser.AddAllowedOptionWithHelpString("-help", "print this message");
            argsParser.AddAllowedOptionWithHelpString(QuietOption, "pops the UI if the option is not used, otherwise exits");
            argsParser.AddAllowedOptionWithHelpString(BundlingOption, "use edge routing with bundles" );
            argsParser.AddOptionWithAfterStringWithHelp(FileOption, "the name of the input file");
            argsParser.AddOptionWithAfterStringWithHelp(SvgFileNameOption, "the name of the svg output file");
            argsParser.AddOptionWithAfterStringWithHelp(EdgeSeparationOption, "use specified edge separation in edge bundling");
            argsParser.AddAllowedOptionWithHelpString(MdsOption, "use mds layout");
            argsParser.AddAllowedOptionWithHelpString(FdOption, "use force directed layout");
            argsParser.AddOptionWithAfterStringWithHelp(InkImportanceOption, "ink importance coefficient in edge bundling");
            argsParser.AddOptionWithAfterStringWithHelp(TightPaddingOption, "tight padding coefficient in edge bundling");
            argsParser.AddOptionWithAfterStringWithHelp(LoosePaddingOption, "loose padding coefficient in edge bundling");
            argsParser.AddOptionWithAfterStringWithHelp(CapacityCoeffOption, "capacity coeffiecient in edge bundling ");
            argsParser.AddAllowedOptionWithHelpString(AsyncLayoutOption, "run the viewer in the async mode");

            if (!argsParser.Parse()) {
                Console.WriteLine(argsParser.UsageString());
                Environment.Exit(1);
            }
            return argsParser;
        }

        
        
    }
}
