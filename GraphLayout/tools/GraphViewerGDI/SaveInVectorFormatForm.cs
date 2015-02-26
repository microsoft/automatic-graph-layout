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
using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using Microsoft.Msagl.Drawing;

namespace Microsoft.Msagl.GraphViewerGdi {
    internal partial class SaveInVectorFormatForm : Form {
        readonly GViewer gViewer;

        internal SaveInVectorFormatForm(GViewer gViewerControl) {
            InitializeComponent();
            saveCurrentView.Checked = gViewerControl.SaveCurrentViewInImage;
            saveTotalView.Checked = !gViewerControl.SaveCurrentViewInImage;
            gViewer = gViewerControl;
            saveCurrentView.CheckedChanged += saveCurrentView_CheckedChanged;
            toolTip.SetToolTip(saveInTextBox, "The default file format is SVG");
            Text = "Save in the SVG format";
            saveInTextBox.Text = "*.svg";
            AcceptButton=okButton;
            CancelButton = cancelButton;            
        }

        string FileName {
            get { return saveInTextBox.Text; }
        }

        void saveCurrentView_CheckedChanged(object sender, EventArgs e) {
            gViewer.SaveCurrentViewInImage = saveCurrentView.Checked;
        }


        void BrowseButtonClick(object sender, EventArgs e) {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Svg files(*.svg)|*.svg|EMF Files(*.emf)|*.emf|WMF Files(*.wmf)|*.wmf";
            saveFileDialog.OverwritePrompt = false;
            DialogResult dialogResult = saveFileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK) {
                saveInTextBox.Text = saveFileDialog.FileName;
                okButton.Focus(); //to enable hitting the OK button
            }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower"),
         SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"),
         SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions"),
         SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters",
             MessageId = "System.Windows.Forms.MessageBox.Show(System.String)")]
        void okButton_Click(object sender, EventArgs e) {
            if (String.IsNullOrEmpty(saveInTextBox.Text)) {
                MessageBox.Show("File name is not set");
                return;
            }


            string ext = Path.GetExtension(FileName).ToLower();

            if (!(ext == ".emf" || ext == ".wmf" || ext == ".svg"))
                saveInTextBox.Text += ".svg";

            Cursor c = Cursor;
            Cursor = Cursors.WaitCursor;
            try {
                if (ext != ".svg") {
                    var w = (int) Math.Ceiling(gViewer.SrcRect.Width);
                    var h = (int) Math.Ceiling(gViewer.SrcRect.Height);

                    DrawVectorGraphics(w, h);
                } else
                    SvgGraphWriter.Write(gViewer.Graph, FileName, null, null, 4);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
                Cursor = c;
                return;
            }
            Cursor = c;
            Close();
        }

        void DrawGeneral(int w, int h, Graphics graphics) {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;


            if (saveCurrentView.Checked) DrawCurrent(graphics);
            else DrawAll(w, h, graphics);
        }

        void DrawAll(int w, int h, Graphics graphics) {
            //fill the whole image
            graphics.FillRectangle(new SolidBrush(Draw.MsaglColorToDrawingColor(gViewer.Graph.Attr.BackgroundColor)),
                                   new RectangleF(0, 0, w, h));

            //calculate the transform
            double s = 1;
            Graph g = gViewer.Graph;
            double x = 0.5*w - s*(g.Left + 0.5*g.Width);
            double y = 0.5*h + s*(g.Bottom + 0.5*g.Height);

            graphics.Transform = new Matrix((float) s, 0, 0, (float) -s, (float) x, (float) y);
            Draw.DrawPrecalculatedLayoutObject(graphics, gViewer.DGraph);
        }

        void DrawCurrent(Graphics graphics) {
            graphics.Transform = gViewer.CurrentTransform();
            graphics.FillRectangle(new SolidBrush(Draw.MsaglColorToDrawingColor(gViewer.Graph.Attr.BackgroundColor)),
                                   gViewer.SrcRect);
            graphics.Clip = new Region(gViewer.SrcRect);
            Draw.DrawPrecalculatedLayoutObject(graphics, gViewer.DGraph);
        }

        void DrawVectorGraphics(int w, int h) {
            Graphics graphics = CreateGraphics();
            IntPtr ipHdc = graphics.GetHdc();

            //Create a new empty metafile from the memory stream 

            Stream outputStream = File.OpenWrite(FileName);
            var MetafileToDisplay = new Metafile(outputStream, ipHdc, EmfType.EmfOnly);

            //Now that we have a loaded metafile, we get rid of that Graphics object

            graphics.ReleaseHdc(ipHdc);

            graphics.Dispose();

            //Reload the graphics object with the newly created metafile.

            graphics = Graphics.FromImage(MetafileToDisplay);


            DrawGeneral(w, h, graphics);

            //Get rid of the graphics object that we created 

            graphics.Dispose();
            outputStream.Close();
        }
    }
}