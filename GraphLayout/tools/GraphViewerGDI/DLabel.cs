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
using System.Drawing;
using Microsoft.Msagl.Drawing;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// A class representing a drawn label
    /// </summary>
    public sealed class DLabel : DObject, IViewerObject {
        DObject parent;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="parentPar"></param>
        /// <param name="label"></param>
        /// <param name="viewer">the viewer containing the label</param>
        public DLabel(DObject parentPar, Label label, GViewer viewer) : base(viewer) {
            parent = parentPar;
            DrawingLabel = label;
            ((IHavingDLabel) parent).Label = this;
            Font = new Font(DrawingLabel.FontName, (int)DrawingLabel.FontSize, (System.Drawing.FontStyle)(int)label.FontStyle);
        }

        /// <summary>
        /// gets the font of the label
        /// </summary>
        public Font Font { get; set; }

        /// <summary>
        /// the object that label belongs to
        /// </summary>
        public DObject Parent {
            get { return parent; }
            set { parent = value; }
        }

        /// <summary>
        /// gets or set the underlying drawing label
        /// </summary>
        public Label DrawingLabel { get; set; }

        #region IViewerObject Members

        /// <summary>
        /// delivers the underlying label object
        /// </summary>
        public override DrawingObject DrawingObject {
            get { return DrawingLabel; }
        }

        #endregion

        internal override float DashSize() {
            return 1; //it is never used
        }

        /// <summary>
        /// 
        /// </summary>
        protected internal override void Invalidate() {}

        /// <summary>
        /// calculates the rendered rectangle and RenderedBox to it
        /// </summary>
        public override void UpdateRenderedBox() {
            var box = DrawingLabel.BoundingBox;
            if (MarkedForDragging) {
                box.Add(DrawingLabel.GeometryLabel.AttachmentSegmentEnd);
                box.Pad(1); //1 is the width of the attachment line on the screen                                
            }
            RenderedBox = box;
        }
    }
}