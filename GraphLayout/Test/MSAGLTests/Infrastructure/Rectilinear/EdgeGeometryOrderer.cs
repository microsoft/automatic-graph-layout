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
//-----------------------------------------------------------------------
// <copyright file="EdgeGeometryOrderer.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests.Rectilinear 
{
    using System;

    internal class EdgeGeometryOrderer : IComparer<EdgeGeometry> 
    {
        private readonly Dictionary<Port, int> portToIdMap;

        public EdgeGeometryOrderer(Dictionary<Port, int> portToIdMap)
        {
            this.portToIdMap = portToIdMap;
        }

        /// <summary>
        /// The requisite comparison method, ordering first by shape Id then by location.
        /// </summary>
        /// <param name="lhs">The left-hand side.</param>
        /// <param name="rhs">The right-hand side.</param>
        /// <returns>-1 if lhs is less than rhs, 1 if lhs is greater than rhs, else 0</returns>
        public int Compare(EdgeGeometry lhs, EdgeGeometry rhs)
        {
            Validate.IsNotNull(lhs, "Lhs must not be null");
            Validate.IsNotNull(rhs, "Rhs must not be null");
            var cmp = ComparePorts(lhs.SourcePort, rhs.SourcePort);
            if (0 != cmp)
            {
                return cmp;
            }
            cmp = ComparePorts(lhs.TargetPort, rhs.TargetPort);
            if (0 != cmp)
            {
                return cmp;
            }
            cmp = lhs.SourcePort.Location.X.CompareTo(rhs.SourcePort.Location.X);
            if (0 != cmp)
            {
                return cmp;
            }
            cmp = lhs.SourcePort.Location.Y.CompareTo(rhs.SourcePort.Location.Y);
            if (0 != cmp)
            {
                return cmp;
            }
            cmp = lhs.TargetPort.Location.X.CompareTo(rhs.TargetPort.Location.X);
            if (0 != cmp)
            {
                return cmp;
            }
            cmp = lhs.TargetPort.Location.Y.CompareTo(rhs.TargetPort.Location.Y);
            return cmp;
        }

        private int ComparePorts(Port lhsPort, Port rhsPort)
        {
            var lhsPortId = this.portToIdMap[lhsPort];
            var rhsPortId = this.portToIdMap[rhsPort];
            return lhsPortId.CompareTo(rhsPortId);
        }
    }
}
