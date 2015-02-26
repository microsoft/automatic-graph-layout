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
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing {
    internal class Corner {
        protected bool Equals(Corner other) {
            return b == other.b && ((a == other.a && c == other.c) || (a == other.c && c == other.a));
        }

        public override int GetHashCode() {
#if !SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=372; SharpKit/Colin: unchecked is not supported
            unchecked {
#endif
                return b.GetHashCode() ^ ((a.GetHashCode() ^ c.GetHashCode())*397);
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

        readonly internal Point a;
        readonly Point b;
        readonly Point c;

        public Corner(Point a, Point b, Point c) {
            this.a = a;
            this.b = b;
            this.c = c;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
            UpdateHashKey();
#endif
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var corner = obj as Corner;
            if (ReferenceEquals(corner,null)) 
                return false;
            return Equals(corner);
        }
    }
}