using Microsoft.Msagl.Drawing;
using System.Xml;

namespace SvgLayerSample.Svg {
    public abstract class SvgElement : ISvgElement {

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Color BackgroundColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Black;
        public Color FontColor { get; set; } = Color.Black;

        public double StrokeWidth { get; set; } = 1;
        public bool Dashed { get; set; } = false;
        public double? StrokeDashArray { get; set; } = null;



        public virtual void WriteTo(XmlWriter writer) {
            //writer.WriteRaw(this.ToString());
        }

        public static SvgElement DefaultElement() {
            return new DefaultSvgElement();
        }
    }

    public class DefaultSvgElement : SvgElement {

    }

    
}
        
