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
//
// RelativeShape.cs
// MSAGL RelativeShape class for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// A shape wrapping an ICurve delegate, providing additional information.
    /// </summary>
    public class RelativeShape : Shape {
        /// <summary>
        /// The curve of the shape.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BoundaryCurve"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "RelativeShape")]
        public override ICurve BoundaryCurve { 
            get { return curveDelegate(); }
            set {
                throw new InvalidOperationException(
#if TEST_MSAGL
                        "Cannot set BoundaryCurve directly for RelativeShape"
#endif // TEST
                    );
            }
        }

        readonly Func<ICurve> curveDelegate;

        /// <summary>
        /// Constructor taking the ID and the curve delegate for the shape.
        /// </summary>
        /// <param name="curveDelegate"></param>
        public RelativeShape(Func<ICurve> curveDelegate)
        {
            this.curveDelegate = curveDelegate;
        }
    }
}
