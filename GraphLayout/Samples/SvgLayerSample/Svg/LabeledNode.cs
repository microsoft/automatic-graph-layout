using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SvgLayerSample.Svg {
    /// <summary>
    /// The Labeled Node is a Node which can contain labels. Each label will
    /// be printed in a vertical column.
    /// </summary>
    public class LabeledNode : Node {
        public List<Label> Labels { get; }
        public SvgElement SvgElement { get; } = SvgElement.DefaultElement();

        public LabeledNode(string id) : base(id) {
            this.Labels = new List<Label> {
                new Label(id)
            };
        }
        public LabeledNode(string id, List<Label> labels) : base(id) {
            this.Labels = labels;
        }
        public LabeledNode(string id, List<string> labels) : base(id) {
            this.Labels = labels.Where(s => s != null).Select(s => new Svg.Label(s)).ToList();
        }

        public void CreateBoundary() {

            var height = (this.Labels.FirstOrDefault()?.Height / 1.45) ?? 0d; // minimal height, the 1.45 is a magical number, found through experimentation
            var width = 100d;
            foreach (var label in this.Labels) {
                // the padding is 12 on each side, so we'll pad the label width with 24
                if (label.Width + 24 > width) width = label.Width + 24;
                height += label.Height;
            }

            this.GeometryNode.BoundaryCurve = CurveFactory.CreateRectangle(width, height > 0 ? height : 50, new Point(0, 0));
        }

        public void WriteTo(XmlWriter writer) {
            writer.WriteStartElement("svg");
            writer.WriteAttribute("x", this.BoundingBox.Left);
            writer.WriteAttribute("y", this.BoundingBox.Bottom);
            writer.WriteAttribute("width", this.BoundingBox.Width);
            writer.WriteAttribute("height", this.BoundingBox.Height);

            new SvgRect {
                X = 0,
                Y = 0,
                Width = this.BoundingBox.Width,
                Height = this.BoundingBox.Height,
                BackgroundColor = this.SvgElement.BackgroundColor,
                BorderColor = this.SvgElement.BorderColor,
                FontColor = this.SvgElement.FontColor
            }.WriteTo(writer);


            var y = this.Labels.FirstOrDefault()?.Height ?? 20d;
            foreach (var label in this.Labels) {
                label.Color = label.Color ?? this.SvgElement.FontColor;
                label.WriteTo(writer, this.BoundingBox.Width, y);
                y += 15d;
            }

            writer.WriteEndElement();
        }
    }
}
