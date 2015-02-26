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
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Routing {
    //this class contains a set of edge geometries, and set of node boundaries, ICurves, that might obstruct the edge routing 
    internal class PreGraph {
        internal List<EdgeGeometry> edgeGeometries;
        internal Set<ICurve> nodeBoundaries;
        internal Rectangle boundingBox;
       
        internal PreGraph(EdgeGeometry[] egs, Set<ICurve> nodeBoundaries) {
            edgeGeometries = new List<EdgeGeometry>(egs);
            this.nodeBoundaries = new Set<ICurve>(nodeBoundaries);
            boundingBox = Rectangle.CreateAnEmptyBox();
            foreach (var curve in nodeBoundaries)
                boundingBox.Add(curve.BoundingBox);
        }

        internal PreGraph() {}

        internal void AddGraph(PreGraph a) {
            edgeGeometries.AddRange(a.edgeGeometries);
            nodeBoundaries += a.nodeBoundaries;
            boundingBox.Add(a.boundingBox);
        }

        internal void AddNodeBoundary(ICurve curve) {
            nodeBoundaries.Insert(curve);
            boundingBox.Add(curve.BoundingBox);
        }
    }
}
