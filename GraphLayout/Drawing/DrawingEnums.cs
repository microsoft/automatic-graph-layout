using System;
using System.Collections.Generic;
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

        internal static string ColorToString(string attr, string c) {

            if (c == "None")
                return "";
            else
                return attr + c;

        }


        
        public static string ConcatWithComma(params string[] s) {
            return ConcatWithDelimeter(",", s);
        }


        public static string ConcatWithDelimeter(string delimeter, params string[] s) {
            List<string> ns = new List<string>();
            foreach(var str in s) {
                if (!string.IsNullOrEmpty(str))
                    ns.Add(str);
            }
            // ns does not have empty strings or nulls
            if (ns.Count == 0)
                return "";
            if (ns.Count == 1)
                return ns[0];
            string ret = ns[0];
            for (int i = 1; i < ns.Count; i++)
                ret += delimeter + ns[i];
            return ret;
        }


        public static string ConcatWithLineEnd(params string[] s) {
            return ConcatWithDelimeter("\r\n", s);
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

