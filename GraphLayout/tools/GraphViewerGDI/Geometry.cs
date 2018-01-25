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
using System.Drawing;
using Microsoft.Msagl.Drawing;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
namespace Microsoft.Msagl.GraphViewerGdi {

    /// <summary>
    /// Summary description for Geometry.
    /// </summary>
    internal class Geometry: ObjectWithBox {
        internal DObject dObject;

        internal override BBox Box { get { return bBox; } }

        internal BBox bBox;

        internal Geometry(DObject dObject, BBox box) {
            this.dObject = dObject;
            this.bBox = box;
        }
        internal Geometry(DObject dObject) {
            this.dObject = dObject;

            DNode dNode = dObject as DNode;
            if (dNode != null)
                bBox = dNode.DrawingNode.BoundingBox;
            else {
                DLabel dLabel = dObject as DLabel;
                if (dLabel != null)
                    bBox = dLabel.DrawingLabel.BoundingBox;
            }
        }
    }
}
