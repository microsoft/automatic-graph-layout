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
//
// BasicReflectionEvent.cs
// MSAGL Base class for reflection events for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;

namespace Microsoft.Msagl.Routing.Rectilinear {
    internal class BasicReflectionEvent : SweepEvent {
        internal Obstacle ReflectingObstacle { get; private set; }
        internal Obstacle InitialObstacle { get; private set; }
        internal BasicReflectionEvent PreviousSite { get; private set; }

        // Called by StoreLookaheadSite only.
        internal BasicReflectionEvent(Obstacle initialObstacle, Obstacle reflectingObstacle, Point site) {
            this.InitialObstacle = initialObstacle;
            this.ReflectingObstacle = reflectingObstacle;
            this.site = site;
        }

        // Called by LowReflectionEvent or HighReflectionEvent ctors, which are called out of 
        // AddReflectionEvent, which in turn is called by LoadLookaheadIntersections.
        // In this case we know the eventObstacle and initialObstacle are the same obstacle (the
        // one that the reflected ray bounced off of, to generate the Left/HighReflectionEvent).
        internal BasicReflectionEvent(BasicReflectionEvent previousSite, Obstacle reflectingObstacle, Point site) {
            this.InitialObstacle = previousSite.ReflectingObstacle;
            this.ReflectingObstacle = reflectingObstacle;
            this.site = site;
            this.PreviousSite = previousSite;
        }

        // If true, we have a staircase situation.
        internal bool IsStaircaseStep(Obstacle reflectionTarget) {
            return (this.InitialObstacle == reflectionTarget);
        }

        private readonly Point site;
        internal override Point Site {
            get { return site; }
        }
    }
}
