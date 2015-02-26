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
using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Incremental {
    /// <summary>
    /// A constraint must provide a method to find a feasible starting configuration, 
    /// and a method to satisfy the constraint by moving the affected nodes as little as possible
    /// </summary>
    public interface IConstraint {
        /// <summary>
        /// Satisfy the constraint by moving as little as possible.
        /// <returns>Amount of displacement</returns>
        /// </summary>
        double Project();
        /// <summary>
        /// Get the list of nodes involved in the constraint
        /// </summary>
        IEnumerable<Node> Nodes { get; }
        /// <summary>
        /// Constraints are applied according to a schedule.
        /// Level 0 constraints will be applied at all stages,
        /// Level 1 after a certain number of Level 0 has completed
        /// Level 2 after level 1 and so on.
        /// </summary>
        /// <returns></returns>
        int Level { get; }
    }
}