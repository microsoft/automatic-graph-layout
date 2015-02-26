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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Dot2Graph;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Routing.Spline.Bundling.GeneralBundling;
using Parser = DotParser.Parser;
using Size = System.Drawing.Size;

namespace TestFormForGViewer {
    public class TForm : Form {
        readonly ToolTip tt=new ToolTip();

        public GViewer GViewer { get; set; }

        public TrackBar monotoneBar { get; set; }

        public string lastFileName { get; set; }

        public double MonotoneBarValue {
            get {
                return (2.0 * monotoneBar.Value / (monotoneBar.Maximum - monotoneBar.Minimum));
            }

        }

        internal void SetMonotonicityCoefficientTrackBar() {
            monotoneBar = new TrackBar();
            monotoneBar.Location = new Point(400, 0);
            monotoneBar.Width =  200;
            monotoneBar.Minimum = -100;
            monotoneBar.Maximum = 100;
            monotoneBar.SmallChange = 1;
          
            Controls.Add(monotoneBar);
            monotoneBar.BringToFront();
            monotoneBar.ValueChanged += MonotoneBarValueChanged;
            monotoneBar.MouseUp += MonotoneBarMouseUp;
        }

        void MonotoneBarMouseUp(object sender, MouseEventArgs e) {
            //RerouteWithNewMonotoneCoefficient();
        }

        void RerouteEdges(Graph graph, BundlingSettings bundleSettings) {
            bundleSettings.MonotonicityCoefficient = MonotoneBarValue;
            var iViewer = (IViewer)GViewer;
            foreach (var iEdge in iViewer.Entities.Where(edge => edge is IViewerEdge)) {
                iViewer.InvalidateBeforeTheChange(iEdge);
            } 
            RouteBundledEdges(graph.GeometryGraph, false, graph.LayoutAlgorithmSettings.EdgeRoutingSettings);
            foreach (var iEdge in iViewer.Entities.Where(edge=>edge is IViewerEdge) ){
                iViewer.Invalidate(iEdge);
            }
        }

        void MonotoneBarValueChanged(object sender, EventArgs e) {
            tt.Show(MonotoneBarValue.ToString(), monotoneBar, monotoneBar.Width / 2, -monotoneBar.Height / 2);
            RerouteWithNewMonotoneCoefficient();
        }

        void RerouteWithNewMonotoneCoefficient() {
            var graph = GViewer.Graph;
            if (graph == null)
                return;
            var ers = graph.LayoutAlgorithmSettings.EdgeRoutingSettings;
            var bundleSettings = ers.BundlingSettings;
            if (bundleSettings == null) return;
            if (bundleSettings.MonotonicityCoefficient != MonotoneBarValue) {
                RerouteEdges(graph, bundleSettings);
            }
        }

        public void SetGViewer(GViewer gviewer) {

            GViewer=gviewer;
            SuspendLayout();
            Controls.Add(gviewer);
            gviewer.Dock = DockStyle.Fill;
            gviewer.SendToBack();
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(Screen.PrimaryScreen.WorkingArea.Width,
                                                Screen.PrimaryScreen.WorkingArea.Height);

            var statusStrip = new StatusStrip();
            var toolStribLbl = new ToolStripStatusLabel("test");
            statusStrip.Items.Add(toolStribLbl);
            Controls.Add(statusStrip);
            MainMenuStrip = GetMainMenuStrip();
            Controls.Add(MainMenuStrip);
            SetMonotonicityCoefficientTrackBar();
            ResumeLayout();
            gviewer.GraphChanged += gviewer_GraphChanged;            
        }

        
        void gviewer_GraphChanged(object sender, EventArgs e) {
           var bundlingSettings=GViewer.Graph.LayoutAlgorithmSettings.EdgeRoutingSettings.BundlingSettings;
           if (bundlingSettings != null)
               SetValueForMonotoneBar(bundlingSettings.MonotonicityCoefficient);
        }

