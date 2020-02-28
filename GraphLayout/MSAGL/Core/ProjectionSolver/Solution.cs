// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Solution.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Projection Solver Solution results.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    /// <summary>
    /// Per-instance results from ProjectionSolver.Solver.Solve().
    /// </summary>
    public class Solution
        : ICloneable
    {
        /// <summary>
        /// The only failure condition is if there are one or more unsatisfiable constraints, such as cycles
        /// or mutually exclusive equality constraints.
        /// </summary>
        public int NumberOfUnsatisfiableConstraints { get; internal set; }

        /// <summary>
        /// The number of times the outer Project/Split loop was run.
        /// </summary>
        public Int32 OuterProjectIterations { get; internal set; }

        /// <summary>
        /// The number of times Project iterated internally; divide by OuterProjectIterations to get the average
        /// inner iterations per outer iteration; see also MinInnerProjectIterations and MaxInnerProjectIterations.
        /// </summary>
        public Int64 InnerProjectIterationsTotal { get; internal set; }

        /// <summary>
        /// The minimum number of times Project iterated internally for any outer Project iterations.
        /// </summary>
        public Int32 MinInnerProjectIterations { get; internal set; }

        /// <summary>
        /// The maximum number of times Project iterated internally for any outer Project iterations.
        /// </summary>
        public Int32 MaxInnerProjectIterations { get; internal set; }

        /// <summary>
        /// The maximum depth of a constraint tree.
        /// </summary>
        public Int32 MaxConstraintTreeDepth { get; internal set; }

        /// <summary>
        /// The final value of the goal function.
        /// </summary>
        public double GoalFunctionValue { get; internal set; }

        /// <summary>
        /// Whether Solve() used the full Qpsc (Quadratic Programming for Separation Constraints) algorithm,
        /// either by default or because UsedParameters.ForceQpsc was set.
        /// </summary>
        public SolverAlgorithm AlgorithmUsed { get; internal set; }

        /// <summary>
        /// If true, the function ended due to TimeLimit being exceeded.
        /// </summary>
        public bool TimeLimitExceeded { get; internal set; }

        /// <summary>
        /// If true, the function ended due to OuterProjectIterationsLimit being exceeded.
        /// </summary>
        public bool OuterProjectIterationsLimitExceeded { get; internal set; }

        /// <summary>
        /// If true, a call to Project ended early due to InnerProjectIterationsLimit being exceeded.
        /// The result may be nonfeasible.
        /// </summary>
        public bool InnerProjectIterationsLimitExceeded { get; internal set; }

        /// <summary>
        /// Indicates whether one or more execution limits were exceeded.
        /// </summary>
        public bool ExecutionLimitExceeded
        {
            get
            {
                return TimeLimitExceeded || OuterProjectIterationsLimitExceeded || InnerProjectIterationsLimitExceeded;
            }
        }

        #region ICloneable members
        /// <summary>
        /// Shallow-copy everything, including the contained list.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion // ICloneable members
    }
}