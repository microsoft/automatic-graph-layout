using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
#pragma warning disable 0660, 0661

    /// <summary>
    /// Color structure
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes"), Serializable]
    public struct Color {
        byte a;
/// <summary>
/// constructor with alpha and red, green, bluee components
/// </summary>
/// <param name="a"></param>
/// <param name="r"></param>
/// <param name="g"></param>
/// <param name="b"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "r"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "g"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
        public Color(byte a, byte r, byte g, byte b) {
            this.a = a;
            this.r = r;
            this.g = g;
            this.b = b;
        }
        /// <summary>
        /// opaque color
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "r"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "g"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
        public Color(byte r, byte g, byte b) {
            this.a = 255;
            this.r = r;
            this.g = g;
            this.b = b;
        }

        /// <summary>
        /// alpha - transparency
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "A")]
        public byte A {
            get { return a; }
            set { a = value; }

        }


        byte r;
        /// <summary>
        /// red
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "R")]
        public byte R {
            get { return r; }
            set { r = value; }
        }
        byte g;
        /// <summary>
        /// green
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "G")]
        public byte G {
            get { return g; }
            set { g = value; }
        }
        byte b;
        /// <summary>
        /// blue
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "B")]
        public byte B {
            get { return b; }
            set { b = value; }
        }
        ///<summary>
        ///</summary>
        ///<param name="i"></param>
        ///<returns></returns>
        public static string Xex(int i) {
            string s = Convert.ToString(i, 16);
            if (s.Length == 1)
                return "0" + s;

            return s.Substring(s.Length - 2, 2);
        }
/// <summary>
/// ==
/// </summary>
/// <param name="a"></param>
/// <param name="b"></param>
/// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
        public static bool operator ==(Color a, Color b) {
            return a.a == b.a && a.r == b.r && a.b == b.b && a.g == b.g;
        }
/// <summary>
/// !=
/// </summary>
/// <param name="a"></param>
/// <param name="b"></param>
/// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
        public static bool operator !=(Color a, Color b) {
            return a.a != b.a || a.r != b.r || a.b != b.b || a.g != b.g;
        }
/// <summary>
/// ToString
/// </summary>
/// <returns></returns>
        public override string ToString() {
            return "\"#" + Xex(R) + Xex(G) + Xex(B) + (this.A == 255 ? "" : Xex(A)) + "\"";
        }
