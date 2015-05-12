using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy {

    /// <summary>
    /// Determines whether the given fixed number of iterations is used, or whether this number should be computed from the problem size.
    /// </summary>
    public enum MaxIterationMethod {
        /// <summary>
        /// Maximal number of iterations is fixed independent of problem size.
        /// </summary>
        FixedMax,
        /// <summary>
        /// Maximal number of iterations is determined as Ceil(sqrt(problemSize))
        /// </summary>
        SqrtProblemSize,

        /// <summary>
        /// ProblemSize is taken as maximal number of iterations
        /// </summary>
        LinearProblemSize

    }
}
