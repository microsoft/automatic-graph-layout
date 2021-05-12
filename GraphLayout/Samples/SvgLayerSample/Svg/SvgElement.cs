using Microsoft.Msagl.Drawing;
using System.Xml;

namespace SvgLayerSample.Svg {
    public abstract class SvgElement : ISvgElement {

        public virtual double X { get; set; }
        public virtual double Y { get; set; }
        public virtual double Width { get; set; }
        public virtual double Height { get; set; }
        public virtual Color BackgroundColor { get; set; } = Color.White;
        public virtual Color BorderColor { get; set; } = Color.Black;
        public virtual Color FontColor { get; set; } = Color.Black;

        public virtual double StrokeWidth { get; set; } = 1;
        public virtual bool Dashed { get; set; } = false;
        public virtual double? StrokeDashArray { get; set; } = null;

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
        
