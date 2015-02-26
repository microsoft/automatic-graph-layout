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
﻿// --------------------------------------------------------------------------------------------------------------------
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