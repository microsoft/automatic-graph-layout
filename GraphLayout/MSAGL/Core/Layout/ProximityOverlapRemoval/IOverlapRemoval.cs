using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval {
    /// <summary>
    /// Overlap Removal Interface. All Overlap Removal classes should implement this to unify usage of different methods.
    /// </summary>
    public interface IOverlapRemoval {

        /// <summary>
        /// Settings to be used for the overlap removal. Not all settings have to be used.
        /// </summary>
        /// <param name="settings"></param>
        void Settings(OverlapRemovalSettings settings);
        /// <summary>
        /// Main function which removes the overlap for a given graph and finally sets the new node positions.
        /// </summary>
        void RemoveOverlaps();
        /// <summary>
        /// Method giving the number of needed iterations for the last run. (Runtime statistic)
        /// </summary>
        /// <returns></returns>
        int GetLastRunIterations();
        
    }
}
