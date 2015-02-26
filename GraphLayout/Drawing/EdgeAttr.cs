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
    /// <summary>
    /// Edge layout attributes.
    /// </summary>
#if !SILVERLIGHT
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Attr"), Description("Edge layout attributes."),
    TypeConverterAttribute(typeof(ExpandableObjectConverter))]
#endif
    [Serializable]
    public sealed class EdgeAttr : AttributeBase {
       
        int separation=1;
/// <summary>
/// The separation of the edge in layers. The default is 1.
/// </summary>
        public int Separation {
            get { return separation; }
            set { separation = value; }
        }
  
       
        int weight = 1;

        /// <summary>
        /// Greater weight keeps the edge short
        /// </summary>
        public int Weight {
            get { return weight; }
            set { weight = value; }
        }


        /// <summary>
        /// 
        /// </summary>
        public EdgeAttr() {
            Color = new Color(0, 0, 0);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public EdgeAttr Clone() {
            return MemberwiseClone() as EdgeAttr;
        }


        ArrowStyle arrowheadAtSource = ArrowStyle.NonSpecified;

        /// <summary>
        /// Arrow style; for the moment only the Normal and None are supported.
        /// </summary>
        public ArrowStyle ArrowheadAtSource {
            get { return arrowheadAtSource; }
            set {
                arrowheadAtSource = value;
                RaiseVisualsChangedEvent(this, null);
            }
        }


        /// <summary>
        /// Arrow style; for the moment only a few are supported.
        /// </summary>
        ArrowStyle arrowheadAtTarget = ArrowStyle.NonSpecified;

        /// <summary>
        /// Arrow style; for the moment only the Normal and None are supported.
        /// </summary>
        public ArrowStyle ArrowheadAtTarget {
            get { return arrowheadAtTarget; }
            set {
                arrowheadAtTarget = value;
                RaiseVisualsChangedEvent(this, null);
            }
        }

        double arrowheadLength=10;
        /// <summary>
        /// the length of an arrowhead of the edge
        /// </summary>
        public double ArrowheadLength {
            get { return arrowheadLength; }
            set {
                arrowheadLength = value;
                RaiseVisualsChangedEvent(this, null);
            }
        }
        

        /// <summary>
        /// ToString with a parameter.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        public string ToString(string text) {
            string ret = "";
            if (!String.IsNullOrEmpty(text)) {
                text = text.Replace("\r\n", "\\n");
                ret += "label=" + Utils.Quote(text);
            }


            if (arrowheadAtSource != ArrowStyle.NonSpecified)
                ret = Utils.ConcatWithComma(ret, "arrowhead=" + arrowheadAtSource.ToString().ToLower());


            ret = Utils.ConcatWithComma(ret, Utils.ColorToString("color=", Color.ToString()),
                                StylesToString(","),                              
                                IdToString()
                                );


            return ret;

        }

       
      
   
       
        /// <summary>
        /// Signals if an arrow should be drawn at the end.
        /// </summary>
        public bool ArrowAtTarget {
            get { return ArrowheadAtTarget != ArrowStyle.None; }
        }
        
      
    /// <summary>
    /// is true if need to draw an arrow at the source
    /// </summary>
        public bool ArrowAtSource {
            get { return ! (ArrowheadAtSource == ArrowStyle.NonSpecified || ArrowheadAtSource ==ArrowStyle.None); }
        }
    /// <summary>
    /// gets or sets the position of the arrow head at the source
    /// </summary>
        double length=1;
        /// <summary>
        /// applicable for MDS layouts
        /// </summary>
        public double Length {
            get { return length; }
            set { length = value; }
        }              
    }
}
