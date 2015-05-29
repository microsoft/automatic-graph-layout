#if TEST_MSAGL
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.DebugHelpers {
    /// <summary>
    /// shows curves
    /// </summary>
    /// <param name="curves"></param>
    public delegate void Show(params ICurve[] curves);

    ///<summary>
    ///</summary>
    ///<param name="graph"></param>
    public delegate void ShowGraph(GeometryGraph graph);
}
#endif