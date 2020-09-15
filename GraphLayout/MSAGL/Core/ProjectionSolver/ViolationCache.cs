// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViolationCache.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for caching of most-violated constraints for Projection Solver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// Remove this from project build and uncomment here to selectively enable per-class.
//#define VERBOSE

#if VERIFY || VERBOSE
    // Ensure accuracy.
    #define VERIFY_MIN_CONSTRAINT
#else
    //#define VERIFY_MIN_CONSTRAINT
#endif // VERIFY || VERBOSE

//#define Inline_Violation

using System;
using System.Diagnostics;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    // The ViolationCache stores the top N maximum violations initially, allowing
    // a reduction in the number of times we do a full search of all constraints.
    // (It is not guaranteed to retain the max-N violations strictly after the first
    // block is processed following a cache fill, but the approximation is sufficient
    // to provide significant benefit).

    internal struct ViolationCache
    {
        // Minimize blocks/variables traversed when Project() calls GetMaxViolatedConstraint by repeating the
        // search over the block just modified on the next pass and then checking for out-of-block constraints
        // that are higher.  This repeats until constraints is empty and then it's refilled and repeats.
        // There is a shutoff in Project() for this when we get to a point where it's faster to go with the
        // "enumerate all constraints" approach.
        // Note: A priority queue might be better for larger s_cMaxConstraints, but we use a small value,
        // and we need arbitrary-index removal/replacement also.  Profiling is not showing the cache to be
        // taking noticeable time at 20 items and we don't seem to gain cache hits past that.
        private Constraint[] constraints;

        // Must be >= 2 for Insert() dblNextLowVio logic; > 20 seems to yield little increase in hits.
        internal const int MaxConstraints = 20;

        // Number of constraints actually present.
        private int numConstraints;

        // The lowest violation in the cache.
        internal double LowViolation { get; private set; }

        internal bool IsFull
        {
            get { return this.numConstraints == MaxConstraints; }
        }

        internal void Clear()
        {
            LowViolation = 0.0;
            this.numConstraints = 0;
            if (null == this.constraints)
            {
                this.constraints = new Constraint[MaxConstraints];
            }
        }

        internal bool FilterBlock(Block blockToFilter)
        {
            // Note: The cache does not try to retain strict accordance with highest violation.
            // Doing so lowers the hit rate, probably because if LastModifiedBlock has enough variables,
            // then it has enough high violations to flush all other blocks out of the cache, and
            // thus the next call to FilterBlock removes all for the current block (which per the following
            // paragraph results in calling SearchAllConstraints).  As it turns out, it doesn't
            // really matter what order we process the constraints in, other than the perf benefit of
            // doing the largest violations first, so using the max violation in LastModifiedBlock in this
            // situation seems to be good enough to win the tradeoff.
            //
            // If it becomes necessary to maintain strict "cache always contains the highest violations"
            // compliance, then we would have to return false if the filtering removed all elements of
            // the cache, because then we wouldn't know if there were any non-blockToFilter-related constraints
            // with a higher violation (currently we return true in that case because it is good enough to know
            // there is a good chance that this is true).  Also, SearchViolationCache would need a verification in
            // at least VERIFY mode to verify there are no higher violations in allConstraints.

            // Iterate in reverse to remove constraints belonging to LastModifiedBlock.
            // Note:  Enumerators and .Where are not used because they are much slower.
            LowViolation = double.MaxValue;
            bool fRet = this.numConstraints > 0;
            for (int ii = this.numConstraints - 1; ii >= 0; --ii)
            {
                var constraint = this.constraints[ii];

                // Also remove any constraint that may have been activated by MergeBlocks or marked unsatisfiable
                // by Block.Expand.
                if ((constraint.Left.Block == blockToFilter) || (constraint.Right.Block == blockToFilter) || constraint.IsActive || constraint.IsUnsatisfiable)
                {
                    // If there are any items after this one, then they are ones we want to keep,
                    // so swap in the last one in the array before decrementing the count.
                    if (ii < (this.numConstraints - 1))
                    {
                        this.constraints[ii] = this.constraints[this.numConstraints - 1];
                    }
                    --this.numConstraints;
                }
                else
                {
#if Inline_Violation
                    double violation = constraint.Violation;      // Inline_Violation
#else  // Inline_Violation
                    double violation = (constraint.Left.ActualPos * constraint.Left.Scale)
                                        + constraint.Gap
                                        - (constraint.Right.ActualPos * constraint.Right.Scale);
                    Debug.Assert(constraint.Violation == violation, "LeftConstraints: constraint.Violation must == violation");
#endif // Inline_Violation
                    if (violation < LowViolation)
                    {
                        LowViolation = violation;
                    }
                }
            }
            if (0 == this.numConstraints)
            {
                LowViolation = 0.0;
            }
            return fRet;
        }

        // Find the highest constraint with a greater violation than targetViolation.
        internal Constraint FindIfGreater(double targetViolation)
        {
            Constraint maxViolatedConstraint = null;
            for (int ii = 0; ii < this.numConstraints; ++ii)
            {
                var constraint = this.constraints[ii];
#if Inline_Violation
                double violation = constraint.Violation;      // Inline_Violation
#else  // Inline_Violation
                double violation = (constraint.Left.ActualPos * constraint.Left.Scale)
                                    + constraint.Gap
                                    - (constraint.Right.ActualPos * constraint.Right.Scale);
                Debug.Assert(constraint.Violation == violation, "constraint.Violation must == violation");
#endif // Inline_Violation
                if (violation > targetViolation)
                {
                    targetViolation = violation;
                    maxViolatedConstraint = constraint;
                }
            }

            // Remains null if none was found.
            return maxViolatedConstraint;
        }

        internal void Insert(Constraint constraintToInsert, double insertViolation)
        {
            // This should be checked by the caller (instead of here, for perf reasons).
            Debug.Assert(constraintToInsert.Violation > LowViolation, "constraintToInsert.Violation must be > LowViolation");
            Debug.Assert(constraintToInsert.Violation == insertViolation, "constraintToInsert.Violation must == insertViolation");

            int indexOfLowestViolation = 0;
            double lowViolation = insertViolation;
            double nextLowViolation = insertViolation;

            for (int ii = 0; ii < this.numConstraints; ++ii)
            {
                var constraint = this.constraints[ii];

#if Inline_Violation
                double cacheViolation = constraint.Violation;      // Inline_Violation
#else  // Inline_Violation
                double cacheViolation = (constraint.Left.ActualPos * constraint.Left.Scale)
                                    + constraint.Gap
                                    - (constraint.Right.ActualPos * constraint.Right.Scale);
                Debug.Assert(constraint.Violation == cacheViolation, "constraint.Violation must == cacheViolation");
#endif // Inline_Violation
                if (cacheViolation < lowViolation)
                {
                    // If we don't replace an existing block pair, then we'll replace the lowest
                    // violation in the cache, so will need to know the next-lowest violation.
                    nextLowViolation = lowViolation;

                    indexOfLowestViolation = ii;
                    lowViolation = cacheViolation;
                }
                else if (cacheViolation < nextLowViolation)
                {
                    nextLowViolation = cacheViolation;
                }
            } // endfor each constraint

            // If the cache isn't full yet, add the new one, else replace the lowest violation in the list.
            if (!IsFull)
            {
                // Add to the cache.
                this.constraints[this.numConstraints++] = constraintToInsert;
                if (IsFull)
                {
                    this.LowViolation = lowViolation;
                }
            }
            else
            {
                // Replace in the cache.
                this.constraints[indexOfLowestViolation] = constraintToInsert;
                this.LowViolation = nextLowViolation;
            }

#if VERIFY_MIN_CONSTRAINT
            VerifyMinConstraint();
#endif // VERIFY_MIN_CONSTRAINT
        } // end Insert()

#if VERIFY_MIN_CONSTRAINT
        // Dev-time routine only.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
        private void VerifyMinConstraint()
        {
            // Verify the above optimized lowest-violation tracking.  This also catches
            // any bad indexing that leaves a null or stale constraint.
            if (MaxConstraints == this.numConstraints)
            {
                double lowViolation = this.constraints[0].Violation;
                for (int ii = 1; ii < this.numConstraints; ++ii)
                {
                    if ((null == this.constraints[ii]) || (this.constraints[ii].Violation <= 0.0))
                    {
                        throw new InvalidOperationException(
#if TEST_MSAGL
                                "NULL or stale constraint found in cache"
#endif // TEST_MSAGL
                            );
                    }
                    if (this.constraints[ii].Violation < lowViolation)
                    {
                        lowViolation = this.constraints[ii].Violation;
                    }
                }
                if (lowViolation != this.LowViolation)
                {
                    throw new InvalidOperationException(
#if TEST_MSAGL
                            "Mismatched Low Violation"
#endif // TEST_MSAGL
                        );
                }
            }
        }
#endif // VERIFY_MIN_CONSTRAINT

#if CACHE_STATS
        internal struct CacheStats
        {
            internal int NumberOfCalls;
            internal int NumberOfHits;
            internal void Clear()
            {
                this.NumberOfCalls = 0;
                this.NumberOfHits = 0;
            }
            internal void Print()
            {
                System.Diagnostics.Debug.WriteLine("GetMaxViolationCalls = {0}; ViolationCacheHits = {1}", this.NumberOfCalls, this.NumberOfHits);
            }
        } // end struct CacheStats
#endif // CACHE_STATS
    }
}

