using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace SvgLayerSample.Svg {
    public class Diagram {

        private MemoryStream ms { get; } = new MemoryStream();
        private XmlWriter xmlWriter { get; }
        private Graph drawingGraph { get;  }

        public Diagram(Graph drawingGraph) {
            // create the Xml Writer
            var streamWriter = new StreamWriter(ms);
            var xmlWriterSettings = new XmlWriterSettings { Indent = true };
            xmlWriter = XmlWriter.Create(streamWriter, xmlWriterSettings);

            // set the Graph
            this.drawingGraph = drawingGraph;
        }

        // Abstract the creation of the GeometryGraph and the node.CreateBoundary calls away in
        // a single call on the Diagram.
        public void Run() {
            drawingGraph.CreateGeometryGraph();

            foreach (var node in drawingGraph.Nodes) {
                if (node is LabeledNode ln) ln.CreateBoundary();
            }

            var routingSettings = new Microsoft.Msagl.Core.Routing.EdgeRoutingSettings {
                UseObstacleRectangles = true,
                BendPenalty = 100,
                EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.StraightLine
            };
            var settings = new SugiyamaLayoutSettings {
                ClusterMargin = 50,
                PackingAspectRatio = 3,
                PackingMethod = Microsoft.Msagl.Core.Layout.PackingMethod.Columns,
                RepetitionCoefficientForOrdering = 0,
                EdgeRoutingSettings = routingSettings,
                NodeSeparation = 50,
                LayerSeparation = 150
            };
            LayoutHelpers.CalculateLayout(drawingGraph.GeometryGraph, settings, null);

            _run();
        }

        private void _run() {
            // The culsture info is so that we "ToString" correctly, especially with
            // things like doubles:  1,2344  ->   1.2344
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try {
                
                // flip coords
                TransformGraphByFlippingY();

                Open();

                foreach (var sg in drawingGraph.RootSubgraph.AllSubgraphsDepthFirst()) {

                    if (sg.Id == "the root subgraph's boundary") continue;

                    new SvgRect {
                        X = sg.BoundingBox.Left,
                        Y = -sg.BoundingBox.Top,
                        Width = sg.BoundingBox.Width,
                        Height = sg.BoundingBox.Height,
                        StrokeDashArray = 4
                    }.WriteTo(xmlWriter);
                }

                foreach (var node in drawingGraph.Nodes) {
                    if (node is LabeledNode ln) {
                        ln.WriteTo(xmlWriter);
                    }
                }

                foreach (var edge in drawingGraph.Edges) {
                    new Connector(edge).WriteTo(xmlWriter);
                }

                Close();

                // reset coords
                TransformGraphByFlippingY();
            }
            finally {
                TransformGraphByFlippingY();

            }
        }

        void Open() {
            xmlWriter.WriteComment($"SvgWriter version: {this.GetType().Assembly.GetName().Version}");
            var box = drawingGraph.BoundingBox;
            xmlWriter.WriteStartElement("svg", "http://www.w3.org/2000/svg");
            xmlWriter.WriteAttributeString("xmlns", "xlink", null, "http://www.w3.org/1999/xlink");
            xmlWriter.WriteAttribute("width", box.Width);
            xmlWriter.WriteAttribute("height", box.Height);
            xmlWriter.WriteAttribute("id", "svg2");
            xmlWriter.WriteAttribute("version", "1.1");
            xmlWriter.WriteStartElement("g");
            xmlWriter.WriteAttribute("transform", String.Format("translate({0},{1})", -box.Left, -(box.Bottom - 12)));
        }

        /// <summary>
        /// writes the end of the file end closes the the stream
        /// </summary>
         void Close() {
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
            xmlWriter.Close();
        }

        void TransformGraphByFlippingY() {
            var matrix = new PlaneTransformation(1, 0, 0, 0, -1, 0);
            drawingGraph.GeometryGraph.Transform(matrix);
        }


        public override string ToString() {
            ms.Position = 0;
            var sr = new StreamReader(ms);
            var myStr = sr.ReadToEnd();
            var doc = XDocument.Parse(myStr);
            return doc.ToString();
        }


    }
}
