using Microsoft.Msagl.Core.Geometry;

#if TEST_MSAGL && !SHARPKIT
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