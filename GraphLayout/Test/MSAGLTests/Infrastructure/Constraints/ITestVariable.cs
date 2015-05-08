// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITestVariable.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.UnitTests.Constraints
{
    // VariableDef carries an instantiation of this class, because OverlapRemoval and ProjectionSolver
    // use different classes (Node and Variable, respectively).
    internal interface ITestVariable
    {
        double ActualPos { get; }
    }
}
