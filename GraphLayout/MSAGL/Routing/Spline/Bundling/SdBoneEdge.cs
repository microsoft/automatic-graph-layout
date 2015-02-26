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
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;
using System.Diagnostics;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    [DebuggerDisplay("({SourcePoint.X},{SourcePoint.Y})->({TargetPoint.X},{TargetPoint.Y})")]
    internal class SdBoneEdge {
        internal readonly VisibilityEdge VisibilityEdge;
        internal readonly SdVertex Source;
        internal readonly SdVertex Target;
        int numberOfPassedPaths;

        internal SdBoneEdge(VisibilityEdge visibilityEdge, SdVertex source, SdVertex target) {
            VisibilityEdge = visibilityEdge;
            Source = source;
            Target = target;
        }

        internal Point TargetPoint {
            get { return Target.Point; }
        }

        internal Point SourcePoint {
            get { return Source.Point; }
        }

        internal bool IsOccupied {
            get { return numberOfPassedPaths > 0; }
        }

        internal Set<CdtEdge> CrossedCdtEdges { get; set; }

        internal bool IsPassable {
            get {
                return Target.IsTargetOfRouting || Source.IsSourceOfRouting ||
                       VisibilityEdge.IsPassable == null ||
                       VisibilityEdge.IsPassable();
            }
        }

        internal void AddOccupiedEdge() {
            numberOfPassedPaths++;
        }

        internal void RemoveOccupiedEdge() {
            numberOfPassedPaths--;
            Debug.Assert(numberOfPassedPaths >= 0);
        }
    }
}