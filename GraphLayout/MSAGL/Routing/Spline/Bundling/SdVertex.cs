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
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    internal class SdVertex {
        internal VisibilityVertex VisibilityVertex;
        internal List<SdBoneEdge> InBoneEdges = new List<SdBoneEdge>();
        internal List<SdBoneEdge> OutBoneEdges = new List<SdBoneEdge>();

        internal SdVertex Prev {
            get {
                if (PrevEdge == null) return null;
                return PrevEdge.Source == this ? PrevEdge.Target : PrevEdge.Source;
            }
        }

        internal SdBoneEdge PrevEdge { get; set; }

        internal SdVertex(VisibilityVertex visibilityVertex) {
            VisibilityVertex = visibilityVertex;
        }

        internal CdtTriangle Triangle;

        internal bool IsSourceOfRouting { get; set; }

        internal bool IsTargetOfRouting { get; set; }

        internal Point Point { get { return VisibilityVertex.Point; } }

        double cost;

        internal double Cost {
            get {
                if (IsSourceOfRouting) return cost;
                return Prev == null ? double.PositiveInfinity : cost;
            }
            set { cost = value; }
        }

        public void SetPreviousToNull() {
            PrevEdge = null;
        }
    }
}