#if DEBUG
using System;

namespace Microsoft.Msagl.DebugHelpers {
    ///<summary>
    ///</summary>
    [Serializable]

    public class DebugShape {
        ///<summary>
        ///</summary>
        public int Pen { get; set; }
        ///<summary>
        ///</summary>
        public string Color { get; set; }
        /// <summary>
        /// Filling Color of the Shape.
        /// </summary>
        public string FillColor { get; set;}

    }
}
#endif