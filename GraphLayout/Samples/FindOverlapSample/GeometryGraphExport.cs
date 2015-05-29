using System;
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
