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
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.Geometry.Curves
{
    internal class PolylineIterator:IEnumerator<Point>
    {
        Polyline polyline;

        PolylinePoint currentPolyPoint;

        internal PolylineIterator(Polyline poly)
        {
            this.polyline = poly;
        }

        #region IEnumerator<Point> Members

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=332
        public Point Current
#else
        Point IEnumerator<Point>.Current
#endif
        {
            get { return currentPolyPoint.Point; }
        }

        #endregion

        #region IDisposable Members

#if !SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=71
        public void Dispose()
#else
        void IDisposable.Dispose()
#endif
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get { return currentPolyPoint.Point; }
        }

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=332&thanks=332
        public bool MoveNext()
#else
        bool System.Collections.IEnumerator.MoveNext()
#endif
        {
            if (currentPolyPoint == null)
            {
                currentPolyPoint = polyline.StartPoint;
                return currentPolyPoint != null;
            }
            if(currentPolyPoint==polyline.EndPoint)
                return false;
            currentPolyPoint = currentPolyPoint.Next;
            return true;
        }

        void System.Collections.IEnumerator.Reset()
        {
            currentPolyPoint = null;
        }

        #endregion
    }
}
