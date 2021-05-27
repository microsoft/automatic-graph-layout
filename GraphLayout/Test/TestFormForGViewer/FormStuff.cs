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
using System.IO;
using System.Windows.Forms;
using Dot2Graph;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace TestFormForGViewer {
    public class FormStuff {
        public static bool initialLayout;
        static string lastFileName;
        public static GViewer GViewer;

        public static Form CreateOrAttachForm(GViewer gviewer, Form form) {
            GViewer=gviewer;
            if (form == null)
                form = new Form();
            form.SuspendLayout();
            form.Controls.Add(gviewer);
            gviewer.Dock = DockStyle.Fill;
            gviewer.SendToBack();
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Size = new System.Drawing.Size(Screen.PrimaryScreen.WorkingArea.Width,
                              Screen.PrimaryScreen.WorkingArea.Height);
            form.MainMenuStrip = GetMainMenuStrip();
            form.Controls.Add(form.MainMenuStrip);
            form.ResumeLayout();
            form.Load += form_Load;
            return form;
        }

        static void form_Load(object sender,EventArgs e) {
            ((Form) sender).Focus();
        }


        static MenuStrip GetMainMenuStrip() {
            var menu=new MenuStrip();
            menu.Items.Add(FileStripItem());

            return menu;

        }

        static ToolStripItem FileStripItem() {
            var item = new ToolStripMenuItem("File");
            item.DropDownItems.Add((ToolStripItem) OpenDotFileItem());
            item.DropDownItems.Add(ReloadDotFileItem());
            return item;
        }

        static ToolStripItem ReloadDotFileItem() {
            var item = new ToolStripMenuItem("Reload file");
            item.ShortcutKeys = Keys.F5;
            item.Click += ReloadFileClick;
            return item;
        }

        static void ReloadFileClick(object sender, EventArgs e) {
          if(lastFileName!=null)
              ReadGraphFromFile(lastFileName, GViewer, false);
        }

        static ToolStripItem OpenDotFileItem() {
            var item = new ToolStripMenuItem("Open file");
            item.ShortcutKeys = Keys.Control | Keys.O;
            item.Click += OpenFileClick;
            return item;
        }

        static void OpenFileClick(object sender, EventArgs e) {
            
            var openFileDialog = new OpenFileDialog {
                                                        RestoreDirectory = true,
                                                        Filter = " dot files (*.dot)|*.dot|All files (*.*)|*.*"
                                                    };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                ReadGraphFromFile(openFileDialog.FileName, GViewer, false);
        }

        internal static Graph CreateDrawingGraphFromFile(string fileName, out int line, out int column, out bool msaglFile) {
            string msg;
            var graph = Parser.Parse(fileName, out line, out column, out msg);
            if (graph != null) {
                msaglFile = false;
                return graph;
            }

            try {
                graph = Graph.Read(fileName);
                msaglFile = true;
                return graph;
            } catch (Exception) {
                System.Diagnostics.Debug.WriteLine("cannot read " + fileName);
            }
            msaglFile = false;
            return null;
        }


        public static void ReadGraphFromFile(string fileName, GViewer gViewer, bool verbose) {
            int eLine, eColumn;
            bool msaglFile;
            Graph graph = CreateDrawingGraphFromFile(fileName, out eLine, out eColumn, out msaglFile);
            lastFileName = fileName;
            if (graph == null)
                MessageBox.Show(String.Format("{0}({1},{2}): cannot process the file", fileName, eLine, eColumn));

            else {
                
#if TEST_MSAGL
                graph.LayoutAlgorithmSettings.Reporting = Test.verbose;
#endif
                gViewer.FileName = fileName;
                Stopwatch sw = null;
                if (verbose) {
                    gViewer.AsyncLayout = false;
                    sw = new Stopwatch();
                    sw.Start();
                }
                gViewer.Graph = graph;
                if (sw != null) {
                    sw.Stop();
                    System.Diagnostics.Debug.WriteLine("layout done for {0} ms", (double)sw.ElapsedMilliseconds / 1000);
                }
            }
        }
    }
}
