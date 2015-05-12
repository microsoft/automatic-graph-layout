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
