#if DEBUG && !SILVERLIGHT && !SHARPKIT
using System.Collections.Generic;

namespace Microsoft.Msagl.DebugHelpers {
    ///<summary>
    ///</summary>
    public interface IDebugGraphics {
        /// <summary>
        /// 
        /// </summary>
        IList<DebugShape> Shapes { get; }
        /// <summary>
        /// 
        /// </summary>
        void Clear();
    }
}
#endif