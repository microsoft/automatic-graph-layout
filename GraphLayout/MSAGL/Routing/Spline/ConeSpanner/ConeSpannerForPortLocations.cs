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
using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Visibility;
using System.Linq;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner
{
    internal class ConeSpannerForPortLocations : AlgorithmBase {

        readonly IEnumerable<Polyline> obstacles;
        // double coneAngle = Math.PI / 18;// ten degrees
        //double coneAngle = Math.PI / 9;// twenty degrees
        double coneAngle = 
            Math.PI / 6;// thirty degrees

        //double coneAngle = Math.PI / 4;// 45 degrees
        //double coneAngle = Math.PI / 2;// 90 degrees!

        internal double ConeAngle {
            get { return coneAngle; }
            set { coneAngle = value; }
        }


        internal ConeSpannerForPortLocations(IEnumerable<Polyline> obstacles, VisibilityGraph visibilityGraph,
            IEnumerable<Point> portLocationsPointSet) {
            PortLocations = portLocationsPointSet;
            this.obstacles = VisibilityGraph.OrientHolesClockwise(obstacles);
            VisibilityGraph = visibilityGraph;
        }

        protected IEnumerable<Point> PortLocations { get; set; }

        public VisibilityGraph VisibilityGraph { get; private set; }

        internal static int GetTotalSteps(double coneAngle)
        {
            return (int)(2 * Math.PI / coneAngle);
        }

        protected override void RunInternal() {
            //we need to run the full circle of directions since we do not eminate cones from
            //obstacle vertices here
            for (int i = 0; i<GetTotalSteps(coneAngle); i++) {
                var angle = coneAngle*i;
                AddDirection(new Point(Math.Cos(angle), Math.Sin(angle)));
                ProgressStep();
            }
        }

       

        void AddDirection(Point direction) {
            var visibilityGraph = new VisibilityGraph();
            LineSweeperForPortLocations.Sweep(obstacles, direction, coneAngle, visibilityGraph,
                                              PortLocations);
            foreach (var edge in visibilityGraph.Edges)
                VisibilityGraph.AddEdge(edge.SourcePoint, edge.TargetPoint,
                                        ((a, b) => new TollFreeVisibilityEdge(a, b)));
        }

    }
}
