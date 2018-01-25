using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Core.Layout {
  /// <summary>
  /// restricts the access to a port
  ///  </summary>
  public interface IPortEntry {
    /// <summary>
    /// returns the points nearby the middle of the entries
    /// </summary>
    /// <returns></returns>      
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
    IEnumerable<Point> GetEntryPoints();
  }
}
