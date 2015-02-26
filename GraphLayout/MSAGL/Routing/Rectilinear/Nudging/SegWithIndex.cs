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
using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Rectilinear.Nudging
{
    internal class SegWithIndex {
        internal Point[] Points;
        internal int I;//offset
    
        internal SegWithIndex(Point[] pts, int i) {
            Debug.Assert(i<pts.Length&&i>=0);
            Points = pts;
            I = i;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
            UpdateHashKey();
#endif
        }

        internal Point Start {get{return Points[I];}}
        internal Point End{ get{return Points[I+1];}}
    
        override public bool Equals(object obj) {
            var other = (SegWithIndex) obj;
            return other.Points== Points&& other.I == I;
        }

        public override int GetHashCode() {
#if !SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=372; SharpKit/Colin: unchecked is not supported
            unchecked {
#endif
                return (Points.GetHashCode() * 397) ^ I;
#if !SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=372
            }
#endif
        }

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
        private SharpKit.JavaScript.JsString _hashKey;
        private void UpdateHashKey()
        {
            _hashKey = GetHashCode().ToString();
        }
#endif

    }
}