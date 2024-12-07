//
// ObstaclePort.cs
// MSAGL class for ObstaclePorts for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
    public class ObstaclePort {
        public Port Port { get; private set; }
        public Obstacle Obstacle { get; private set; }

        public VisibilityVertex CenterVertex;

        // These are derived from PortEntry spans if present, else from Port.Location.
        public List<ObstaclePortEntrance> PortEntrances { get; private set; }

        public bool HasCollinearEntrances { get; private set; }

        // Hang onto this separately to detect port movement.
        public Point Location { get; private set; }

        public Rectangle VisibilityRectangle = Rectangle.CreateAnEmptyBox();

        public ObstaclePort(Port port, Obstacle obstacle) {
            this.Port = port;
            this.Obstacle = obstacle;
            this.PortEntrances = new List<ObstaclePortEntrance>();
            this.Location = ApproximateComparer.Round(this.Port.Location);
        }

        public void CreatePortEntrance(Point unpaddedBorderIntersect, Direction outDir, ObstacleTree obstacleTree) {
            var entrance = new ObstaclePortEntrance(this, unpaddedBorderIntersect, outDir, obstacleTree);
            PortEntrances.Add(entrance);
            this.VisibilityRectangle.Add(entrance.MaxVisibilitySegment.End);
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=370
            this.HasCollinearEntrances = this.HasCollinearEntrances | entrance.IsCollinearWithPort;
#else
            this.HasCollinearEntrances |= entrance.IsCollinearWithPort;
#endif
        }

        public void ClearVisibility() {
            // Most of the retained PortEntrance stuff is about precalculated visibility.
            this.PortEntrances.Clear();
        }

        public void AddToGraph(TransientGraphUtility transUtil, bool routeToCenter) {
            // We use only border vertices if !routeToCenter.
            if (routeToCenter) {
                CenterVertex = transUtil.FindOrAddVertex(this.Location);
            }
        }

        public void RemoveFromGraph() {
            CenterVertex = null;
        }

        // PortManager will recreate the Port if it detects this (this.Location has already been rounded).
        public bool LocationHasChanged { get { return !PointComparer.Equal(this.Location, ApproximateComparer.Round(this.Port.Location)); } }

        /// <summary>
        /// The curve associated with the port.
        /// </summary>
        public ICurve PortCurve { get { return this.Port.Curve; } }

        /// <summary>
        /// The (unrounded) location of the port.
        /// </summary>
        public Point PortLocation { get { return this.Port.Location; } }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return this.Port + this.Obstacle.ToString();
        }
    }
}