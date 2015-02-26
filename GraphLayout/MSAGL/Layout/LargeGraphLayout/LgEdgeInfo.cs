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
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// 
    /// </summary>
    public class LgEdgeInfo:LgInfoBase {
#if DEBUG && TEST_MSAGL
        /// <summary>
        /// to string for debugging
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return String.Format("zoom lvl={0:F2}", ZoomLevel);
        }
#endif
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edge"></param>
        public LgEdgeInfo(Edge edge) {
            Edge = edge;
            ZoomLevel = int.MaxValue;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public Edge Edge { get; set; }

//        /// <summary>
//        /// those need to be set to correctly draw an edge of the level: ActiveGeometries include ICurves and Arrowheads ( labels todo?)
//        /// </summary>
//        public List<EdgePartialGeometryOnLevel> EdgeGeometriesByLevels; //EdgeGeometriesByLevels[i] corresponds to level i, 

    }    
}