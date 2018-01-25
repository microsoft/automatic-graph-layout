using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Globalization;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;
using P2=Microsoft.Msagl.Core.Geometry.Point;
using Microsoft.Msagl.Drawing;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Keep the information related to an object label
    /// </summary>
    [Serializable]
    public class Label: DrawingObject {

        ///<summary>
        ///</summary>
        public DrawingObject Owner { get; set; }
        /// <summary>
        /// an empty constructor
        /// </summary>
        public Label(){}

        /// <summary>
        /// a constructor with text
        /// </summary>
        /// <param name="textPar"></param>
        public Label(string textPar) { this.text = textPar; }


        ///<summary>
        ///</summary>
        public Point Center
        {
            get
            {
                if (Owner == null)
                    return new Point();
                var edge = Owner as Edge;
                if (edge != null)
                    return edge.GeometryEdge.Label.Center;
                return ((Node)Owner).GeometryNode.Center;
            }
        }

        double width;
        double height;
            /// <summary>
        /// the width of the label
        /// </summary>
        public double Width
        {
            get { return GeometryLabel == null ? width : GeometryLabel.Width; }
            set
            {
                if (GeometryLabel == null)
                    width = value;
                else GeometryLabel.Width = value;
            }
        }

        /// <summary>
        /// the height of the label
        /// </summary>
        public double Height
        {
            get { return GeometryLabel == null ? height : GeometryLabel.Height; }
            set
            {
                if (GeometryLabel == null)
                    height = value;
                else GeometryLabel.Height = value;
            }
        }

        /// <summary>
        /// left coordinate 
        /// </summary>
        public double Left { get { return Center.X - Width / 2; } }
        /// <summary>
        /// top coordinate
        /// </summary>
        public double Top { get { return Center.Y + Height / 2; } }

        /// <summary>
        /// left coordinate 
        /// </summary>
        public double Right { get { return Center.X + Width / 2; } }
        /// <summary>
        /// top coordinate
        /// </summary>
        public double Bottom { get { return Center.Y - Height / 2; } }

        /// <summary>
        /// gets the left top corner
        /// </summary>
        public P2 LeftTop { get{ return new P2(Left,Top);}}


        /// <summary>
        /// gets the right bottom corner
        /// </summary>
        public P2 RightBottom { get { return new P2(Right, Bottom); } }

        /// <summary>
        /// returns the bounding box of the label
        /// </summary>
        override public Rectangle BoundingBox { 
            get { return new Rectangle(LeftTop, RightBottom); }
        }

        /// <summary>
        /// gets or sets the label size
        /// </summary>
        virtual public Size Size {
            get { return new Size(Width, Height); }
            set {
                Width = value.Width;
                Height = value.Height;
            }
        }

        
        internal Color fontcolor = Color.Black;

        ///<summary>
        ///Label font color.
        ///</summary>
        [Description("type face color")]
        public Color FontColor {
            get { return fontcolor; }
            set {
                fontcolor = value;
            }
        }

        FontStyle fontStyle = FontStyle.Regular;

        ///<summary>
        ///Label font style.
        ///</summary>
        [Description("type face style")]
        public FontStyle FontStyle
        {
            get { return fontStyle; }
            set
            {
                fontStyle = value;
            }
        }


        ///<summary>
        ///Type face font.
        ///</summary>
        string fontName = "";

        ///<summary>
        ///Type face font
        ///</summary>
        [Description("type face font"),
        DefaultValue("")]
        public string FontName {
            get {
                if (String.IsNullOrEmpty(fontName))
                    return DefaultFontName;
                else
                    return fontName;
            }
            set {
                fontName = value;
            }

        }

        


        string text;
        /// <summary>
        /// A label of the entity. The label is rendered opposite to the ID. 
        /// </summary>
        public string Text {
            get { return text; }
            set {
                if (value != null)
                    text = value.Replace("\\n", "\n");
                else
                    text = "";
            }
        }

       


        internal double fontsize = DefaultFontSize;

        ///<summary>
        ///The point size of the id.
        ///</summary>
        public double FontSize {
            get { return fontsize; }
            set { fontsize = value; }
        }


        internal static string defaultFontName = "Times-Roman";
        /// <summary>
        /// the name of the defaul font
        /// </summary>
        public static string DefaultFontName {
            get { return defaultFontName; }
            set { defaultFontName = value; }
        }

        static int defaultFontSize = 12;
        /// <summary>
        /// the default font size
        /// </summary>
        static public int DefaultFontSize {
            get { return defaultFontSize; }
            set { defaultFontSize = value; }
        }



        Core.Layout.Label geometryLabel=new Core.Layout.Label();

        /// <summary>
        /// gets or set geometry label
        /// </summary>
        public Core.Layout.Label GeometryLabel {
            get { return geometryLabel; }
            set { geometryLabel = value; }
        }
        /// <summary>
        /// gets the geometry of the label
        /// </summary>
        public override GeometryObject GeometryObject {
            get { return GeometryLabel; }
            set { GeometryLabel = (Core.Layout.Label) value; }
        }
    }
}

