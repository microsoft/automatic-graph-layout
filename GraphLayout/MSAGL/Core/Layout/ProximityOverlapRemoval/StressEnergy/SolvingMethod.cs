using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy {
   
    /// <summary>
    /// Determines how the minimum of the quadratic majorant is determined.
    /// </summary>
    public enum SolvingMethod {
        /// <summary>
        /// Localized Optimization where each node is placed on the average of the votes he gets from the other nodes.
        /// </summary>
        Localized,
        /// <summary>
        /// Preconditioned Conjugated Gradient.
        /// </summary>
        PrecondConjugateGradient,
        /// <summary>
        /// Conjugate Gradient without preconditioning.
        /// </summary>
        ConjugateGradient
    }


}