/// <summary>
/// 
/// </summary>
        static public Color AliceBlue { get { return new Color(255, 240, 248, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color AntiqueWhite { get { return new Color(255, 250, 235, 215); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Aqua { get { return new Color(255, 0, 255, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Aquamarine { get { return new Color(255, 127, 255, 212); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Azure { get { return new Color(255, 240, 255, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Beige { get { return new Color(255, 245, 245, 220); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Bisque { get { return new Color(255, 255, 228, 196); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Black { get { return new Color(255, 0, 0, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color BlanchedAlmond { get { return new Color(255, 255, 235, 205); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Blue { get { return new Color(255, 0, 0, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color BlueViolet { get { return new Color(255, 138, 43, 226); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Brown { get { return new Color(255, 165, 42, 42); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color BurlyWood { get { return new Color(255, 222, 184, 135); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color CadetBlue { get { return new Color(255, 95, 158, 160); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Chartreuse { get { return new Color(255, 127, 255, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Chocolate { get { return new Color(255, 210, 105, 30); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Coral { get { return new Color(255, 255, 127, 80); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color CornflowerBlue { get { return new Color(255, 100, 149, 237); } }
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cornsilk")]
        static public Color Cornsilk { get { return new Color(255, 255, 248, 220); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Crimson { get { return new Color(255, 220, 20, 60); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Cyan { get { return new Color(255, 0, 255, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkBlue { get { return new Color(255, 0, 0, 139); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkCyan { get { return new Color(255, 0, 139, 139); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkGoldenrod { get { return new Color(255, 184, 134, 11); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkGray { get { return new Color(255, 169, 169, 169); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkGreen { get { return new Color(255, 0, 100, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkKhaki { get { return new Color(255, 189, 183, 107); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkMagenta { get { return new Color(255, 139, 0, 139); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkOliveGreen { get { return new Color(255, 85, 107, 47); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkOrange { get { return new Color(255, 255, 140, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkOrchid { get { return new Color(255, 153, 50, 204); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkRed { get { return new Color(255, 139, 0, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkSalmon { get { return new Color(255, 233, 150, 122); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkSeaGreen { get { return new Color(255, 143, 188, 139); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkSlateBlue { get { return new Color(255, 72, 61, 139); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkSlateGray { get { return new Color(255, 47, 79, 79); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkTurquoise { get { return new Color(255, 0, 206, 209); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DarkViolet { get { return new Color(255, 148, 0, 211); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DeepPink { get { return new Color(255, 255, 20, 147); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DeepSkyBlue { get { return new Color(255, 0, 191, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DimGray { get { return new Color(255, 105, 105, 105); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color DodgerBlue { get { return new Color(255, 30, 144, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Firebrick { get { return new Color(255, 178, 34, 34); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color FloralWhite { get { return new Color(255, 255, 250, 240); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color ForestGreen { get { return new Color(255, 34, 139, 34); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Fuchsia { get { return new Color(255, 255, 0, 255); } }
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Gainsboro")]
        static public Color Gainsboro { get { return new Color(255, 220, 220, 220); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color GhostWhite { get { return new Color(255, 248, 248, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Gold { get { return new Color(255, 255, 215, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Goldenrod { get { return new Color(255, 218, 165, 32); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Gray { get { return new Color(255, 128, 128, 128); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Green { get { return new Color(255, 0, 128, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color GreenYellow { get { return new Color(255, 173, 255, 47); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Honeydew { get { return new Color(255, 240, 255, 240); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color HotPink { get { return new Color(255, 255, 105, 180); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color IndianRed { get { return new Color(255, 205, 92, 92); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Indigo { get { return new Color(255, 75, 0, 130); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Ivory { get { return new Color(255, 255, 255, 240); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Khaki { get { return new Color(255, 240, 230, 140); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Lavender { get { return new Color(255, 230, 230, 250); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LavenderBlush { get { return new Color(255, 255, 240, 245); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LawnGreen { get { return new Color(255, 124, 252, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LemonChiffon { get { return new Color(255, 255, 250, 205); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightBlue { get { return new Color(255, 173, 216, 230); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightCoral { get { return new Color(255, 240, 128, 128); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightCyan { get { return new Color(255, 224, 255, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightGoldenrodYellow { get { return new Color(255, 250, 250, 210); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightGray { get { return new Color(255, 211, 211, 211); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightGreen { get { return new Color(255, 144, 238, 144); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightPink { get { return new Color(255, 255, 182, 193); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightSalmon { get { return new Color(255, 255, 160, 122); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightSeaGreen { get { return new Color(255, 32, 178, 170); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightSkyBlue { get { return new Color(255, 135, 206, 250); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightSlateGray { get { return new Color(255, 119, 136, 153); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightSteelBlue { get { return new Color(255, 176, 196, 222); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LightYellow { get { return new Color(255, 255, 255, 224); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Lime { get { return new Color(255, 0, 255, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color LimeGreen { get { return new Color(255, 50, 205, 50); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Linen { get { return new Color(255, 250, 240, 230); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Magenta { get { return new Color(255, 255, 0, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Maroon { get { return new Color(255, 128, 0, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumAquamarine { get { return new Color(255, 102, 205, 170); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumBlue { get { return new Color(255, 0, 0, 205); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumOrchid { get { return new Color(255, 186, 85, 211); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumPurple { get { return new Color(255, 147, 112, 219); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumSeaGreen { get { return new Color(255, 60, 179, 113); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumSlateBlue { get { return new Color(255, 123, 104, 238); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumSpringGreen { get { return new Color(255, 0, 250, 154); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumTurquoise { get { return new Color(255, 72, 209, 204); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MediumVioletRed { get { return new Color(255, 199, 21, 133); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MidnightBlue { get { return new Color(255, 25, 25, 112); } }
                /// <summary>
        /// 
        /// </summary>
        static public Color MintCream { get { return new Color(255, 245, 255, 250); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color MistyRose { get { return new Color(255, 255, 228, 225); } }
                /// <summary>
        /// 
        /// </summary>
        static public Color Moccasin { get { return new Color(255, 255, 228, 181); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color NavajoWhite { get { return new Color(255, 255, 222, 173); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Navy { get { return new Color(255, 0, 0, 128); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color OldLace { get { return new Color(255, 253, 245, 230); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Olive { get { return new Color(255, 128, 128, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color OliveDrab { get { return new Color(255, 107, 142, 35); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Orange { get { return new Color(255, 255, 165, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color OrangeRed { get { return new Color(255, 255, 69, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Orchid { get { return new Color(255, 218, 112, 214); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color PaleGoldenrod { get { return new Color(255, 238, 232, 170); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color PaleGreen { get { return new Color(255, 152, 251, 152); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color PaleTurquoise { get { return new Color(255, 175, 238, 238); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color PaleVioletRed { get { return new Color(255, 219, 112, 147); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color PapayaWhip { get { return new Color(255, 255, 239, 213); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color PeachPuff { get { return new Color(255, 255, 218, 185); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Peru { get { return new Color(255, 205, 133, 63); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Pink { get { return new Color(255, 255, 192, 203); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Plum { get { return new Color(255, 221, 160, 221); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color PowderBlue { get { return new Color(255, 176, 224, 230); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Purple { get { return new Color(255, 128, 0, 128); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Red { get { return new Color(255, 255, 0, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color RosyBrown { get { return new Color(255, 188, 143, 143); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color RoyalBlue { get { return new Color(255, 65, 105, 225); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color SaddleBrown { get { return new Color(255, 139, 69, 19); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Salmon { get { return new Color(255, 250, 128, 114); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color SandyBrown { get { return new Color(255, 244, 164, 96); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color SeaGreen { get { return new Color(255, 46, 139, 87); } }
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SeaShell")]
        static public Color SeaShell { get { return new Color(255, 255, 245, 238); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Sienna { get { return new Color(255, 160, 82, 45); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Silver { get { return new Color(255, 192, 192, 192); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color SkyBlue { get { return new Color(255, 135, 206, 235); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color SlateBlue { get { return new Color(255, 106, 90, 205); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color SlateGray { get { return new Color(255, 112, 128, 144); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Snow { get { return new Color(255, 255, 250, 250); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color SpringGreen { get { return new Color(255, 0, 255, 127); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color SteelBlue { get { return new Color(255, 70, 130, 180); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Tan { get { return new Color(255, 210, 180, 140); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Teal { get { return new Color(255, 0, 128, 128); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Thistle { get { return new Color(255, 216, 191, 216); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Tomato { get { return new Color(255, 255, 99, 71); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Transparent { get { return new Color(0, 255, 255, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Turquoise { get { return new Color(255, 64, 224, 208); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Violet { get { return new Color(255, 238, 130, 238); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Wheat { get { return new Color(255, 245, 222, 179); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color White { get { return new Color(255, 255, 255, 255); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color WhiteSmoke { get { return new Color(255, 245, 245, 245); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color Yellow { get { return new Color(255, 255, 255, 0); } }
        /// <summary>
        /// 
        /// </summary>
        static public Color YellowGreen { get { return new Color(255, 154, 205, 50); } }
    }
}
