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

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// Constants used by OptimalRectanglePacking
    /// </summary>
    public static class PackingConstants
    {
        /// <summary>
        /// The greeks thought the GoldenRatio was a good aspect ratio: Phi = (1 + Math.Sqrt(5)) / 2
        /// </summary>
        /// <remarks>we also use this internally in our golden section search</remarks>
        public static readonly double GoldenRatio = (1 + Math.Sqrt(5)) / 2;

        /// <summary>
        /// equiv to 1 - (1/Phi) where Phi is the Golden Ratio: i.e. the smaller of the two sections
        /// if you divide a unit length by the golden ratio
        /// </summary>
        internal static readonly double GoldenRatioRemainder = 2 - GoldenRatio;
    }
}
