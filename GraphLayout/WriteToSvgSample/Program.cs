
using System.IO;
using System.Text;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;

namespace WriteToSvgSample {
    class Program {
        static void Main(string[] args) {
            var drawingGraph = new Graph();
            drawingGraph.AddEdge("A", "B"); // now the drawing graph has nodes A,B and and an edge A -> B\
                                            // the geometry graph is still null, so we are going to create it

            drawingGraph.CreateGeometryGraph();
            // Now the drawing graph elements point to the corresponding geometry elements, 
            // however the node boundary curves are not set.
            // Setting the node boundaries
            foreach (var n in drawingGraph.Nodes) {
                // Ideally we should look at the drawing node attributes, and figure out, the required node size
                // I am not sure how to find out the size of a string rendered in SVG. Here, we just blindly assign to each node a rectangle with width 60 and height 40, and round its corners.
                n.GeometryNode.BoundaryCurve = CurveFactory.CreateRectangleWithRoundedCorners(60, 40, 3, 2, new Point(0, 0));
            }

            SetUpLabels(drawingGraph);
            LayoutHelpers.CalculateLayout(drawingGraph.GeometryGraph, new SugiyamaLayoutSettings(), null);
            PrintSvgAsString(drawingGraph);
        }

        static void SetUpLabels(Graph drawingGraph) {
            var na = drawingGraph.FindNode("A");
            na.LabelText = "AText";
            na.Label.Width = na.Width * 0.6;
            var nb = drawingGraph.FindNode("B");
            nb.LabelText = "BText";
            nb.Label.Width = nb.Width * 0.6;
        }

        static void PrintSvgAsString(Graph drawingGraph) {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            var svgWriter = new SvgGraphWriter(writer.BaseStream, drawingGraph);
            svgWriter.Write();
            // get the string from MemoryStream
            ms.Position = 0;
            var sr = new StreamReader(ms);
            var myStr = sr.ReadToEnd();
            System.Console.WriteLine(myStr);
        }
    }
}
