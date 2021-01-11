#if TEST_MSAGL
using System;
using Microsoft.Msagl.Core.Geometry.Curves;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Msagl.DebugHelpers {
    ///<summary>
    ///</summary>
    [Serializable]
    public class DebugCurve  {
        public int Pen { get; set; }
        ///<summary>
        ///</summary>
        public string Color { get; set; }
        /// <summary>
        /// Filling Color of the Shape.
        /// </summary>
        public string FillColor { get; set; }
        /// <summary>
        /// color transparency
        /// </summary>
        public byte Transparency { get; set; }

        double width = 1;

        ///<summary>
        ///</summary>
        public ICurve Curve { get; set; }

        ///<summary>
        ///</summary>
        public double Width {
            get { return width; }
            set { width = value; }
        }

        ///<summary>
        ///</summary>
        ///<param name="transparency"></param>
        ///<param name="width"></param>
        ///<param name="curve"></param>
        ///<param name="color"></param>
        ///<param name="id"></param>
        public DebugCurve(byte transparency, double width, string color, ICurve curve, object id)
            : this(width, color, curve, id) {
            Transparency = transparency;            
        }

        ///<summary>
        ///</summary>
        ///<param name="transparency"></param>
        ///<param name="width"></param>
        ///<param name="curve"></param>
        ///<param name="color"></param>
        ///<param name="dashArray"></param>
        public DebugCurve(byte transparency, double width, string color, ICurve curve, double [] dashArray)
            : this(width, color, curve, null) {
            Transparency = transparency;
            DashArray = dashArray;
        }

        ///<summary>
        ///</summary>
        ///<param name="transparency"></param>
        ///<param name="width"></param>
        ///<param name="curve"></param>
        ///<param name="color"></param>
        public DebugCurve(byte transparency, double width, string color, ICurve curve)
            : this(width, color, curve, null)
        {
            Transparency = transparency;
        }
        /// <summary>
        /// </summary>
        /// <param name="transparency"></param>
        /// <param name="width"></param>
        /// <param name="color"></param>
        /// <param name="fillColor"></param>
        /// <param name="curve"></param>
        public DebugCurve(byte transparency, double width, string color, string fillColor, ICurve curve)
            : this(width, color, fillColor, curve, null) {
           Transparency = transparency;
        }

        ///<summary>
        ///</summary>
        ///<param name="width"></param>
        ///<param name="color"></param>
        ///<param name="curve"></param>
        public DebugCurve(double width, string color, ICurve curve):this(width,color,curve,null)
        {
            
        }
        ///<summary>
        ///</summary>
        ///<param name="width"></param>
        ///<param name="color"></param>
        ///<param name="curve"></param>
        ///<param name="id"></param>
        public DebugCurve(double width, string color, ICurve curve, object id) {
            Transparency = 255;
            Width = width;
            Color = color;
            Curve = curve;
            Label = id;
        }
        /// <summary>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="color"></param>
        /// <param name="fillColor"></param>
        /// <param name="curve"></param>
        /// <param name="id"></param>
        public DebugCurve(double width, string color,string fillColor, ICurve curve, object id): this(width, color, curve, null) {
            FillColor = fillColor;
        }

        ///<summary>
        ///</summary>
        public object Label { get; set; }

        ///<summary>
        ///</summary>
        ///<param name="curve"></param>
        ///<param name="color"></param>
        public DebugCurve(string color, ICurve curve):this(1, color, curve, null){}

        ///<summary>
        ///</summary>
        ///<param name="curve"></param>
        ///<param name="pen"></param>
        public DebugCurve(double pen, ICurve curve) : this(pen, "black", curve, null){}

        ///<summary>
        ///</summary>
        ///<param name="curve"></param>
        ///<param name="id"></param>
        public DebugCurve(ICurve curve, object id) : this(1, "black", curve, id) { }

        ///<summary>
        ///</summary>
        ///<param name="color"></param>
        ///<param name="curve"></param>
        ///<param name="id"></param>
        public DebugCurve(string color, ICurve curve, object id) : this(1, color, curve, id) { }

        
        ///<summary>
        ///</summary>
        ///<param name="curve"></param>
        public DebugCurve(ICurve curve) : this(1, "black", curve, null) { }

        /// <summary>
        /// color strings for debugging
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public static readonly string[] Colors = {
                                "DeepSkyBlue", "IndianRed", "Orange", "Gold", "DarkRed", "Plum",
                                                       "Red", "Violet", "Indigo", "Yellow", "OrangeRed", "Tomato", "Purple"
                                                       , "SaddleBrown", "Green", "Navy", "Aqua", "Pink", "Bisque", "Black",
                                                       "BlanchedAlmond", "Blue", "BlueViolet", "Brown", "Lime", "BurlyWood"
                                                       , "Chocolate", "Coral", "CornflowerBlue", "Cornsilk", "Crimson",
                                                       "Cyan", "CadetBlue", "Chartreuse", "DarkBlue", "DarkCyan",
                                                       "DarkGoldenrod", "DarkGray", "DarkGreen", "DarkKhaki", "DarkMagenta"
                                                       , "DarkOliveGreen", "DarkOrange", "DarkOrchid", "DarkSalmon",
                                                       "DarkSeaGreen", "DarkSlateBlue", "DarkSlateGray", "DarkTurquoise",
                                                       "DarkViolet", "DeepPink", "DimGray", "DodgerBlue", "Firebrick",
                                                      "FloralWhite", "ForestGreen", "Fuchsia", "CodeAnalysis", "Gainsboro"
                                                       , "GhostWhite", "Goldenrod", "Gray", "GreenYellow", "Honeydew",
                                                       "HotPink", "Ivory", "Lavender", "LavenderBlush", "LawnGreen",
                                                       "LemonChiffon", "LightBlue", "LightCoral", "LightCyan",
                                                       "LightGoldenrodYellow", "LightGray", "LightGreen", "LightPink",
                                                       "LightSalmon", "LightSeaGreen", "LightSkyBlue", "LightSlateGray",
                                                       "LightSteelBlue", "LightYellow", "LimeGreen", "Linen", "Magenta",
                                                       "Maroon", "MediumAquamarine", "MediumBlue", "MediumOrchid",
                                                       "MediumPurple", "MediumSeaGreen", "MediumSlateBlue",
                                                       "MediumSpringGreen", "MediumTurquoise", "MediumVioletRed",
                                                       "MidnightBlue", "MintCream", "MistyRose", "Moccasin", "NavajoWhite",
                                                       "OldLace", "Olive", "OliveDrab", "Orchid", "PaleGoldenrod",
                                                       "PaleGreen", "PaleTurquoise", "PaleVioletRed", "PapayaWhip",
                                                       "PeachPuff", "Peru", "PowderBlue", "RosyBrown", "RoyalBlue",
                                                       "Salmon", "SandyBrown", "SeaGreen", "CodeAnalysis", "SeaShell",
                                                       "Sienna", "Silver", "SkyBlue", "SlateBlue", "SlateGray", "Snow",
                                                       "SpringGreen", "SteelBlue", "Tan", "Teal", "Thistle", "Transparent",
                                                       "Turquoise", "Aquamarine", "Azure", "Beige", "Wheat", "White",
                                                       "WhiteSmoke", "YellowGreen", "Khaki", "AntiqueWhite"
                          };

        ///<summary>
        /// the pattern of dashes
        ///</summary>
        public double [] DashArray;
    }

}
#endif
