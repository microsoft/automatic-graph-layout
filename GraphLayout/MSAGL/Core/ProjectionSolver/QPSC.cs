// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Qpsc.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Gradient Projection implementation in Projection Solver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// Remove this from project build and uncomment here to selectively enable per-class.
//#define VERBOSE

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    // An instance of Qpsc drives the gradient-projection portion of the Projection Solver.
    internal class Qpsc
    {
        private readonly Parameters solverParameters;

        //
        // This class tracks closely to the Ipsep_Cola paper's solve_QPSC function, outside of
        // the SplitBlocks() and Project() operations.  The relevant data are extracted from the
        // Variables of the solver and placed within the mxA (Hessian A) matrix and vecWiDi (b)
        // vector on initialization, and then the vecPrevX (x-hat), vecCurX (x-bar), and vecDeltaX
        // (d) vectors are filled and the normal matrix operations proceed as in the paper.
        //
        // From the Ipsep paper: Qpsc solves a system with a goal function that includes minimization of
        // distance between pairs of neighbors as well as minimization of distances necessary
        // to satisfy separation constraints.  I.e., our goal function is:
        //     f(X) = Sum(i < n) w_i (x_i - a_i)^2 
        //          + Sum(i,j in Edge list) w_ij (x_i - x_j)^2
        // Where 
        //     X is the vector of CURRENT axis positions (x_i)
        //     a_i is the desired position for each x_i (called d in the paper)
        //     w_ij is the weight of each edge between i and j (possibly multiple edges between
        //          the same i/j pair)
        // Now we can write f(x) = x'Ax.
        // The gradient g at X is computed:
        //     g = Ax+b
        // where b is the negative of the vector (so it becomes Ax-b)
        //     [w_0*d_0 ... w_n-1*d_n-1]
        // and the optimal stepsize to reduce f in the direction d is:
        //     alpha = d'g / (g'Ag)
        // In order to compute any of these efficiently we need an expression for the i'th
        // term of the product of our sparse symmetric square matrix A and a vector v.
        // Now the i'th term of Av is the inner product of row i of A and the vector v,
        // i.e. A[i] is the row vector:
        //     Av[i] = A[i] * v = A[i][0]*v[0] + A[i][1]*v[1] + ... +A[i][n-1]*v[n-1]
        // So what are A[i][0]...A[i][n-1]?
        //     First the diagonal entries: A[i][i] = wi + Sum(wij for every neighbor j of i).
        //     Then the off diagonal entries: A[i][j] = -Sum(wij for each time j is a neighbor of i).
        //     And all A[i][k] where there is no neighbor relation between i and k is 0.
        //     Then, because this is the partial derivative wrt x, each cell is multiplied by 2.
        // Thus for the small example of 3 variables i, j, and k, with neighbors ij and ik,
        // the A matrix is (where w is the weight of the variable or neighbor pair):
        //             i               j              k
        //       +-----------------------------------------
        //     i | 2(wi+wij+wik)      -2wij          -2wik
        //     j |   -2wij          2(wj+wij)          0
        //     k |   -2wik             0           2(wk+wik)
        //
        // Because A is sparse, each row is represented as an array of the A[i][0..n] entries
        // that are nonzero.

        // The foregoing was updated to the Diagonal scaling paper:
        //      Constrained Stress Majorization Using Diagonally Scaled Gradient Projection.pdf
        // which is also checked into the project.  The implementation here is somewhat modified from it:
        //
        //      We store the offset o[i] in the variable.
        //      The position for a variable i in a block B is:
        //          y[i] = (S[B] * Y[B] + o[i])/s[i]
        //
        //      Then the df/dv[i] is:
        //          df/dv[i] = 2 * w[i] * ( y[i] - d[i] );
        //
        //      And comp_dfdv(i , AC, ~c) is a bit different too:
        //          Dfdv = df/dv[i]
        //          For each c in AC s.t. i=lv[c] and c!= ~c:
        //              Lambda[c] = comp_dfdv(rv[c], AC, c)
        //              dfdv += Lambda[c] * s[lv[c]] 
        //          For each c in AC s.t. i=rv[c] and c!= ~c:
        //              Lambda[c] = comp_dfdv(lv[c], AC, c)
        //              dfdv -= Lambda[c] * s[rv[c]]
        //
        //      The statistics for the blocks are calculated as follows: 
        //          For each variable i we have:
        //                a[i] = S[B] / s[i]
        //                b[i] = o[i] / s[i]
        //      Then:
        //          AD[B] = sum over i=0..n:  a[i] * d[i] * w[i]
        //          AB[B] = sum over i=0..n:  a[i] * b[i] * w[i]
        //          A2[B] = sum over i=0..n:  a[i] * a[i] * w[i]
        //      And the ideal position calculation for the block is then the same as the paper:
        //          Y[B] = (AD[B] - AB[B])/A2[B]

        // A MatrixCell implements A[i][j] as above.
        struct MatrixCell
        {
            // Initially the summed weights of all variables to which this variable has a relationship
            // (including self as described above for the diagonal), then modified with scale.
            internal double Value;

            // The index of the variable j for this column in the i row (may be same as i).
            internal readonly uint Column;

            internal MatrixCell(double w, uint index)
            {
                Value = w;
                Column = index;
            }
        }

        // Store original weight to be restored when done.  With the ability to re-Solve() after
        // updating constraint gaps, we must restore DesiredPos as well.
        private struct QpscVar
        {
            internal readonly Variable Variable;
            internal readonly double OrigWeight;
            internal readonly double OrigScale;
            internal readonly double OrigDesiredPos;
            internal QpscVar(Variable v)
            {
                Variable = v;
                OrigWeight = v.Weight;
                OrigScale = v.Scale;
                OrigDesiredPos = Variable.DesiredPos;
            }
        }

        //
        // SolveQpsc static data members:  These do not change after initialization.
        // The matrix is sparse in columns for each row, but not in rows, because there is always
        // at least one entry per row, the diagonal.
        //
        private readonly MatrixCell[][] matrixQ;            // Sparse matrix A in the Ipsep paper; matrix Q (modified to Q') in the Scaling paper
        private readonly double[] vectorWiDi;               // b (weight * desiredpos) in the Ipsep paper; modified to b' = Sb from the Scaling paper
        private readonly QpscVar[] vectorQpscVars;          // List of variables, for perf (avoid nested block/variable List<> iteration)
        private readonly List<MatrixCell> newMatrixRow = new List<MatrixCell>();       // current row in AddVariable

        //
        // SolveQpsc per-iteration data members:  These change with each iteration.
        //
        private readonly double[] gradientVector;           // g in the paper
        private readonly double[] vectorQg;                 // Qg in the paper
        private readonly double[] vectorPrevY;              // y-hat in the paper
        private readonly double[] vectorCurY;               // y-bar in the paper
        private bool isFirstProjectCall;                    // If true we're on our first call to Project

        // Holds the value of f(x) = yQ'y + b'y as computed on the last iteration; used to test for
        // convergence and updated before HasConverged() returns.
        private double previousFunctionValue = double.MaxValue;

#if VERIFY || VERBOSE
        // Verify that |d| <= |sg|
        double normAlphaG;
#endif // VERIFY || VERBOSE

        internal Qpsc(Parameters solverParameters, int cVariables)
        {
            this.solverParameters = solverParameters;

            this.matrixQ = new MatrixCell[cVariables][];
            this.vectorWiDi = new double[cVariables];
            this.vectorQpscVars = new QpscVar[cVariables];

            this.gradientVector = new double[cVariables];
            this.vectorQg = new double[cVariables];
            this.vectorPrevY = new double[cVariables];
            this.vectorCurY = new double[cVariables];
        }

        //
        // solver.SolveQpsc drives the Qpsc instance as follows:
        // Initialization:
        //    Qpsc qpsc = new Qpsc(numVariables);
        //    foreach (variable in (foreach block))
        //       qpsc.AddVariable(variable)
        //    qpsc.VariablesComplete()
        // Per iteration:
        //    if (!qpsc.PreProject()) break;
        //    solver.SplitBlocks()
        //    solver.Project()
        //    if (!qpsc.PostProject()) break;
        // Done:
        //    qpsc.ProjectComplete()

        internal void AddVariable(Variable variable)
        {
            Debug.Assert((null == this.matrixQ[variable.Ordinal]) && (null == this.vectorQpscVars[variable.Ordinal].Variable), "variable.Ordinal already exists");
            this.isFirstProjectCall = true;

            // This is the weight times desired position, multiplied by 2.0 per the partial derivative.
            // We'll use this to keep as close as possible to the desired position on each iteration.
            this.vectorWiDi[variable.Ordinal] = -2.0 * variable.Weight * variable.DesiredPos;

            // Temporarily hijack vectorPrevY for use as scratch storage, to handle duplicate
            // neighbor pairs (take the highest weight).
#if VERIFY
            // Ensure that we've zero'd out all entries after the prior iteration.
            foreach (double dbl in this.vectorPrevY)
            {
                Debug.Assert(0.0 == dbl, "Unexpected nonzero value in vectorPrevY");
            }
#endif // VERIFY

            // Sum the weight for cell i,i (the diagonal).
            this.vectorPrevY[variable.Ordinal] = variable.Weight;
            if (null != variable.Neighbors)
            {
                foreach (var neighborWeightPair in variable.Neighbors)
                {
                    // We should already have verified this in AddNeighbourPair.
                    Debug.Assert(neighborWeightPair.Neighbor.Ordinal != variable.Ordinal, "self-neighbors are not allowed");

                    // For the neighbor KeyValuePairs, Key == neighboring variable and Value == relationship
                    // weight.  If we've already encountered this pair then we'll sum the relationship weights, under
                    // the assumption the caller will be doing something like creating edges for different reasons,
                    // and multiple edges should be like rubber bands, the sum of the strengths.  Mathematica also
                    // sums duplicate weights.

                    // Per above comments:
                    //     First the diagonal entries: A[i][i] = wi + Sum(wij for every neighbor j of i).
                    this.vectorPrevY[variable.Ordinal] += neighborWeightPair.Weight;

                    //     Then the off diagonal entries: A[i][j] = -Sum(wij for time j is a neighbor of i).
                    this.vectorPrevY[neighborWeightPair.Neighbor.Ordinal] -= neighborWeightPair.Weight;
                }
            } // endif null != variable.Neighbors

            // Add the sparse row to the matrix (all non-zero slots of vectorPrevY are weights to that neighbor).
            for (uint ii = 0; ii < this.vectorPrevY.Length; ++ii)
            {
                if (0.0 != this.vectorPrevY[ii])
                {
                    // The diagonal must be > 0 and off-diagonal < 0.
                    Debug.Assert((ii == variable.Ordinal) == (this.vectorPrevY[ii] > 0.0), "Diagonal must be > 0.0");

                    // All 'A' cells must be 2*(summed weights).
                    this.newMatrixRow.Add(new MatrixCell(this.vectorPrevY[ii] * 2.0, ii));
                    this.vectorPrevY[ii] = 0.0;
                }
            }
            this.matrixQ[variable.Ordinal] = this.newMatrixRow.ToArray();
            this.newMatrixRow.Clear();

            this.vectorQpscVars[variable.Ordinal] = new QpscVar(variable);

            // For the non-Qpsc loop, we consider weights in block reference-position calculation.
            // Here, we have that in vectorWiDi which we use in calculating gradient and alpha, which
            // in turn we use to set the gradient-stepped desiredPos.  So turn it off for the duration
            // of Qpsc - we restore it in QpscComplete().
            variable.Weight = 1.0;
        } // end AddVariable()

        internal void VariablesComplete()
        {
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Q and b (before scaling)...");
            DumpMatrix();
            DumpVector("WiDi", vectorWiDi);
#endif // VERBOSE

            foreach (var qvar in this.vectorQpscVars)
            {
                var variable = qvar.Variable;
                foreach (var cell in this.matrixQ[variable.Ordinal])
                {
                    if (cell.Column == variable.Ordinal)
                    {
                        if (solverParameters.Advanced.ScaleInQpsc)
                        {
                            variable.Scale = 1.0 / Math.Sqrt(Math.Abs(cell.Value));
                            if (double.IsInfinity(variable.Scale))
                            {
                                variable.Scale = 1.0;
                            }

                            // This is the y = Sx step from the Scaling paper.
                            variable.ActualPos /= variable.Scale;

                            // This is the b' <- Sb step from the Scaling paper
                            this.vectorWiDi[variable.Ordinal] *= variable.Scale;
                        }

                        // This is needed for block re-initialization.
                        this.vectorCurY[variable.Ordinal] = variable.ActualPos;
                        variable.DesiredPos = variable.ActualPos;
                    }
                }
            }

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Qpsc.VariablesComplete Variable scales and values");
            System.Diagnostics.Debug.WriteLine(" ordinal     origDesired     desired     scale       unscaled actualpos      scaled actualpos");
            foreach (var qvar in vectorQpscVars)
            {
                System.Diagnostics.Debug.WriteLine("  {0} {1} {2} {3} {4} {5}",
                                    qvar.Variable.Ordinal,
                                    qvar.OrigDesiredPos, qvar.Variable.DesiredPos,
                                    qvar.Variable.Scale,
                                    qvar.Variable.ActualPos / qvar.Variable.Scale, qvar.Variable.ActualPos);
            }
#endif // VERBOSE

            if (!solverParameters.Advanced.ScaleInQpsc)
            {
                return;
            }

            // Now convert mxQ to its scaled form S#QS (noting that the transform of a diagonal matrix S is S
            // so this is optimized), and we've made the S matrix such that Q[i][i] is 1.  The result is in-place
            // conversion of Q to scaledQ s.t.
            //   for all ii
            //      for all jj
            //         if ii == jj, scaledQ[ii][jj] = 1
            //         else         scaledQ[ii][jj] = Q[ii][jj] * var[ii].scale * var[jj].scale
            ////
            for (int rowNum = 0; rowNum < this.matrixQ.Length; ++rowNum)
            {
                var row = this.matrixQ[rowNum];
                for (int sparseCol = 0; sparseCol < row.Length; ++sparseCol)
                {
                    if (row[sparseCol].Column == rowNum)
                    {
                        row[sparseCol].Value = 1.0;
                    }
                    else
                    {
                        // Diagonal on left scales rows [SQ], on right scales columns [QS].
                        row[sparseCol].Value *= this.vectorQpscVars[rowNum].Variable.Scale
                                                * this.vectorQpscVars[row[sparseCol].Column].Variable.Scale;
                    }
                }
            }
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Q' and b' (after scaling)...");
            DumpMatrix();
            DumpVector("WiDi", vectorWiDi);
#endif // VERBOSE
        } // end VariablesComplete()

        // Called by SolveQpsc before the split/project phase.  Returns false if the difference in the
        // function value on the current vs. previous iteration is sufficiently small that we're done.
        // @@PERF: Right now this is distinct matrix/vector operations.  Profiling shows most time
        // in Qpsc is taken by MatrixVectorMultiply.  We could gain a bit of performance by combining
        // some things but keep it simple unless that's needed.
        internal bool PreProject()
        {
            if (this.isFirstProjectCall)
            {
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("Previous and scale-munged ActualPos:");
                System.Diagnostics.Debug.WriteLine(" Ordinal   origDesired desired   unscaledPos     scaledPos");
                foreach (var qvar in vectorQpscVars)
                {
                    Debug.Assert(qvar.Variable.ActualPos == vectorCurY[qvar.Variable.Ordinal], "FirstProject: ActualPos != CurY");
                    System.Diagnostics.Debug.WriteLine("    {0} {1} {2} {3} {4}", qvar.Variable.Ordinal,
                                qvar.OrigDesiredPos, qvar.Variable.DesiredPos,
                                vectorCurY[qvar.Variable.Ordinal] / qvar.Variable.Scale,
                                vectorCurY[qvar.Variable.Ordinal]);
                }
#endif // VERBOSE
                // Due to MergeEqualityConstraints we may have moved some of the variables.  This won't
                // affect feasibility since QpscMakeFeasible would already have ensured that any unsatisfiable
                // constraints are so marked.
                foreach (var qvar in this.vectorQpscVars)
                {
                    this.vectorCurY[qvar.Variable.Ordinal] = qvar.Variable.ActualPos;
                }
            }

            //
            // Compute: g = Q'y + b' (in the Scaling paper terminology)
            //
            // g(radient) = Q'y...
            MatrixVectorMultiply(this.vectorCurY, this.gradientVector /*result*/);

            // If we've minimized the goal function (far enough), we're done.
            // This uses the Q'y value we've just put into gradientVector and tests the goal-function value
            // to see if it is sufficiently close to the previous value to be considered converged.
            if (HasConverged())
            {
                return false;
            }

            // ...g = Q'y + b'
            VectorVectorAdd(this.gradientVector, this.vectorWiDi, this.gradientVector /*result*/);

            //
            // Compute: alpha = g#g / g#Q'g  (# == transpose)
            //
            double alphaNumerator = VectorVectorMultiply(this.gradientVector, this.gradientVector);     // Compute numerator of stepsize
            double alphaDenominator = 0.0;
            if (0.0 != alphaNumerator)
            {
                MatrixVectorMultiply(this.gradientVector, this.vectorQg /*result*/);
                alphaDenominator = VectorVectorMultiply(this.vectorQg, this.gradientVector);
            }
            if (0.0 == alphaDenominator)
            {
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("Converging due to zero-gradient");
#endif // VERBOSE
                return false;
            }
            double alpha = alphaNumerator / alphaDenominator;
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("Alpha = {0} (num {1}, den {2})", alpha, alphaNumerator, alphaDenominator);
#endif // VERBOSE

            //
            // Store off the current position as the previous position (the paper's y^ (y-hat)),
            // then calculate the new current position by subtracting the (gradient * alpha)
            // from it and update the Variables' desired position.
            //
            VectorCopy(this.vectorPrevY /*dest*/, this.vectorCurY /*src*/);

#if VERIFY || VERBOSE
            normAlphaG = 0.0;
            foreach (var g in this.gradientVector)
            {
                var ag = alpha * g;
                normAlphaG += ag * ag;
            }
            normAlphaG = Math.Sqrt(normAlphaG);
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("NormAlphaG = {0}", this.normAlphaG);
#endif // VERBOSE
#endif // VERIFY || VERBOSE

            // Update d(esiredpos) = y - alpha*g
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("PreProject gradient-stepped desired positions:");
#endif // VERBOSE
            // Use vectorCurY as temp as it is not used again here and is updated at start of PostProject.
            VectorScaledVectorSubtract(this.vectorPrevY, alpha, this.gradientVector, this.vectorCurY /*d*/);
            for (var ii = 0; ii < this.vectorCurY.Length; ++ii)
            {
                this.vectorQpscVars[ii].Variable.DesiredPos = this.vectorCurY[ii];
#if VERBOSE
                var qvar = vectorQpscVars[ii];
                System.Diagnostics.Debug.WriteLine("{0} curY {1} orig desiredpos = {2}, current desiredpos = {3}, gradient = {4}",
                        ii, vectorCurY[ii], qvar.OrigDesiredPos, qvar.Variable.DesiredPos, gradientVector[ii]);
#endif // VERBOSE
            }
            return true;
        } // end PreProject()

        // Called by SolveQpsc after the split/project phase.
        internal bool PostProject()
        {
            //
            // Update our copy of current positions (y-bar from the paper) and deltaY (p in the Scaling paper; y-bar minus y-hat).
            //
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("PostProject current scaled and unscaled ActualPos:");
#endif // VERBOSE
            foreach (var qvar in this.vectorQpscVars)
            {
                this.vectorCurY[qvar.Variable.Ordinal] = qvar.Variable.ActualPos;
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("    {0} {1} {2}", qvar.Variable.Ordinal, qvar.Variable.ActualPos, qvar.Variable.ActualPos * qvar.Variable.Scale);
#endif // VERBOSE
            }

            // vectorCurY temporarily becomes the p-vector from the Scaling paper since we don't use the "current"
            // position otherwise, until we reset it at the end.
            VectorVectorSubtract(this.vectorPrevY, this.vectorCurY, this.vectorCurY /*result; p (deltaY)*/);

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("PostProject variable deltas:");
#endif // VERBOSE

#if VERIFY || VERBOSE
            double normDeltaY = 0.0;
            for (int ii = 0; ii < this.vectorCurY.Length; ++ii)
            {
                normDeltaY += this.vectorCurY[ii] * this.vectorCurY[ii];
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("  {0} {1}", ii, vectorCurY[ii]);
#endif // VERBOSE
            }
            normDeltaY = Math.Sqrt(normDeltaY);
#if VERBOSE
            System.Diagnostics.Debug.WriteLine("NormAlphaG = {0}; normDeltaY = {1}", this.normAlphaG, normDeltaY);
#endif // VERBOSE

            // dblNormSg must be >= dblNormd; account for rounding errors.
            Debug.Assert((this.normAlphaG / normDeltaY) > 0.001, "normAlphaG must be >= normDeltaY");
#endif // VERIFY || VERBOSE

            //
            // Compute: Beta = min(g#p / p#Qp, 1)
            //
            double betaNumerator = VectorVectorMultiply(this.gradientVector, this.vectorCurY /*p*/);  // Compute numerator of stepsize
            double beta = 0.0;
#if VERBOSE
            if (0.0 == betaNumerator) {
                System.Diagnostics.Debug.WriteLine("  Beta-numerator is zero");
            }
#endif // VERBOSE
            if (0.0 != betaNumerator)
            {
                // Calculate Qp first (matrix ops are associative so (AB)C == A(BC), so calculate the rhs first
                // with MatrixVectorMultiply).  Temporarily hijack vectorQg for this operation.
                MatrixVectorMultiply(this.vectorCurY /*p*/, this.vectorQg /*result*/);

                // Now p#(Qp).
                double betaDenominator = VectorVectorMultiply(this.vectorQg, this.vectorCurY /*p*/);

                // Dividing by almost-0 would yield a huge value which we'd cap at 1.0 below.
                beta = (0.0 == betaDenominator) ? 1.0 : (betaNumerator / betaDenominator);
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("Beta = {0} (num {1}, den {2})", beta, betaNumerator, betaDenominator);
#endif // VERBOSE
                if (beta > 1.0)
                {
                    // Note:  With huge ranges, beta is >>1 here - like 50 or millions.  This is expected as
                    // we're dividing by p#Qp where p is potentially quite small.
                    beta = 1.0;
                }
                else if (beta < 0.0)
                {
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("  Resetting negative Beta to zero");
#endif // VERBOSE
                    // Setting it above 0.0 can move us away from convergence, so set it to 0.0 which leaves 
                    // vectorCurY unchanged from vectorPrevY and we'll terminate if there are no splits/violations.
                    // If we were close to convergence in preProject, we could have a significantly negative
                    // beta here, which means we're basically done unless split/project still have stuff to do.
                    beta = 0.0;
                }
            } // Beta numerator is nonzero

            // Update the "Qpsc-local" copy of the current positions for use in the next loop's PreProject().
            VectorScaledVectorSubtract(this.vectorPrevY, beta, this.vectorCurY /*p*/, this.vectorCurY /*result*/);

#if VERBOSE
            System.Diagnostics.Debug.WriteLine("PostProject variable positions (scaled, unscaled) adjusted for Beta:");
            for (int ii = 0; ii < vectorCurY.Length; ++ii) {
                System.Diagnostics.Debug.WriteLine("  {0} {1} {2}", ii, vectorCurY[ii], vectorCurY[ii] * vectorQpscVars[ii].Variable.Scale);
            }
#endif // VERBOSE
            this.isFirstProjectCall = false;
            return beta > 0.0;
        } // end PostProject()

        internal double QpscComplete()
        {
            // Restore original desired position and unscale the actual position.
            foreach (QpscVar qvar in this.vectorQpscVars)
            {
                qvar.Variable.Weight = qvar.OrigWeight;
                qvar.Variable.DesiredPos = qvar.OrigDesiredPos;

                if (solverParameters.Advanced.ScaleInQpsc)
                {
                    // This multiplication essentially does what Constraint.Violation does, so the "satisfied" state
                    // of constraints won't be changed.
                    qvar.Variable.ActualPos *= qvar.Variable.Scale;
                    qvar.Variable.Scale = qvar.OrigScale;
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("var {0} block {1} blockscale {2} blockrefpos {3} ActualPos {4}", qvar.Variable.Ordinal, qvar.Variable.Block.Id,
                                        qvar.Variable.Block.Scale, qvar.Variable.Block.ReferencePos, qvar.Variable.ActualPos);
#endif // VERBOSE
                }
            }

            // This was updated to the final function value before HasConverged returned.
            return this.previousFunctionValue;
        }

        #region Internal workers

        private bool HasConverged()
        {
            //
            // Compute the function value relative to the previous iteration to test convergence:
            //     (x#Ax)/2 + bx + (w d).d       Note: final term is from Tim's Mathematica
            // where the last term (w d).d is constant and, because we only test decreasing value,
            // can therefore be omitted.
            //
            // We don't need to do the Ax operation as this is done as part of PreProject which has
            // already put this into gradientVector.
            //
            double currentFunctionValue = GetFunctionValue(this.vectorCurY);

#if VERBOSE
            // (x'Ax)/2 + bx + (w d).d, to test against the Mathematica output.
            double dblTestFuncValue = currentFunctionValue;
            for (int ii = 0; ii < vectorQpscVars.Length; ++ii) {
                QpscVar qvar = vectorQpscVars[ii];

                // Undo what we did to create the vecWiDi entry.
                double dblOrigDi = (vectorWiDi[ii] / qvar.OrigWeight) / -2.0;

                // Add the (w d).d final factor for the function value.
                dblTestFuncValue += qvar.OrigWeight * dblOrigDi * dblOrigDi;
            }
            System.Diagnostics.Debug.WriteLine("Iteration function value: {0}", dblTestFuncValue);
#endif // VERBOSE

            // If this is not our first PreProject call, test for convergence.
            bool fConverged = false;
            if (!this.isFirstProjectCall)
            {
#if VERBOSE
                // Let's see what |Xprev - Xcur| is...
                double dblDeltaNorm = 0.0;
                for (int ii = 0; ii < vectorCurY.Length; ++ii) {
                    dblDeltaNorm += (vectorPrevY[ii] - vectorCurY[ii]) * (vectorPrevY[ii] - vectorCurY[ii]);
                }
                dblDeltaNorm = Math.Sqrt(dblDeltaNorm);
                System.Diagnostics.Debug.WriteLine("|Xprev - xCur|: {0}", dblDeltaNorm);
#endif // VERBOSE

                // Check for convergence.  We are monotonically decreasing so prev should be > cur
                // with some allowance for rounding error.
                double diff = (this.previousFunctionValue - currentFunctionValue);
                double quotient = 0.0;
                if (diff != 0.0)
                {
                    double divisor = (0 != this.previousFunctionValue) ? this.previousFunctionValue : currentFunctionValue;
                    quotient = Math.Abs(diff / divisor);
#if EX_VERIFY
                    Debug.Assert((previousFunctionValue > currentFunctionValue) || (quotient < 0.001),    // account for rounding error
                                "Convergence quotient should not be significantly negative");
#endif // EX_VERIFY
                }
#if VERBOSE
                System.Diagnostics.Debug.WriteLine("  dblPrevFunctionValue {0:F5}, currentFunctionValue {1:F5}, dblDiff {2}, dblQuotient {3}",
                                previousFunctionValue, currentFunctionValue, diff, quotient);
#endif // VERBOSE

                if ((Math.Abs(diff) < solverParameters.QpscConvergenceEpsilon)
                        || (Math.Abs(quotient) < solverParameters.QpscConvergenceQuotient))
                {
#if VERBOSE
                    System.Diagnostics.Debug.WriteLine("  Terminating due to function value change within convergence epsilon");
#endif // VERBOSE
                    fConverged = true;
                }
            } // endif !isFirstProjectCall
            this.previousFunctionValue = currentFunctionValue;
            return fConverged;
        }

        private double GetFunctionValue(double[] positions)
        {
            // (x#Ax)/2...
            double value = VectorVectorMultiply(this.gradientVector, positions) / 2.0;

            // (x'Ax)/2 + bx...
            return value + VectorVectorMultiply(this.vectorWiDi, positions);
        }

        // Returns the dot product of two column vectors (with an "implicit transpose").
        private static double VectorVectorMultiply(double[] lhs, double[] rhs)
        {
            // Do not use LINQ's Sum, it slows end-to-end by over 10%.
            double sum = 0.0;
            for (int ii = 0; ii < lhs.Length; ++ii)
            {
                sum += lhs[ii] * rhs[ii];
            }
            return sum;
        }

        // Multiplies matrixQ with the column vector rhs leaving the result in column vector in result[].
        private void MatrixVectorMultiply(double[] rhs, double[] result)
        {
            // The only matrix we have here is (sparse) matrixQ so it's not a parameter.
            int rowIndex = 0;
            foreach (var row in this.matrixQ)
            {
                // Do not use LINQ's Sum, it slows end-to-end by over 10%.
                double sum = 0.0;
                foreach (var cell in row)
                {
                    sum += cell.Value * rhs[cell.Column];
                }
                result[rowIndex++] = sum;
            }
        }

#if DEAD_CODE
        // Currently not needed as matrix multiplication is associative and we only do Vector x Matrix in
        // a chain with Vector(transpose) x Matrix x Vector, so we do the Matrix x Vector operation first
        // followed by Vector x Vector.
        //
        // Multiplies the column vector lhs (with implicit transpose) with matrixQ leaving the result in column vector in result[].
        void VectorMatrixMultiply(double[] lhs, double[] result)
        {
            // The only matrix we have here is (sparse) matrixQ so it's not a parameter.
            for (var col = 0; col < result.Length; ++col)
            {
                result[col] = 0.0;
            }
            for (var row = 0; row < matrixQ.Length; ++row)
            {
                foreach (var cell in matrixQ[row]) {
                    result[cell.Column] += cell.Value * lhs[row];
                }
            }
        }
#endif // DEAD_CODE

        // Returns the addition result in result[] (which may be lhs or rhs or a different vector).
        private static void VectorVectorAdd(double[] lhs, double[] rhs, double[] result)
        {
            for (int ii = 0; ii < lhs.Length; ++ii)
            {
                result[ii] = lhs[ii] + rhs[ii];
            }
        }

        // Returns the subtraction result in result[] (which may be lhs or rhs or a different vector).
        private static void VectorVectorSubtract(double[] lhs, double[] rhs, double[] result)
        {
            for (int ii = 0; ii < lhs.Length; ++ii)
            {
                result[ii] = lhs[ii] - rhs[ii];
            }
        }

        // Same as VectorVectorSubtract except that rhs is multiplied by the scale value.
        private static void VectorScaledVectorSubtract(double[] lhs, double scale, double[] rhs, double[] result)
        {
            for (int ii = 0; ii < lhs.Length; ++ii)
            {
                result[ii] = lhs[ii] - (scale * rhs[ii]);
            }
        }

#if DEAD_CODE
        // Same as VectorScaledVectorSubtract except that we're adding and returning the result.
        double VectorScaledVectorAdd(double[] lhs, double scale, double[] rhs, double[] result) {
            double dblAbsDiff = 0.0;
            for (int ii = 0; ii < lhs.Length; ++ii) {
                result[ii] = lhs[ii] + (scale * rhs[ii]);
                dblAbsDiff += Math.Abs(result[ii] - lhs[ii]);
            }
            return dblAbsDiff;
        }
#endif // DEAD_CODE

        // Copies src to dest
        private static void VectorCopy(double[] dest, double[] src)
        {
            for (int ii = 0; ii < src.Length; ++ii)
            {
                dest[ii] = src[ii];
            }
        }

#if VERBOSE
        void DumpMatrix() {
            // We only have the one matrix to dump.
            System.Diagnostics.Debug.WriteLine("A:");
            double[] row = new double[vectorQpscVars.Length];
            uint activeCells = 0;
            for (int ii = 0; ii < row.Length; ++ii) {
                for (int jj = 0; jj < row.Length; ++jj) {
                    row[jj] = 0;      // Remove prev row values
                }
                activeCells += (uint)matrixQ[ii].Length;
                foreach (MatrixCell cell in matrixQ[ii]) {
                    row[cell.Column] = cell.Value;
                }
                DumpVector(null /*name*/, row);
            }
            System.Diagnostics.Debug.WriteLine("A matrix: {0} cells (average {1:F2} per row)", activeCells, (double)activeCells / matrixQ.Length);
        }

        void DumpVector(string name, double[] vector) {
            if (null != (object)name) {
                System.Diagnostics.Debug.WriteLine(name);
            }
            for (int jj = 0; jj < vector.Length; ++jj) {
                System.Diagnostics.Debug.Write(" {0:F5}", vector[jj]);
            }
            System.Diagnostics.Debug.WriteLine();
        }
#endif // VERBOSE
        #endregion // Internal workers
    }
}