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
    public partial class SaveViewAsImageForm : Form {
        readonly GViewer gViewer;

        internal SaveViewAsImageForm(GViewer gViewerControl) {
            InitializeComponent();
            saveCurrentView.Checked = gViewerControl.SaveCurrentViewInImage;
            saveTotalView.Checked = !gViewerControl.SaveCurrentViewInImage;
            gViewer = gViewerControl;
            CancelButton = cancelButton;
            imageScale.TickStyle = TickStyle.Both;
            imageScale.TickFrequency = 5;
            imageScale.Minimum = 10;
            imageScale.Maximum = 100;
            imageScale.Value = imageScale.Minimum;      
            SetScaleLabelTexts();
            imageScale.ValueChanged += imageScale_ValueChanged;
            saveCurrentView.CheckedChanged += saveCurrentView_CheckedChanged;
            toolTip.SetToolTip(saveInTextBox, "The default file format is JPG");
            saveInTextBox.Text = "*.jpg";
        }


        double ImageScale {
            get {
                double span = imageScale.Maximum - imageScale.Minimum;
                double l = (imageScale.Value - imageScale.Minimum)/span;
                return 1.0 + l*9;
            }
        }

        string FileName {
            get { return saveInTextBox.Text; }
        }

        void saveCurrentView_CheckedChanged(object sender, EventArgs e) {
            SetScaleLabelTexts();
            gViewer.SaveCurrentViewInImage = saveCurrentView.Checked;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
            MessageId = "System.String.Format(System.String,System.Object)")]
        void imageScale_ValueChanged(object sender, EventArgs e) {
            toolTip.SetToolTip(imageScale, String.Format("Image scale is {0}", ImageScale));
            SetScaleLabelTexts();
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
            MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        void SetScaleLabelTexts() {
            int w, h;

            if (saveTotalView.Checked) {
                w = (int) Math.Ceiling(gViewer.Graph.Width*ImageScale);
                h = (int) Math.Ceiling(gViewer.Graph.Height*ImageScale);
            }
            else {
                w = (int) (gViewer.SrcRect.Width*ImageScale);
                h = (int) (gViewer.SrcRect.Height*ImageScale);
            }
            imageSizeLabel.Text = String.Format("Image size : {0} x {1}", w, h);
        }

        void browseButton_Click(object sender, EventArgs e) {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter =
                "JPG Files(*.JPG)|*.JPG|BMP Files(*.BMP)|*.BMP|GIF Files(*.GIF)|*.GIF|Png Files(*.Png)|*.Png|SVG files(*.svg)|*.SVG";
            saveFileDialog.OverwritePrompt = false;
            DialogResult dialogResult = saveFileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK) {
                saveInTextBox.Text = saveFileDialog.FileName;
                okButton.Focus(); //to enable hitting the OK button
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"),
         SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions"),
         SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters",
             MessageId = "System.Windows.Forms.MessageBox.Show(System.String)")]
        void OkButtonClick(object sender, EventArgs e) {
            if (String.IsNullOrEmpty(saveInTextBox.Text)) {
                MessageBox.Show("File name is not set");
                return;
            }

            Cursor c = Cursor;
            Cursor = Cursors.WaitCursor;
            try {
                int w, h;
                if (saveCurrentView.Checked) {
                    w = (int) Math.Ceiling(gViewer.SrcRect.Width*ImageScale);
                    h = (int) Math.Ceiling(gViewer.SrcRect.Height*ImageScale);
                }
                else {
                    w = (int) Math.Ceiling(gViewer.Graph.Width*ImageScale);
                    h = (int) Math.Ceiling(gViewer.Graph.Height*ImageScale);
                }

                Bitmap bitmap = null;
                string ext = GetFileNameExtension();
                if (ext == ".EMF" || ext == ".WMF") DrawVectorGraphics(w, h);
                else if (ext == ".SVG") SvgGraphWriter.Write(gViewer.Graph, FileName, null, null, 4);
                else {
                    bitmap = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                        DrawGeneral(w, h, graphics);
                }

                AdjustFileName();
                if (bitmap != null)
                    bitmap.Save(saveInTextBox.Text);
            }
            catch (Exception ex) {
                MessageBox.Show("Cannot save the image: " + ex.Message);
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
            double s = ImageScale;
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


        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        void AdjustFileName() {
            string ext = GetFileNameExtension();
            if (ext == ".BMP" || ext == ".JPG" || ext == ".GIF" || ext == ".EMF" || ext == ".PNG" || ext == ".WMF" ||
                ext == ".SVG") {
                //do nothing
            }
            else
                saveInTextBox.Text = saveInTextBox.Text.ToLower() + ".png";
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToUpper")]
        string GetFileNameExtension() {
            return Path.GetExtension(FileName).ToUpper();
        }

        void cancelButton_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
