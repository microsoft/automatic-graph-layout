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
﻿using System.Collections.Generic;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Layout.Layered {
    internal class EdgeComparerBySource : IComparer<LayerEdge> {
        int[] X;
        internal EdgeComparerBySource(int[] X) {
            this.X = X;
        }

#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203
        //SharpKit/Colin - https://code.google.com/p/sharpkit/issues/detail?id=290
        public int Compare(LayerEdge a, LayerEdge b) {
#else
        int System.Collections.Generic.IComparer<LayerEdge>.Compare(LayerEdge a, LayerEdge b) {
#endif
            ValidateArg.IsNotNull(a, "a");
            ValidateArg.IsNotNull(b, "b");
            int r = X[a.Source] - X[b.Source];
            if (r != 0)
                return r;

            return X[a.Target] - X[b.Target];
        }

    }
}
