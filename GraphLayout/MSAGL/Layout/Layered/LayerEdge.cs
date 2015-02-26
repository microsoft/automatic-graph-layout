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

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// This is an edge going down only one layer.
    /// It points to the original edge that can pass several layers
    /// 
    /// </summary>
#if TEST_MSAGL
    public
#else
    internal
#endif
 class LayerEdge {
        internal int Weight = 1;
        internal LayerEdge(int source, int target, int crossingWeight, int weight) {
            this.Source = source;
            this.Target = target;
            this.crossingWeight = crossingWeight;
            Weight = weight;
        }
        internal LayerEdge(int source, int target, int crossingWeight):this(source,target,crossingWeight,1) {            
        }
        int source;
        /// <summary>
        /// the source
        /// </summary>
        public int Source {
            get {
                return source;
            }
            set {
                source = value;
            }
        }

        int target;
        /// <summary>
        /// the target
        /// </summary>
        public int Target {
            get {
                return target;
            }
            set {
                target = value;
            }
        }
/// <summary>
/// overrides the equlity
/// </summary>
/// <param name="obj"></param>
/// <returns></returns>
        public override bool Equals(object obj) {
            LayerEdge ie = obj as LayerEdge;
            return ie.source == this.source && ie.target == this.target;
        }

        /// <summary>
        /// overrides GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            uint hc = (uint)source.GetHashCode();
            return (int)((hc << 5 | hc >> 27) + (uint)target);
        }

        int crossingWeight=1;
        /// <summary>
        /// it is equalt to the number of edges this edge represents 
        /// </summary>
        public int CrossingWeight {
            get { return crossingWeight; }
//            set { crossingWeight = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}->{1}", this.source, this.target);
        }
    }
}
