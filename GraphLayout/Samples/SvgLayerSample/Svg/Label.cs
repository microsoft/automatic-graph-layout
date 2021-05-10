using Microsoft.Msagl.Drawing;
using System;
using dwg = System.Drawing;
using System.Xml;

namespace SvgLayerSample.Svg {
    public class Label {

        public string LabelText { get; }
        public double Height => TextElement.TextHeight + PaddingTop;
        public double Width => TextElement.TextWidth;
        public double MarginToPreviousLabel { get; set; } = 12d;

        public Color? Color { get; set; } = null;

        private readonly Text TextElement;
        public double PaddingTop { get; set; } = 0d;

        public dwg.Font Font  {
            get {
                return TextElement.Font;
            }
            set {
                TextElement.Font = value;
            }
        }


        public Label(string labelText) {
            this.LabelText = Utils.WordWrap(labelText, 25);
            this.TextElement = new Text(this.LabelText);
        }
        public Label(string labelText, dwg.Font font) {
            this.LabelText = Utils.WordWrap(labelText, 25);
            this.TextElement = new Text(this.LabelText);
            this.Font = font;
        }

        public void WriteTo(XmlWriter writer) {
            this.WriteTo(writer, this.Width, 0);
        }

        public void WriteTo(XmlWriter writer, double containerWidth, double y) {

            // y is the starting y for this text element
            writer.WriteStartElement("text");
            writer.WriteAttribute("x", "50%");
            writer.WriteAttribute("y", y + this.PaddingTop);
            writer.WriteAttribute("font-familly", this.Font.FontFamily.Name);
            writer.WriteAttribute("font-weight", this.Font.Bold ? "bold": "normal");
            writer.WriteAttribute("font-size", this.Font.Size);
            writer.WriteAttribute("text-anchor", "middle");
            writer.WriteAttribute("fill", Color);


            var index = 0;
            foreach (var part in this.LabelText.Split(Environment.NewLine)) {
                writer.WriteStartElement("tspan");
                if (index > 0) writer.WriteAttribute("x", "50%");
                if (index > 0) writer.WriteAttribute("dy", "20");
                writer.WriteAttribute("fill", Color);
                writer.WriteString(part);
                writer.WriteEndElement();
                index++;
            }

            writer.WriteEndElement();
        }
    }
}
