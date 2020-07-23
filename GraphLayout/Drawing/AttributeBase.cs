using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using P2=Microsoft.Msagl.Core.Geometry.Point;


namespace Microsoft.Msagl.Drawing{
    /// <summary>
    /// Base class for attribute hierarchy.
    /// Some of the attributes are present just for DOT compatibility and not fully supported.  
    /// </summary>
    [Serializable]
    public abstract class AttributeBase{
        
        static CultureInfo uSCultureInfo = new CultureInfo("en-US");


        Color color;

        /// <summary>
        /// An id of the entity.
        /// </summary>
        string id;

        /// <summary>
        /// The width of a node border or an edge.
        /// </summary>
        internal double lineWidth = 1;

        internal List<Style> styles = new List<Style>();

        /// <summary>
        /// a default constructor
        /// </summary>
        protected AttributeBase(){
            color = new Color(0, 0, 0); // black
        }

        
        /// <summary>
        /// The current culture. Not tested with another culture.
        /// </summary>
        public static CultureInfo USCultureInfo{
            get { return uSCultureInfo; }
            set { uSCultureInfo = value; }
        }

        /// <summary>
        /// A color of the object.
        /// </summary>
        public Color Color{
            get { return color; }
            set { 
                color = value;
                RaiseVisualsChangedEvent();
            }
        }

        /// <summary>
        /// An array of attribute styles.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public IEnumerable<Style> Styles{
            get { return styles; }
        }

        /// <summary>
        /// the ID of the entity
        /// </summary>
        public string Id{
            get { return id; }
            set { id = value; }
        }

        ///<summary>
        ///Influences border width of clusters, border width of nodes
        /// and edge thickness.
        ///</summary>
        public virtual double LineWidth{
            get { return lineWidth; }
            set{
                lineWidth = value;
                RaiseVisualsChangedEvent();
            }
        }


        ///<summary>
        ///the URI of the entity, it seems not to be present in DOT
        ///</summary>
        public string Uri { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler VisualsChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="style"></param>
        public void AddStyle(Style style){
            styles.Add(style);
            RaiseVisualsChangedEvent();
        }

        void RaiseVisualsChangedEvent(){
            if (VisualsChanged != null)
                VisualsChanged(this, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="style"></param>
        public void RemoveStyle(Style style){
            styles.Remove(style);
            RaiseVisualsChangedEvent();
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearStyles(){
            styles.Clear();
            RaiseVisualsChangedEvent();
        }

        internal void RaiseVisualsChangedEvent(object sender, EventArgs args){
            if (VisualsChanged != null)
                VisualsChanged(sender, args);
        }


        
        
        
        internal string IdToString(){
            if (String.IsNullOrEmpty(Id))
                return "";

            return "id=" + Utils.Quote(Id);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString"
            )]
        internal string StylesToString(string delimeter){
            var al = new List<string>();

            if (lineWidth != -1)
                al.Add("style=\"setlinewidth(" + lineWidth + ")\"");


            if (styles != null){
                foreach (Style style in styles)
                    al.Add("style=" + Utils.Quote(style.ToString()));
            }

            var s = al.ToArray();

            string ret = Utils.ConcatWithDelimeter(delimeter, s);

            return ret;
        }
    }
}