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
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Routing
{
    /// <summary>
    /// an utility class to keep different polylines created around a shape
    /// </summary>
    internal class TightLooseCouple
    {
        internal Polyline TightPolyline { get; set; }
        internal Shape LooseShape { get; set; }

        internal TightLooseCouple(){}

        public TightLooseCouple(Polyline tightPolyline, Shape looseShape, double distance)
        {
            TightPolyline = tightPolyline;
            LooseShape = looseShape;
            Distance=distance;
        }
        /// <summary>
        /// the loose polyline has been created with this distance
        /// </summary>
        internal double Distance { get; set; }

        /// <summary>
        /// compare just by TightPolyline
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {  
            if(TightPolyline==null)
                throw new InvalidOperationException();
            return TightPolyline.GetHashCode();
        }
        /// <summary>
        /// compare just by TightPolyline
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var couple = obj as TightLooseCouple;
            if(couple==null)
                return false;
            return TightPolyline == couple.TightPolyline;
        }
        public override string ToString()
        {
            return (TightPolyline == null ? "null" : TightPolyline.ToString().Substring(0,5)) + "," + (LooseShape == null ? "null" : LooseShape.ToString().Substring(0,5));
        }
    }
}