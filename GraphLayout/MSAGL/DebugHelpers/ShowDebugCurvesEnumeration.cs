#if TEST_MSAGL
using System.Collections.Generic;

namespace Microsoft.Msagl.DebugHelpers {
    ///<summary>
    /// shows shapes 
    ///</summary>
    ///<param name="shapes"></param>
    public delegate void ShowDebugCurvesEnumeration(IEnumerable<DebugCurve> shapes);
}   
#endif