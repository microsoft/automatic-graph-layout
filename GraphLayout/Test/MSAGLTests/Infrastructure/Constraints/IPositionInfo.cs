// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPositionInfo.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Msagl.UnitTests.Constraints
{
    // ClusterDef and VariableDef both contain position info and we do similar calculations on it so
    // use a common interface.
    internal interface IPositionInfo
    {
        // May be available only after Solve().
        double PositionX { get; }
        double PositionY { get; }
        double Left { get; }
        double Right { get; }
        double Top { get; }
        double Bottom { get; }

        // May be available after initialization.
        double DesiredPosX { get; }
        double DesiredPosY { get; }

        double SizeX { get; }
        double SizeY { get; }

        double InitialLeft { get; }
        double InitialRight { get; }
        double InitialTop { get; }
        double InitialBottom { get; }

        string ClassName { get; }
        string InstanceId { get; }
    }
}