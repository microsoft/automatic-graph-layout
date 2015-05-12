using Microsoft.Msagl.Core.Geometry;

#if DEBUG && !SILVERLIGHT  && !SHARPKIT
namespace Microsoft.Msagl.DebugHelpers {
    ///<summary>
    ///</summary>
    public class DebugRectangle : DebugShape {
        /// <summary>
        /// 
        /// </summary>
        public Rectangle Rectangle { get; set; }
    }
}
#endif