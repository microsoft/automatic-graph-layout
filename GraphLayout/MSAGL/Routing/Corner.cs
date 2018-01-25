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