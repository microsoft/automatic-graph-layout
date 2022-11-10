using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Spline.ConeSpanner;
using Microsoft.Msagl.Routing.Visibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests.Visibility {
    [TestClass]
    public class TestLineSweeper {
        [TestMethod]
        public void TwoRectangles() {
            VisibilityGraph visibilityGraph = new VisibilityGraph();

            var addedPolygons = new List<Polygon>();
            addedPolygons.Add(new Polygon(getPoly(new Point(), 20)));
            addedPolygons.Add(new Polygon(getPoly(new Point(200, 0), 20)));
            foreach(var p in addedPolygons) {
                visibilityGraph.AddHole(p.Polyline);
            }
            var ir = new InteractiveTangentVisibilityGraphCalculator(new List<Polygon>(), addedPolygons, visibilityGraph);
            ir.Run();
            
        }

        private Polyline getPoly(Point point, double size) {
            var rect = new Rectangle(new Size(size, size), point);
            return rect.Perimeter();
        }

    }
}
