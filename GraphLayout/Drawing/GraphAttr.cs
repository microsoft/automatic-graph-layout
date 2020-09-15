using System;
using System.ComponentModel;
using System.Collections;
using P2=Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace Microsoft.Msagl.Drawing
{
    /// <summary>
    /// Microsoft.Msagl.Drawing attribute.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Attr"), Description("Graph or cluster layout attributes."),
    TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class GraphAttr : AttributeBase
    {
        double minimalWidth;

        /// <summary>
        /// The resulting layout should be not more narrow than this value. 
        /// </summary>
        public double MinimalWidth {
            get { return minimalWidth; }
            set { minimalWidth = Math.Max(value,0); }
        }

        double minimalHeight;

        /// <summary>
        /// The resulting layout should at least as high as this this value
        /// </summary>
        public double MinimalHeight {
            get { return minimalHeight; }
            set { minimalHeight = Math.Max(value, 0); }
        }

        double minNodeHeight = 9;
        double minNodeWidth = 13.5;
        /// <summary>
        /// the lower bound for the node height
        /// </summary>
        public double MinNodeHeight {
            get { return minNodeHeight; }
            set { minNodeHeight = Math.Max(9.0 / 10, value); }
        }

        /// <summary>
        /// the lower bound for the node width
        /// </summary>
        public double MinNodeWidth {
            get { return minNodeWidth; }
            set { minNodeWidth = Math.Max(13.5 / 10, value); }
        }

        bool simpleStretch = true;
        /// <summary>
        /// Works together with AspectRatio. If is set to false then the apsect ratio algtorithm kicks in.
        /// </summary>
        public bool SimpleStretch {
            get { return simpleStretch; }
            set { simpleStretch = value; }
        }


        /// <summary>
        /// the required aspect ratio of the graph bounding box
        /// </summary>
        public double AspectRatio { get; set; }


        int border;
        /// <summary>
        /// thickness of the graph border line
        /// </summary>
        public int Border
        {
            get { return border; }

            set { border = value; }
        }
     

        /// <summary>
        /// dumps the attribute into a string
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public string ToString(string label)
        {
            string ret = "graph [";
            if (!String.IsNullOrEmpty(label))
            {
                label = label.Replace("\r\n", "\\n");
                ret += "label=" + Utils.Quote(label);
            }

            if (this.LayerDirection != LayerDirection.None)
            {
                ret = Utils.ConcatWithLineEnd(ret, "layerdir=" + this.LayerDirection.ToString());
            }


            ret = Utils.ConcatWithLineEnd(ret, "layersep=" + this.LayerSeparation);


            ret = Utils.ConcatWithLineEnd(ret, "nodesep=" + this.NodeSeparation);

            ret = Utils.ConcatWithLineEnd(
                                               ret,
                                               Utils.ColorToString("color=", Color.ToString()),

                                               Utils.ColorToString("bgcolor=", this.bgcolor.ToString()),

                                               StylesToString("\r\n"), "]");


            return ret;

        }

        ///<summary>
        ///Background color for drawing ,plus initial fill color - white by default.
        ///</summary>
        internal Color bgcolor = new Color(255, 255, 255);//white

        /// <summary>
        /// Background color for drawing and initial fill color.
        /// </summary>
        [Description("Background color for drawing ,plus initial fill color.")]
        public Color BackgroundColor
        {
            get
            {
                return bgcolor;
            }
            set
            {
                bgcolor = value;
            }

        }


        private double margin = 10;
/// <summary>
/// margins width
/// </summary>
        public double Margin
        {
            get { return margin; }
            set { margin = value; }
        }

        bool optimizeLabelPositions=true;
        /// <summary>
        /// if set to true then the label positions are optimized
        /// </summary>
        public bool OptimizeLabelPositions {
            get { return optimizeLabelPositions; }
            set { optimizeLabelPositions = value; }
        }

        double minNodeSeparation = 72 * 0.50 / 8;
        /// <summary>
        /// the minimal node separation
        /// </summary>
        public double MinNodeSeparation { get { return minNodeSeparation; } }
        
        ///<summary>
        ///Separation between nodes
        ///</summary>
        private double nodesep = 72 * 0.50 / 4;
/// <summary>
/// the node separation
/// </summary>
        public double NodeSeparation
        {
            get { return nodesep; }
            set { nodesep = Math.Max(value,MinNodeSeparation); }
        }


        private LayerDirection layerdir = LayerDirection.TB;
        /// <summary>
        /// Directs node layers
        /// </summary>
        public LayerDirection LayerDirection
        {
            get { return layerdir; }
            set { layerdir = value; }
        }
        ///<summary>
        ///Separation between layers in
        ///</summary>
        private double layersep = 72 * 0.5; // is equal to minLayerSep
 /// <summary>
 /// the distance between two neigbor layers
 /// </summary>
        public double LayerSeparation
        {
            get { return layersep; }
            set { layersep = Math.Max(value,minLayerSep); }
        }

        double minLayerSep = 72 * 0.5 * 0.01;
        /// <summary>
        /// the minimal layer separation
        /// </summary>
        public double MinLayerSeparation
        {
            get { return minLayerSep; }
        }
        /// <summary>
        /// constructor
        /// </summary>
        public GraphAttr()
        {
            
        }

    }
}
