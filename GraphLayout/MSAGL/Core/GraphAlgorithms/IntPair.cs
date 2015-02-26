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

namespace Microsoft.Msagl.Core.GraphAlgorithms {
    /// <summary>
    /// Represents a couple of integers.
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
#if TEST_MSAGL
    public
#else
    internal
#endif
 sealed class IntPair : IEdge {

        internal int x;
        internal int y;

        /// <summary>
        /// the first element of the pair
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811")]
        public int First {
            get { return x; }
            set
            {
                x = value;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
                UpdateHashKey();
#endif
            }
        }

        /// <summary>
        /// the second element of the pair
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811")]
        public int Second {
            get { return y; }
            set
            {
                y = value;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
                UpdateHashKey();
#endif
            }
        }
        /// <summary>
        ///  the less operator
        /// </summary>
        /// <param name="pair0"></param>
        /// <param name="pair1"></param>
        /// <returns></returns>
        public static bool operator <(IntPair pair0, IntPair pair1) {
            if (pair0 != null && pair1 != null)
                return pair0.x < pair1.x || pair0.x == pair1.x && pair0.y < pair1.y;

            throw new InvalidOperationException();
        }
        /// <summary>
        /// the greater operator
        /// </summary>
        /// <param name="pair0"></param>
        /// <param name="pair1"></param>
        /// <returns></returns>
        public static bool operator >(IntPair pair0, IntPair pair1) {
            return pair1 < pair0;
        }
        /// <summary>
        /// Compares two pairs
        /// </summary>
        /// <param name="pair0"></param>
        /// <param name="pair1"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811")]
        public static int Compare(IntPair pair0, IntPair pair1) {
            if (pair0 < pair1)
                return 1;
            if (pair0 == pair1)
                return 0;

            return -1;
        }

        /// <summary>
        /// override the equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            IntPair other = obj as IntPair;
            if (other == null)
                return false;

            return x == other.x && y == other.y;
        }

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
        private SharpKit.JavaScript.JsString _hashKey;
        private void UpdateHashKey()
        {
            _hashKey = ""+x+","+y;
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            uint hc = (uint)x.GetHashCode();
            return (int)((hc << 5 | hc >> 27) + (uint)y);
        }

        /// <summary>
        /// the constructor
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        public IntPair(int first, int second) {
            this.x = first;
            this.y = second;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
            UpdateHashKey();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return "(" + x + "," + y + ")";
        }

#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203 Explicitly implemented interfaces are not generate without any warning
        public int Source {
#else
        int IEdge.Source {
#endif
            get { return x; }
            set
            {
                x = value;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
                UpdateHashKey();
#endif
            }
        }

#if SHARPKIT //http://code.google.com/p/sharpkit/issues/detail?id=203 Explicitly implemented interfaces are not generate without any warning
        public int Target {
#else
        int IEdge.Target {
#endif
            get { return y; }
            set
            {
                y = value;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
                UpdateHashKey();
#endif
            }
        }
    }
}
