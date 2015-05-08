#if TEST_MSAGL
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Layout.Layered;

namespace Microsoft.Msagl.DebugHelpers {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="db"></param>
    /// <param name="curves"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "db")]
    public delegate void ShowDatabase(Database db, params ICurve[] curves);
}
#endif