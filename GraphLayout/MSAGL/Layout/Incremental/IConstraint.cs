using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Incremental {
    /// <summary>
    /// A constraint must provide a method to find a feasible starting configuration, 
    /// and a method to satisfy the constraint by moving the affected nodes as little as possible
    /// </summary>
    public interface IConstraint {
        /// <summary>
        /// Satisfy the constraint by moving as little as possible.
        /// <returns>Amount of displacement</returns>
        /// </summary>
        double Project();
        /// <summary>
        /// Get the list of nodes involved in the constraint
        /// </summary>
        IEnumerable<Node> Nodes { get; }
        /// <summary>
        /// Constraints are applied according to a schedule.
        /// Level 0 constraints will be applied at all stages,
        /// Level 1 after a certain number of Level 0 has completed
        /// Level 2 after level 1 and so on.
        /// </summary>
        /// <returns></returns>
        int Level { get; }
    }
}