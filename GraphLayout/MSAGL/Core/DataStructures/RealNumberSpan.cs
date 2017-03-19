using System;

namespace Microsoft.Msagl.Core.DataStructures {
    /// <summary>
    /// this class behaves like one dimensional bounding box
    /// </summary>
    public class RealNumberSpan{
        internal RealNumberSpan(){
            IsEmpty = true;
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