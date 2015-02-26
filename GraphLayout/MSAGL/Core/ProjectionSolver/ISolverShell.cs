/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace Microsoft.Msagl.Core.ProjectionSolver {
    /// <summary>
    /// An interface that abstracts the underlying solver implementation.
    /// </summary>
    public interface ISolverShell {
        /// <summary>
        /// Add a goal that the distance between two variables is minimized.
        /// </summary>
        /// <param name="id1">app id for the first variable</param>
        /// <param name="id2">app id for the second variable</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        void AddGoalTwoVariablesAreClose(int id1, int id2);

        /// <summary>
        /// Add a goal that the distance between two variables is minimized, with a weight for the relationship.
        /// </summary>
        /// <param name="id1">app id for the first variable</param>
        /// <param name="id2">app id for the second variable</param>
        /// <param name="weight">the weight of the corresponding term in the goal function</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        void AddGoalTwoVariablesAreClose(int id1, int id2, double weight);

        /// <summary>
        /// Add a constraint that a+gap is less than or equal to b.
        /// </summary>
        /// <param name="idLeft">app id for left variable</param>
        /// <param name="idRight">app id for right variable</param>
        /// <param name="gap">required separation</param>
        void AddLeftRightSeparationConstraint(int idLeft, int idRight, double gap);

        /// <summary>
        /// Add a constraint that a+gap is equal, or less than or equal, to b.
        /// </summary>
        /// <param name="idLeft">app id for left variable</param>
        /// <param name="idRight">app id for right variable</param>
        /// <param name="gap">Required separation</param>
        /// <param name="isEquality">Whether gap is exact rather than minimum</param>
        void AddLeftRightSeparationConstraint(int idLeft, int idRight, double gap, bool isEquality);

        /// <summary>
        /// Add a variable with a desired position and weight.
        /// </summary>
        /// <param name="id">app id for the variable</param>
        /// <param name="position">desired position</param> 
        /// <param name="weight">the weight of the corresponding term in the goal function</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        void AddVariableWithIdealPosition(int id, double position, double weight);

        /// <summary>
        /// Add a variable with a desired position.
        /// </summary>
        /// <param name="id">app id for the variable</param>
        /// <param name="position">desired position</param> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        void AddVariableWithIdealPosition(int id, double position);

        /// <summary>
        /// Get the solved position of a variable.
        /// </summary>
        /// <param name="id">app id of the variable</param>
        /// <returns>solved position</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        double GetVariableResolvedPosition(int id);

        /// <summary>
        /// Initialize (or reinitialize) the solver.
        /// </summary>
        void InitSolver();

        /// <summary>
        /// Execute the solver, filling in the Solution object and the values to be returned by GetvariableResolvedPosition.
        /// </summary>
        /// <returns>Pass or fail</returns>
        void Solve();

        /// <summary>
        /// Execute the solver, filling in the Solution object and the values to be returned by GetvariableResolvedPosition.
        /// </summary>
        /// <param name="parameters">Parameter object class specific to the underlying solver</param>
        /// <returns>Pass or fail</returns>
        void Solve(object parameters);

        /// <summary>
        /// Execute the solver, filling in the Solution object and the values to be returned by GetvariableResolvedPosition.
        /// </summary>
        /// <param name="parameters">Parameter object class specific to the underlying solver</param>
        /// <param name="executionLimitExceeded">if true, one or more limits such as iteration count 
        ///         or timeout were exceeded</param>
        /// <returns>Pass or fail</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        bool Solve(object parameters, out bool executionLimitExceeded);

        /// <summary>
        /// Returns the solution object class specific to the underlying solver, or null if there has
        /// been no call to Solve() or it threw an exception.
        /// </summary>
        object Solution { get; }

        /// <summary>
        /// Add a variable at a fixed position.
        /// </summary>
        /// <param name="id">app id for variable</param>
        /// <param name="position">desired position</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        void AddFixedVariable(int id, double position);

        /// <summary>
        /// Returns whether the given app id was added via AddVariableWithIdealPosition or AddFixedVariable.
        /// </summary>
        /// <param name="v"></param>
        /// <returns>Whether or not the app id was found</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        bool ContainsVariable(int v);
        /// <summary>
        /// gets the variable ideal position that has been set at the beginning
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        double GetVariableIdealPosition(int v);
    }
}
