using System;
using System.Collections;
using System.ComponentModel;
using Microsoft.Msagl.Core.Layout;
using P2=Microsoft.Msagl.Core.Geometry.Point;
using Microsoft.Msagl;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Attribute of a Node.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Attr"), Description("Node layout attributes."),
    TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class NodeAttr : AttributeBase {

        double padding = 2;  

        /// <summary>
        /// Splines should avoid being closer to the node than Padding
        /// </summary>
        public double Padding {
            get { return padding; }
            set { padding = Math.Max(0,value); 
                RaiseVisualsChangedEvent(this,null);
            }
        }

        double xRad = 3;

        /// <summary>
        ///x radius of the rectangle box 
        /// </summary>
        public double XRadius {
            get { return xRad; }
            set { xRad = value; 
                RaiseVisualsChangedEvent(this,null);
            }
        }

        double yRad = 3;
        /// <summary>
        /// y radius of the rectangle box 
        /// </summary>
        public double YRadius {
            get { return yRad; }
            set { yRad = value; }
        }

        private LgNodeInfo.LabelPlacement clusterLabelPlacement = LgNodeInfo.LabelPlacement.Top;
        /// <summary>Informs the rendering layer on where the margin for a cluster label is supposed to appear.</summary>
        public LgNodeInfo.LabelPlacement ClusterLabelMargin {
            get { return clusterLabelPlacement; }
            set { clusterLabelPlacement = value; }
        }
        
        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString() {

            return Utils.ConcatWithComma(StylesToString(","), 
                                   Utils.ColorToString("color=", base.Color.ToString()),
                                   Utils.ShapeToString("shape=", this.shape),
                                   Utils.ColorToString("fillcolor=", fillcolor.ToString()),
                                   IdToString()
                                   );

        }

        /// <summary>
        /// Clones the node attribute
        /// </summary>
        /// <returns></returns>
        public NodeAttr Clone() {
            NodeAttr r = this.MemberwiseClone() as NodeAttr;
            return r;
        }

        static Color defaultFillColor = Color.LightGray;
        /// <summary>
        /// the default fill color
        /// </summary>
        static public Color DefaultFillColor {
            get { return defaultFillColor; }
            set { defaultFillColor = value; }
        }

     
        internal Color fillcolor = Color.Transparent;
        ///<summary>
        ///Node fill color.
        ///</summary>
        public Color FillColor {

            get {
                return fillcolor;
            }
            set {
                fillcolor = value;
                RaiseVisualsChangedEvent(this, null);
            }
        }

        
        internal Shape shape = Shape.Box;


        /// <summary>
        /// Node shape.
        /// </summary>
        public Shape Shape {
            get { return shape; }
            set {
                shape = value;
                RaiseVisualsChangedEvent(this, null);
            }
        }        

        int labelMargin = 1;

        /// <summary>
        /// the node label margin
        /// </summary>
        public int LabelMargin {
            get { return labelMargin; }
            set {
                labelMargin = value;
                RaiseVisualsChangedEvent(this, null);
            }
        }

        private double labelWidthToHeightRatio = 1.0;

        /// <summary>
        /// the label width to height ratio.
        /// </summary>
        public double LabelWidthToHeightRatio {
            get { return labelWidthToHeightRatio; }
            set { labelWidthToHeightRatio = value; }
        }
    }
}
