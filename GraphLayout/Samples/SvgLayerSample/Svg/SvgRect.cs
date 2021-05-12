using Microsoft.Msagl.Drawing;
using System.Xml;

namespace SvgLayerSample.Svg {
    public class SvgRect : SvgElement {
        
        public override void WriteTo(XmlWriter writer) {
            writer.WriteStartElement("rect");
            writer.WriteAttribute("x", X);
            writer.WriteAttribute("y", Y);
            writer.WriteAttribute("width", Width);
            writer.WriteAttribute("height", Height);
            writer.WriteAttribute("stroke", BorderColor);
            writer.WriteAttribute("fill", BackgroundColor);

            if (this.StrokeDashArray != null) {
                writer.WriteAttribute("stroke-dasharray", StrokeDashArray);
            }

            writer.WriteEndElement();
        }

    }
}
