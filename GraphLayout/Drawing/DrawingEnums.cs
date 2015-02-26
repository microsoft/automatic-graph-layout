/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.ComponentModel;

namespace Microsoft.Msagl.Drawing {
    internal sealed class Utils {

        Utils() { }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool ContainsStyle(Style style, System.Collections.ArrayList styles) {
            foreach (Style s in styles)
                if (s == style)
                    return true;

            return false;
        }






        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        public static string ShapeToString(string attr, Shape shape) {
            return attr + shape.ToString().ToLower();
        }


        /// <summary>
        /// Quotes the string
        /// </summary>
        /// <param name="s">string to quote</param>
        /// <returns>quoted string</returns>
        public static string Quote(string s) { return "\"" + s + "\""; }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        //static int ToByte(double c){
        //    return (int)(255.0*c+0.5);
        //}

        //  static string Xex(int i){
        //      string s=Convert.ToString(i,16);
        //if(s.Length==1)
        //          return "0"+s;

        //      return s.Substring(s.Length-2,2);
        //  }

        internal static string ColorToString(string attr, string c) {

            if (c == "None")
                return "";
            else
                return attr + c;

        }


        static string ConcatWithDelimeter(string delimeter, string[] s, int offset) {

            if (offset == s.Length)
                return "";

            string ret = ConcatTwoWithDelimeter(delimeter, s[offset], ConcatWithDelimeter(delimeter, s, offset + 1));

            return ret;

        }

        public static string ConcatWithComma(params string[] s) {
            string ret = ConcatWithDelimeter(",", s, 0);
            return ret;
        }


        public static string ConcatWithDelimeter(string delimeter, params string[] s) {
            return ConcatWithDelimeter(delimeter, s, 0);
        }


        public static string ConcatWithLineEnd(params string[] s) {
            return ConcatWithDelimeter("\r\n", s);
        }

        static string ConcatTwoWithDelimeter(string delimeter, string s0, string s1) {

            if (String.IsNullOrEmpty(s0))
                return s1;

            if (String.IsNullOrEmpty(s0))
                return s0;

            return s0 + delimeter + s1;
        }


    }



    /// <summary>
    /// Styles enumeration
    /// </summary>
    public enum Style {
        ///<summary>
        ///The default style - solid.
        ///</summary>
        None,
        /// <summary>
        /// 
        /// </summary>
        Dashed,
        /// <summary>
        /// 
        /// </summary>
        Solid,
        /// <summary>
        /// not supported
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Invis")]
        Invis,
        /// <summary>
        /// 
        /// </summary>
        Bold,
        /// <summary>
        /// not supported
        /// </summary>
        Filled,
        /// <summary>
        /// not supported
        /// </summary>
        Diagonals,
        /// <summary>
        /// not supported
        /// </summary>
        Dotted,
        /// <summary>
        /// not supported
        /// </summary>
        Rounded
    }


    /// <summary>
    /// http://www.graphviz.org/cvs/doc/info/attrs.html#k:arrowType
    /// </summary>
    public enum ArrowStyle {
        ///<summary>
        ///The default.
        ///</summary>
        NonSpecified,
        /// <summary>
        /// The default.
        /// </summary>
        None,
        /// <summary>
        /// The default
        /// </summary>
        Normal,
        /// <summary>
        /// Looks like a tee
        /// </summary>
        Tee,
        /// <summary>
        /// Diamond (UML symbol for a Containment)
        /// </summary>
        Diamond,
        /// <summary>
        /// ODiamond (UML symbol for an Aggregation)
        /// </summary>
        ODiamond,
        /// <summary>
        /// Generalization (UML symbol for a Generalization)
        /// </summary>
        Generalization
    }
    /// <summary>
    /// http://www.graphviz.org/cvs/doc/info/attrs.html#k:dirType
    /// </summary>
    public enum EdgeDirection {
        ///<summary>
        ///
        ///</summary>
        NonSpecified,
        /// <summary>
        /// Not supported.
        /// </summary>
        Forward,
        /// <summary>
        /// Not supported.
        /// </summary>
        Back,
        /// <summary>
        /// Not supported.
        /// </summary>
        Both,
        /// <summary>
        /// Not supported.
        /// </summary>
        None
    }






    /// <summary>
    /// http://www.graphviz.org/cvs/doc/info/attrs.html#aa:orientation
    /// </summary>
    public enum Orientation {
        /// <summary>
        /// 
        /// </summary>
        Portrait,
        /// <summary>
        /// 
        /// </summary>
        Landscape
    }

    /// <summary>
    /// Layer constrain enum: is not used for the time being
    /// </summary>
    public enum Layer {
        /// <summary>
        /// 
        /// </summary>
        Undefined,
        /// <summary>
        /// 
        /// </summary>
        Same,
        /// <summary>
        /// 
        /// </summary>
        Min,
        /// <summary>
        /// 
        /// </summary>
        Max,
        /// <summary>
        /// 
        /// </summary>
        Source,
        /// <summary>
        /// 
        /// </summary>
        Sink,

    }

    /// <summary>
    /// http://www.graphviz.org/cvs/doc/info/attrs.html#a:ratio
    /// </summary>
    public enum Ratio {
        /// <summary>
        /// 
        /// </summary>
        Fill,
        /// <summary>
        /// 
        /// </summary>
        Auto,
        /// <summary>
        /// 
        /// </summary>
        Compress,
        /// <summary>
        /// 
        /// </summary>
        Expand
    }

    /// <summary>
    /// http://www.graphviz.org/cvs/doc/info/attrs.html#a:labeljust
    /// </summary>
    public enum LabelJustification {
        /// <summary>
        /// 
        /// </summary>
        Left,
        /// <summary>
        /// 
        /// </summary>
        Right
    }

    /// <summary>
    /// http://www.graphviz.org/cvs/doc/info/attrs.html#a:labelloc
    /// </summary>
    public enum LabelLocation {
        /// <summary>
        /// 
        /// </summary>
        Top,
        /// <summary>
        /// 
        /// </summary>
        Bottom
    }
}

