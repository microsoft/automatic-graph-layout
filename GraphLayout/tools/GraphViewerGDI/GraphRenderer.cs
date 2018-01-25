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
using System.Collections.Generic;
using System.Text;
using System.Drawing;
namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// provides an API for drawing in a image
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Renderer")]
    public sealed class GraphRenderer {
        object layedOutGraph;

        Microsoft.Msagl.Drawing.Graph graph; 
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="drGraph"></param>
        public GraphRenderer(Microsoft.Msagl.Drawing.Graph drGraph) {
            this.graph = drGraph;
        }


        /// <summary>
        /// calulates the layout
        /// </summary>
        public void CalculateLayout() {
            using (GViewer gv = new GViewer()) {
                gv.CurrentLayoutMethod = LayoutMethod.UseSettingsOfTheGraph;
                layedOutGraph = gv.CalculateLayout(graph);
            }
        }

        /// <summary>
        /// renders the graph on the image
        /// </summary>
        /// <param name="image"></param>
        public void Render(Image image) {
            if (image != null)
                Render(Graphics.FromImage(image), 0, 0, image.Width, image.Height);
        }

        /// <summary>
        /// Renders the graph inside of the rectangle xleft,ytop, width, height
        /// </summary>
        /// <param name="graphics">the graphics object</param>
        /// <param name="left">left of the rectangle</param>
        /// <param name="top">top of the rectangle</param>
        /// <param name="width">width of the rectangle</param>
        /// <param name="height">height of the rectangle</param>
        public void Render(System.Drawing.Graphics graphics, int left, int top, int width, int height) {
            Render(graphics,new System.Drawing.Rectangle(left,top,width,height));
        }
        /// <summary>
        /// Renders the graph inside of the rectangle
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="rect"></param>
        public void Render(System.Drawing.Graphics graphics, System.Drawing.Rectangle rect) {
            if (graphics != null) {
                if (layedOutGraph == null)
                    CalculateLayout();

                double s = Math.Min(rect.Width / graph.Width, rect.Height / graph.Height);
                double xoffset = rect.Left + 0.5 * rect.Width - s * (graph.Left + 0.5 * graph.Width);
                double yoffset = rect.Top + 0.5 * rect.Height + s * (graph.Bottom + 0.5 * graph.Height);
                using (SolidBrush sb = new SolidBrush(Draw.MsaglColorToDrawingColor(graph.Attr.BackgroundColor)))
                    graphics.FillRectangle(sb, rect);

                using (System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix((float)s, 0, 0, (float)-s, (float)xoffset, (float)yoffset))
                    graphics.Transform = m;

                Draw.DrawPrecalculatedLayoutObject(graphics, layedOutGraph);
            }
        }

    }
}
