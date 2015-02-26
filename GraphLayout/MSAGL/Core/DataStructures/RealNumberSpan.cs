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

namespace Microsoft.Msagl.Core.DataStructures {
    /// <summary>
    /// this class behaves like one dimensional bounding box
    /// </summary>
    public class RealNumberSpan{
        internal RealNumberSpan(){
            IsEmpty = true;
        }

        internal bool Intersects(RealNumberSpan a) {
            return !(a.Max < Min || a.Min > Max);
        }

        internal bool IsEmpty { get; set; }

        internal void AddValue(double x){
            if(IsEmpty){
                Min = Max = x;
                IsEmpty = false;
            } else if(x < Min)
                Min = x;
            else if(x > Max)
                Max = x;
        }

        internal double Min { get; set; }
        internal double Max { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double Length{
            get { return Max-Min; }
        }
#if TEST_MSAGL
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        public override string ToString() {
            return IsEmpty ? "empty" : String.Format("{0},{1}", Min, Max);
        }
#endif
    }
}