        void SetValueForMonotoneBar(double monotonicityCoefficient) {
            double a = (monotonicityCoefficient + 1)/2;
            monotoneBar.ValueChanged -= MonotoneBarValueChanged;
            monotoneBar.Value = (int) (a*(monotoneBar.Maximum - monotoneBar.Minimum) + monotoneBar.Minimum + 0.5);
            monotoneBar.ValueChanged += MonotoneBarValueChanged;
        }

        MenuStrip GetMainMenuStrip() {
            var menu = new MenuStrip();
            menu.Items.Add(FileStripItem());
            return menu;
        }

        ToolStripItem FileStripItem() {
            var item = new ToolStripMenuItem("File");
            item.DropDownItems.Add((ToolStripItem)OpenDotFileItem());
            item.DropDownItems.Add(ReloadDotFileItem());
            return item;
        }

        ToolStripItem ReloadDotFileItem() {
            var item = new ToolStripMenuItem("Reload file");
            item.ShortcutKeys = Keys.F5;
            item.Click += ReloadFileClick;
            return item;
        }

        ToolStripItem OpenDotFileItem() {
            var item = new ToolStripMenuItem("Open file");
            item.ShortcutKeys = Keys.Control | Keys.O;
            item.Click += OpenFileClick;
            return item;
        }
        
        void OpenFileClick(object sender, EventArgs e) {
        
            var openFileDialog = new OpenFileDialog {
                RestoreDirectory = true,
                Filter = " dot files (*.dot)|*.dot|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                ReadGraphFromFile(openFileDialog.FileName, GViewer, false);
        }

        void ReloadFileClick(object sender, EventArgs e) {
            if (lastFileName != null)
                ReadGraphFromFile(lastFileName, GViewer, false);
        }

        public static void ReadGraphFromFile(string fileName, GViewer gViewer, bool verbose) {
            var tform = (TForm)gViewer.ParentForm;
            int eLine, eColumn;
            bool msaglFile;
            Graph graph = CreateDrawingGraphFromFile(fileName, out eLine, out eColumn, out msaglFile);
            tform.lastFileName = fileName;
            if (graph == null)
                MessageBox.Show(String.Format("{0}({1},{2}): cannot process the file", fileName, eLine, eColumn));

            else {

#if REPORTING
                graph.LayoutAlgorithmSettings.Reporting = Test.verbose;
#endif
                gViewer.FileName = fileName;
                Stopwatch sw = null;
                if (verbose) {
                    gViewer.AsyncLayout = false;
                    sw = new Stopwatch();
                    sw.Start();
                }
                
                if(gViewer.Graph!=null)
                    graph.LayoutAlgorithmSettings=gViewer.Graph.LayoutAlgorithmSettings;

                gViewer.Graph = graph;
                if (sw != null) {
                    sw.Stop();
                    Console.WriteLine("layout done for {0} ms", (double)sw.ElapsedMilliseconds / 1000);
                }
            }
        }
        internal static Graph CreateDrawingGraphFromFile(string fileName, out int line, out int column, out bool msaglFile) {
            var sr = new StreamReader(fileName);
            string dotString = sr.ReadToEnd();
            sr.Close();
            string msg;
            var graph = Parser.GraphFromDotString(dotString, out line, out column, out msg);
            if (graph != null) {
                graph.LayoutAlgorithmSettings.EdgeRoutingSettings.KeepSmoothedPolyline = true;
                msaglFile = false;
                return graph;
            }

            try {
                graph = Graph.Read(fileName);
                msaglFile = true;
                return graph;
            } catch (Exception) {
                Console.WriteLine("cannot read " + fileName);
            }
            msaglFile = false;
            return null;
        }

        public static void RouteBundledEdges(GeometryGraph geometryGraph, bool measureTime, EdgeRoutingSettings edgeRoutingSettings) {
            Stopwatch sw=null;
            if (measureTime) {
                sw=new Stopwatch();
                sw.Start();
            }
            BundleRouter br = new BundleRouter(geometryGraph, edgeRoutingSettings.ConeAngle,
                                               edgeRoutingSettings.Padding, edgeRoutingSettings.PolylinePadding, edgeRoutingSettings.BundlingSettings);
            br.Run();
            if (sw != null) {
                sw.Stop();
                Console.WriteLine("bundling takes " + sw.Elapsed);
            }
        }
    }
}