using Microsoft.Msagl.Core.Geometry.Curves;

#if DEBUG && ! SILVERLIGHT
namespace Microsoft.Msagl.DebugHelpers {
    ///<summary>
    ///</summary>
    public class DebugEllipse : DebugShape {
        /// <summary>
        /// 
        /// </summary>
        public Ellipse Ellipse { get; set; }
    }
}
#endif
