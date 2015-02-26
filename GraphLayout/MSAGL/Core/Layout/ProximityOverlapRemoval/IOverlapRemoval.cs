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
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval {
    /// <summary>
    /// Overlap Removal Interface. All Overlap Removal classes should implement this to unify usage of different methods.
    /// </summary>
    public interface IOverlapRemoval {

        /// <summary>
        /// Settings to be used for the overlap removal. Not all settings have to be used.
        /// </summary>
        /// <param name="settings"></param>
        void Settings(OverlapRemovalSettings settings);
        /// <summary>
        /// Main function which removes the overlap for a given graph and finally sets the new node positions.
        /// </summary>
        /// <param name="graph"></param>
        void RemoveOverlap(GeometryGraph graph);
        /// <summary>
        /// Method giving the number of needed iterations for the last run. (Runtime statistic)
        /// </summary>
        /// <returns></returns>
        int GetLastRunIterations();
        
    }
}
