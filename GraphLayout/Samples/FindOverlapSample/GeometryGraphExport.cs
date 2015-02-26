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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace FindOverlapSample {
    class GeometryGraphExport {


//        public void SaveToPNG(GeometryGraph graph, String filename) {
//            Bitmap bitmap = null;
//            var box = graph.BoundingBox;
//            int w = (int) box.Width;
//            int h = (int) box.Height;
//                bitmap = new Bitmap((int)box.Width, (int)box.Height, PixelFormat.Format32bppPArgb);
//            using (Graphics graphics = Graphics.FromImage(bitmap)) {
//                graphics.SmoothingMode = SmoothingMode.HighQuality;
//            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
//            graphics.CompositingQuality = CompositingQuality.HighQuality;
//            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
//
//             //fill the whole image
//            graphics.FillRectangle(new SolidBrush(new System.Drawing.Color()),
//                                   new RectangleF(0, 0, (int)box.Width, (int)box.Height));
//
//                
//            //calculate the transform
//            double s = 1;
//            Graph g = gViewer.Graph;
//            double x = 0.5*w - s*(g.Left + 0.5*g.Width);
//            double y = 0.5*h + s*(g.Bottom + 0.5*g.Height);
//
//            graphics.Transform = new Matrix((float) s, 0, 0, (float) -s, (float) x, (float) y);
//            Draw.DrawPrecalculatedLayoutObject(graphics, gViewer.DGraph);
//            else DrawAll(w, h, graphics);
//            }
//            DrawGeneral(w, h, graphics);
//            }
//
//            AdjustFileName();
//            if (bitmap != null)
//                bitmap.Save(saveInTextBox.Text);
//            
//        }
    }
}
