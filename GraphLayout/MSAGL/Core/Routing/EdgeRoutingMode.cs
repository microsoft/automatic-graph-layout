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

namespace Microsoft.Msagl.Core.Routing {
    /// <summary>
    /// defines the way edges are routed
    /// </summary>
    public enum EdgeRoutingMode {
        /// <summary>
        /// routing splines over tangent visibility graph edge as a sequence of Bezier segments 
        /// </summary>
        Spline,
        /// <summary>
        /// drawing ordered bundles
        /// </summary>
        SplineBundling,
        /// <summary>
        /// draw edges as straight lines 
        /// </summary>
        StraightLine,
        /// <summary>
        /// inside of Sugiyama algorithm use the standard spline routing
        /// </summary>
        SugiyamaSplines,
        /// <summary>
        /// rectilinear edge routing
        /// </summary>
        Rectilinear,
        /// <summary>
        /// rectilinear but not checking for the optimal port and routing just to the node centers
        /// </summary>
        RectilinearToCenter,
        /// <summary>
        /// means no routing should be done
        /// </summary>
        None
    }
}