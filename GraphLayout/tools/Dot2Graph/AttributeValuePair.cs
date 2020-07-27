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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core.DataStructures;
using Color = System.Drawing.Color;
using P2 = Microsoft.Msagl.Core.Geometry.Point;
using System.Text.RegularExpressions;
using CoupleLabelBaseAttr =  System.Tuple<Microsoft.Msagl.Drawing.Label, Microsoft.Msagl.Drawing.AttributeBase>;
using CoupleLabelGraphAttr = System.Tuple<Microsoft.Msagl.Drawing.Label, Microsoft.Msagl.Drawing.GraphAttr>;
using CoupleLabelNodeAttr =  System.Tuple<Microsoft.Msagl.Drawing.Label, Microsoft.Msagl.Drawing.NodeAttr>;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Label = Microsoft.Msagl.Drawing.Label;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;


namespace Dot2Graph {
    internal enum AttributeTypeEnum {
        Ignore,
        Error,
        Location,
        Label,
        LabelAngle,
        LabelFontSize,
        Style,
        Color,
        RGBColor,
        LineWidth,
        Fontcolor,
        Fontname,
        FontSize,
        Pos,
        LabelLocation,
        BBox,
        Margin,
        Width,
        Height,
        Size,
        Shape,
        Rects,
        Sides,
        Distortion,
        Orientation,
        Skew,
        EdgeDirection, //for edge
        Arrowhead,
        ArrowTail,
        ArrowSize,
        Page,
        Regular,
        Center,
        Weight,
        Id,
        BGColor,
        FillColor,
        SameHead,
        SameTail,
        Constraint,
        LayerDirection,
        CELLPADDING,
        CELLSPACING,
        Border,
        XRad,
        YRad,
        Padding,
        Uri
    }

    /// <summary>
    /// Summary description for AttributeValuePair.
    /// </summary>
    /// 
    public class AttributeValuePair {
        internal AttributeTypeEnum attributeTypeEnum;
        object val;

       
        static double TryParseDouble(string val, string attribute)
        {
            double result = 0;
            NumberStyles st = NumberStyles.AllowDecimalPoint |
                              NumberStyles.AllowLeadingSign |
                              NumberStyles.AllowParentheses;                          
            if (Double.TryParse(val, st, AttributeBase.USCultureInfo, out result)) return result;
            throw new Exception(String.Format("Value {0} for attribute '{1}' could not be parsed as a double", val, attribute));
        }

        class Styles
        {
            public List<Style> styles;
            public int lineWidth;

            public Styles()
            {
                styles = new List<Style>();
                lineWidth = 0;
            }
            public void Add(Style s)
            {
                styles.Add(s);
            }
        }

        internal static AttributeValuePair CreateFromsStrings(string attr, string val) {
            var av = new AttributeValuePair();

            string name = attr.ToLower();

            switch (name) {
                case "uri":
                    av.attributeTypeEnum = AttributeTypeEnum.Uri;
                    av.val = val;
                    break;
                case "constraint":
                    av.attributeTypeEnum = AttributeTypeEnum.Constraint;
                    av.val = val != "false";
                    break;
                case "label":
                    av.attributeTypeEnum = AttributeTypeEnum.Label;
                    av.val = val;
                    break;
                case "size":
                    av.attributeTypeEnum = AttributeTypeEnum.Size;
                    av.val = ParseP2(val);
                    break;
                case "style":
                    {
                        Styles styles = new Styles();
                        av.val = styles;
                        string[] vals = Split(val);
                        av.attributeTypeEnum = AttributeTypeEnum.Style;
                        for (int i = 0; i < vals.Length; ++i)
                        {
                            switch (vals[i])
                            {
                                case "filled": styles.Add(Style.Filled); break;
                                case "dashed": styles.Add(Style.Dashed); break;
                                case "solid": styles.Add(Style.Solid); break;
                                case "invis":
                                case "inviz":
                                case "hidden": styles.Add(Style.Invis); break;
                                case "bold": styles.Add(Style.Bold); break;
                                case "diagonals": styles.Add(Style.Diagonals); break;
                                case "dotted": styles.Add(Style.Dotted); break;
                                case "rounded": styles.Add(Style.Rounded); break;                                
                                default:
                                    int lw;
                                    if (ParseLineWidth(vals[i], out lw)) {
                                        styles.lineWidth = lw;
                                        break; 
                                    }
                                    throw new Exception(String.Format("unexpected style '{0}'", val));
                            }
                        }
                        break;
                    }
                case "color":
                    av.attributeTypeEnum = AttributeTypeEnum.Color;
                    av.val = val;
                    break;
                case "pos":
                    av.attributeTypeEnum = AttributeTypeEnum.Pos;
                    av.val = CreatePosData(val);
                    break;
                case "lp":
                    av.attributeTypeEnum = AttributeTypeEnum.LabelLocation;
                    av.val = CreatePosData(val);
                    break;
                case "fontname":
                    av.attributeTypeEnum = AttributeTypeEnum.Fontname;
                    av.val = val;
                    break;
                case "fontsize":
                    av.attributeTypeEnum = AttributeTypeEnum.FontSize;
                    av.val = (int)TryParseDouble(val, name);
                    break;
                case "rankdir":
                    av.attributeTypeEnum = AttributeTypeEnum.LayerDirection;
		            switch(val) {
		            case "LR": av.val = LayerDirection.LR; break;
		            case "TB":
		            case "TD": av.val = LayerDirection.TB; break;
		            case "RL": av.val = LayerDirection.RL; break;
		            case "BT": av.val = LayerDirection.BT; break;
		            default:
                        throw new Exception("layerdir value \"" + val + "\" is not supported");
		            }
                    break;
                case "labeldouble":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "bb":
                    av.attributeTypeEnum = AttributeTypeEnum.BBox;
                    av.val = CreateBBoxFromString(val);
                    break;
                case "fontcolor":
                    av.attributeTypeEnum = AttributeTypeEnum.Fontcolor;
                    av.val = val;
                    break;
                case "margin":
                    {
                        // a pair x,y (inches)
                        av.attributeTypeEnum = AttributeTypeEnum.Margin;
                        string[] vals = Split(val);
                        av.val = TryParseDouble(Get(vals, 0), name);
                        break;
                    }
                case "width":
                    av.attributeTypeEnum = AttributeTypeEnum.Width;
                    av.val = TryParseDouble(val, name); 
                    break;
                case "height":
                    av.attributeTypeEnum = AttributeTypeEnum.Height;
                    av.val = TryParseDouble(val, name); 
                    break;
                case "shape":
                    av.attributeTypeEnum = AttributeTypeEnum.Shape;
                    val = val.ToLower();
		            switch(val) {
		            case "box":           av.val = Shape.Box; break;
		            case "circle":        av.val = Shape.Circle; break;
		            case "ellipse":       av.val = Shape.Ellipse; break;
		            case "plaintext":     av.val = Shape.Plaintext; break;
		            case "point":         av.val = Shape.Point; break;
		            case "record":        av.val = Shape.Record; break;
		            case "mdiamond":      av.val = Shape.Mdiamond; break;
		            case "polygon":       av.val = Shape.Polygon; break;
		            case "doublecircle":  av.val = Shape.DoubleCircle; break;
		            case "house":         av.val = Shape.House; break;
		            case "invhouse":      av.val = Shape.InvHouse; break;
		            case "parallelogram": av.val = Shape.Parallelogram; break;
		            case "octagon":       av.val = Shape.Octagon; break;
		            case "tripleoctagon": av.val = Shape.TripleOctagon; break;
                    case "triangle":      av.val = Shape.Triangle; break;
                    case "trapezium":     av.val = Shape.Trapezium; break;
                    case "msquare":       av.val = Shape.Msquare; break;
                    case "diamond":       av.val = Shape.Diamond; break;
                    case "hexagon":       av.val = Shape.Hexagon;break;

                    default:              av.val = Shape.Ellipse; break;
		            }
                    break;
                case "ordering":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "rects": {
                    av.attributeTypeEnum = AttributeTypeEnum.Rects;
                    var al = new ArrayList();
                    av.val = al;

                    var points = Split(val);
                    var leftbottom = new P2();
                    var righttop = new P2();
                    for (int i = 0; i < points.Length-3; i += 4) {
                        leftbottom.X = (int)GetNumber(points[i]);
                        leftbottom.Y = (int)GetNumber(points[i+1]);
                        righttop.X = (int)GetNumber(points[i+2]);
                        righttop.Y = (int)GetNumber(points[i+3]);
                        al.Add(new Rectangle(leftbottom, righttop));
                    }
                    }
                    break;
                case "sides":
                    av.attributeTypeEnum = AttributeTypeEnum.Sides;
                    av.val = Int32.Parse(val);
                    break;
                case "distortion":
                    av.attributeTypeEnum = AttributeTypeEnum.Distortion;
                    av.val = val == "" ? 0.0 : TryParseDouble(val, name); 
                    break;
                case "orientation":
                    av.attributeTypeEnum = AttributeTypeEnum.Orientation;
                    av.val = val;
                    break;
                case "skew":
                    av.attributeTypeEnum = AttributeTypeEnum.Skew;
                    av.val = TryParseDouble(val, name); 
                    break;
                case "layer":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "nodesep":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "layersep":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "taillabel":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "ratio":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "minlen":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "splines":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "overlap":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "labeldistance":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "peripheries":
                case "fname":
                case "subkind":
                case "kind":
                case "pname":
                case "headlabel":
                case "samearrowhead":
                case "sametail":
                case "samehead":
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
                case "arrowtail":
                    av.attributeTypeEnum =AttributeTypeEnum.ArrowTail;
                    av.val = ParseArrowStyle(val);
                    break;
                case "arrowhead":
                    av.attributeTypeEnum = AttributeTypeEnum.Arrowhead;
                    av.val = ParseArrowStyle(val);
                    break;
                case "dir":
                    av.attributeTypeEnum = AttributeTypeEnum.EdgeDirection;
                    if (val == "back")
                        av.val = EdgeDirection.Back;
                    else if (val == "forward")
                        av.val = EdgeDirection.Forward;
                    else if (val == "both")
                        av.val = EdgeDirection.Both;
                    else if (val == "none")
                        av.val = EdgeDirection.None;
                    else
                        throw new Exception("unexpected edge direction '" + val + "'");
                    break;
                case "page":
                    av.attributeTypeEnum = AttributeTypeEnum.Page;
                    av.val = ParseP2(val);
                    break;
                case "regular":
                    av.attributeTypeEnum = AttributeTypeEnum.Regular;
                    av.val = val == "1";
                    break;
                case "center":
                    av.attributeTypeEnum = AttributeTypeEnum.Center;
                    av.val = val == "true";
                    break;
                case "wt":
                    av.attributeTypeEnum = AttributeTypeEnum.Weight;
                    av.val = Int32.Parse(val);
                    break;
                case "id":
                    av.attributeTypeEnum = AttributeTypeEnum.Id;
                    av.val = val;
                    break;
                case "labelfontsize":
                    av.attributeTypeEnum = AttributeTypeEnum.LabelFontSize;
                    av.val = val;
                    break;
                case "arrowsize":
                    av.attributeTypeEnum = AttributeTypeEnum.ArrowSize;
                    av.val = val;
                    break;
                case "labelangle":
                    av.attributeTypeEnum = AttributeTypeEnum.ArrowSize;
                    av.val = val;
                    break;
                case "weight":
                    av.attributeTypeEnum = AttributeTypeEnum.Weight;
                    av.val = val;
                    break;
                case "bgcolor":
                    av.attributeTypeEnum = AttributeTypeEnum.BGColor;
                    av.val = val;
                    break;
                case "fillcolor":
                    av.attributeTypeEnum = AttributeTypeEnum.FillColor;
                    av.val = val;
                    break;
                case "cellpadding":
                    av.attributeTypeEnum = AttributeTypeEnum.CELLPADDING;
                    av.val = Int32.Parse(val, AttributeBase.USCultureInfo);
                    break;
                case "border":
                    av.attributeTypeEnum = AttributeTypeEnum.Border;
                    av.val = Int32.Parse(val, AttributeBase.USCultureInfo);
                    break;
                case "xrad":
                    av.attributeTypeEnum = AttributeTypeEnum.XRad;
                    av.val = float.Parse(val, CultureInfo.InvariantCulture);
                    break;
                case "yrad":
                    av.attributeTypeEnum = AttributeTypeEnum.YRad;
                    av.val = float.Parse(val, CultureInfo.InvariantCulture);
                    break;
                case "padding":
                    av.attributeTypeEnum = AttributeTypeEnum.Padding;
                    av.val = float.Parse(val, CultureInfo.InvariantCulture);
                    break;
                default:
                    av.attributeTypeEnum = AttributeTypeEnum.Ignore;
                    break;
            }
            //throw new Exception("attribute \""+ attr +"\" is not supported");


            return av;
        }

        static ArrowStyle ParseArrowStyle(string s)
        {
            switch (s)
            {
                case "none": return ArrowStyle.None;
                case "normal": return  ArrowStyle.Normal; 
                default: return ArrowStyle.Normal; 
            }
            #region comment
            /*
              TBD: use reflection to lookup ArrowStyle instead of this.
              case "dot": return ArrowStyle.Dot; 
              case "odot": return ArrowStyle.ODot; 
              case "inv": return ArrowStyle.Inv; 
              case "invodot":  return ArrowStyle.InvODot; 
              case "invdot": return ArrowStyle.InvDot; 
              case "open": return ArrowStyle.Open; 
              case "halfopen": return ArrowStyle.Halfopen; 
              case "empty": return ArrowStyle.Empty; 
              case "invempty": return ArrowStyle.Invempty; 
              case "diamond": return ArrowStyle.Diamond; 
              case "odiamond": return ArrowStyle.ODiamond; 
              case "box": return ArrowStyle.Box; 
              case "boxbox": return ArrowStyle.BoxBox; 
              case "obox": return ArrowStyle.OBox; 
              case "tee":  return ArrowStyle.Tee; 
              case "crow": return ArrowStyle.Crow; 
              case "boxbox": return ArrowStyle.BoxBox; 
              case "None":  return ArrowStyle.None; 
              case "lbox":  return ArrowStyle.LBox; 
              case "lboxlbox": return ArrowStyle.LBoxLBox; 
              case "rbox": return ArrowStyle.RBox; 
              case "rboxrbox":  return ArrowStyle.RBoxRBox; 
              case "olbox": return ArrowStyle.OLBox; 
              case "orbox": return ArrowStyle.ORBox; 
              case "olboxolbox": return ArrowStyle.OLBoxOLBox; 
              case "orboxorbox": return ArrowStyle.ORBoxORBox; 
              case "oboxobox": return ArrowStyle.OBoxOBox; 
              case "crowcrow": return ArrowStyle.CrowCrow; 
              case "lcrowlcrow": return ArrowStyle.LCrowLCrow; 
              case "lcrow":  return ArrowStyle.LCrow; 
              case "rcrow": return ArrowStyle.RCrow; 
              case "rcrowrcrow": return ArrowStyle.RCrowRCrow; 
              case "orinv":  return ArrowStyle.ORInv; 
              case "orinvorinv": return ArrowStyle.ORInvORInv; 
              case "oinv": return ArrowStyle.OInv; 
              case "oinvoinv": return ArrowStyle.OInvOInv; 
              case "nonenone": return ArrowStyle.NoneNone; 
              case "normalnormal": return ArrowStyle.NormalNormal; 
              case "lnormal":  return ArrowStyle.Lnormal; 
              case "lnormallnormal":  return ArrowStyle.LNormalLNormal; 
              case "rnormal": return ArrowStyle.RNormal; 
              case "rnormalrnormal": return ArrowStyle.RNormalRNormal; 
              case "olnormal":  return ArrowStyle.OLNormal; 
              case "olnormalolnormal":   return ArrowStyle.OLNormalOLNormal; 
              case "ornormal":  return ArrowStyle.ORNormal; 
              case "ornormalornormal": return ArrowStyle.ORNormalORNormal; 
              case "onormal": return ArrowStyle.ONormal; 
              case "onormalonormal": return ArrowStyle.ONormalONormal; 
              case "teetee":   return ArrowStyle.TeeTee; 
              case "ltee": return ArrowStyle.LTee; 
              case "lteeltee": return ArrowStyle.LTeeLTee; 
              case "rtee": return ArrowStyle.RTee; 
              case "rteertee":return ArrowStyle.RTeeRTee; 
              case "vee": return ArrowStyle.Vee; 
              case "veevee": return ArrowStyle.VeeVee; 
              case "lvee": return ArrowStyle.LVee; 
              case "lveelvee": return ArrowStyle.LVeeLVee; 
              case "rvee": return ArrowStyle.RVee; 
              case "rveervee": return ArrowStyle.RVeeRVee; 
              case "diamonddiamond": return ArrowStyle.diamonddiamond; 
              case "ldiamond": return ArrowStyle.ldiamond; 
              case "ldiamondldiamond": return ArrowStyle.ldiamondldiamond; 
              case "rdiamond": return ArrowStyle.rdiamond; 
              case "rdiamondrdiamond": return ArrowStyle.rdiamondrdiamond; 
              case "oldiamond": return ArrowStyle.oldiamond; 
              case "oldiamondoldiamond": return ArrowStyle.oldiamondoldiamond; 
              case "ordiamond": return ArrowStyle.ordiamond; 
              case "ordiamondordiamond": return ArrowStyle.ordiamondordiamond; 
              case "odiamond": return ArrowStyle.odiamond; 
              case "odiamondodiamond": return ArrowStyle.odiamondodiamond; 
              case "dotdot": return ArrowStyle.dotdot; 
              case "odotodot": return ArrowStyle.odotodot; 
              case "invinv":return ArrowStyle.invinv; 
              case "linv": return ArrowStyle.linv; 
              case "linvlinv": return ArrowStyle.linvlinv; 
              case "rinv": return ArrowStyle.rinv; 
              case "rinvrinv":return ArrowStyle.rinvrinv; 
              case "olinv":return ArrowStyle.olinv; 
              case "olinvolinv": return ArrowStyle.olinvolinv; 
              case "orinv": return ArrowStyle.orinv; 
              case "orinvorinv": return ArrowStyle.orinvorinv; 
              case "oinv": return ArrowStyle.oinv; 
              case "oinvoinv": return ArrowStyle.oinvoinv; 
             
              else if (val == "dot")
                av.val = ArrowStyle.Dot;
              else if (val == "odot")
                av.val = ArrowStyle.ODot;
              else if (val == "inv")
                av.val = ArrowStyle.Inv;
              else if (val == "invodot")
                av.val = ArrowStyle.InvODot;
              else if (val == "invdot")
                av.val = ArrowStyle.InvDot;
              else if (val == "open")
                av.val = ArrowStyle.Open;
              else if (val == "halfopen")
                av.val = ArrowStyle.Halfopen;
              else if (val == "empty")
                av.val = ArrowStyle.Empty;
              else if (val == "invempty")
                av.val = ArrowStyle.Invempty;
              else if (val == "diamond")
                av.val = ArrowStyle.Diamond;
              else if (val == "odiamond")
                av.val = ArrowStyle.ODiamond;
              else if (val == "box")
                av.val = ArrowStyle.Box;
              else if (val == "obox")
                av.val = ArrowStyle.OBox;
              else if (val == "tee")
                av.val = ArrowStyle.Tee;
              else if (val == "crow")
                av.val = ArrowStyle.Crow;
              else if (val == "lbox")
                av.val = ArrowStyle.LBox;
              else if (val == "boxbox")
                av.val = ArrowStyle.BoxBox;
              */
           
            #endregion
        }


        static Rectangle CreateBBoxFromString(string s) {
            var leftbottom = new P2();
            var righttop = new P2();

            if (s == "")
                return new Rectangle(new P2(0, 0), new P2(0, 0));

            var points = Split(s);
            leftbottom.X = (int)GetNumber(Get(points,0));
            leftbottom.Y = (int)GetNumber(Get(points,1));
            righttop.X = (int)GetNumber(Get(points,2));
            righttop.Y = (int)GetNumber(Get(points,3));

            return new Rectangle(leftbottom, righttop);
        }

        static int ToByte(double c) {
            var ret = (int) (255.0*c + 0.5);
            if (ret > 255)
                ret = 255;
            else if (ret < 0)
                ret = 0;
            return ret;
        }

        static int FromX(string s) {
            return Int32.Parse(s, NumberStyles.AllowHexSpecifier, AttributeBase.USCultureInfo);
        }

        public static CoupleLabelGraphAttr AddGraphAttrs(CoupleLabelGraphAttr couple, ArrayList arrayList) {
            if (couple == null) couple = new CoupleLabelGraphAttr(new Label(), new GraphAttr());


            foreach (AttributeValuePair attrVal in arrayList)
                AddAttributeValuePair(couple, attrVal);

            return couple;
        }

        public static Color MsaglColorToDrawingColor(Microsoft.Msagl.Drawing.Color gleeColor) {
            return Color.FromArgb(gleeColor.A, gleeColor.R, gleeColor.G, gleeColor.B);
        }

        internal static bool ParseLineWidth(string v, out int lw) {
            lw = 0;
            Match m = Regex.Match(v, @"setlinewidth\((\d+)\)");
            if (!m.Success) return false;
            lw = (int)GetNumber(m.Groups[1].Value);
            return true;
        }

        internal static void AddAttributeValuePair(CoupleLabelGraphAttr couple, AttributeValuePair attrVal) {
            if (AddAttributeKeyVal(new CoupleLabelBaseAttr(couple.Item1, couple.Item2), attrVal)) return;

            switch (attrVal.attributeTypeEnum) {
                case AttributeTypeEnum.Center:
                    // a.Center = (bool)attrVal.val;
                    break;
                case AttributeTypeEnum.BGColor:
                    couple.Item2.BackgroundColor = StringToMsaglColor((string) attrVal.val);
                    break;
                case AttributeTypeEnum.Page:
                    // a.Page = (P2)attrVal.val;        
                    break;
                case AttributeTypeEnum.BBox:
                    // a.BoundingBox = (Rectangle)attrVal.val;
                    break;
                case AttributeTypeEnum.Style: 
                    AddStyles(couple.Item2, attrVal.val);
                    break;                   
                case AttributeTypeEnum.Size: {
                    // P2 p = (P2)attrVal.val;
                    //a.size = p;
                }
                    break;
                case AttributeTypeEnum.Orientation: {
                    var s = attrVal.val as String;
                    //if (s == "portrait")
                    //    ;//  a.Orientation = Orientation.Portrait;
                    //else if (s.StartsWith("land"))
                    //    ;// a.Orientation = Orientation.Landscape;
                    //else
                    //    throw new Exception("unexpected \"" + attrVal.attributeTypeEnum.ToString() + "\"");
                }
                    break;
                case AttributeTypeEnum.LayerDirection:
                    couple.Item2.LayerDirection = (LayerDirection) attrVal.val;
                    break;
                case AttributeTypeEnum.CELLPADDING:
                    //a.CellPadding = (int)attrVal.val;
                    break;
                case AttributeTypeEnum.Border:
                    couple.Item2.Border = (int) attrVal.val;
                    break;
                case AttributeTypeEnum.Height: {
                    //              Rectangle r = a.BoundingBox;
                    //              r.Height = (int)attrVal.val;
                    //              a.BoundingBox = r;
                }
                    break;
                case AttributeTypeEnum.Width: {
                    //  Rectangle r = a.BoundingBox;
                    //  r.Width = (int)attrVal.val;
                    //  a.BoundingBox = r;
                    break;
                }
            }
        }


        /// <summary>
        /// Setting attribute from values.
        /// </summary>
        /// <param name="arrayList"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static void AddEdgeAttrs(ArrayList arrayList, Edge edge) {        
            var label = new Label();
            var edgeAttr = new EdgeAttr();
                
            foreach (AttributeValuePair attrVal in arrayList) {
                if (AddAttributeKeyVal(new CoupleLabelBaseAttr(label, edgeAttr), attrVal))
                    continue;
                switch (attrVal.attributeTypeEnum) {
                    case AttributeTypeEnum.LabelLocation:
                        var posData = (PosData) attrVal.val;
                        var cpList = posData.ControlPoints as List<P2>;
                        if (cpList != null)
                            label.GeometryLabel.Center = cpList[0];
                        break;
                    case AttributeTypeEnum.Arrowhead:
                        edgeAttr.ArrowheadAtTarget = (ArrowStyle) attrVal.val;
                        break;
                    case AttributeTypeEnum.Id:
                        edgeAttr.Id = attrVal.val as String;
                        break;
                    case AttributeTypeEnum.ArrowTail:
                        edgeAttr.ArrowheadAtSource = (ArrowStyle) attrVal.val;
                        break;
                    case AttributeTypeEnum.RGBColor:
                        edgeAttr.Color = StringToMsaglColor((string) attrVal.val);
                        break;
                    case AttributeTypeEnum.Label:
                        label.Text = attrVal.val as String;
                        break;
                    case AttributeTypeEnum.Color:
                        edgeAttr.Color = StringToMsaglColor((string) attrVal.val);
                        break;
                    case AttributeTypeEnum.Fontcolor:
                        label.FontColor = StringToMsaglColor((string) attrVal.val);
                        break;
                    case AttributeTypeEnum.Pos: {
                        posData = (PosData) attrVal.val; //consider creating a Microsoft.Msagl.Splines.ICurve here
                        InitGeomEdge(edge);
                        if (posData.ArrowAtSource)
                            edge.GeometryEdge.EdgeGeometry.SourceArrowhead = new Arrowhead {
                                TipPosition = posData.ArrowAtSourcePosition
                            };
                        if (posData.ArrowAtTarget)
                            edge.GeometryEdge.EdgeGeometry.TargetArrowhead = new Arrowhead {
                                TipPosition = posData.ArrowAtTargetPosition
                            };

                        var list = posData.ControlPoints as List<P2>;
                        if (list != null && list.Count%3 == 1)
                            AddBezieSegsToEdgeFromPosData(edge, list);

                        break;
                    }
                    case AttributeTypeEnum.Style:
                        AddStyles(edgeAttr, attrVal.val);
                        break;
                    case AttributeTypeEnum.EdgeDirection:
                        if ((EdgeDirection)attrVal.val == EdgeDirection.Both)
                            edgeAttr.ArrowheadAtSource = ArrowStyle.Normal;
                        break;
                    case AttributeTypeEnum.Weight: 
                        if(attrVal.val is String)
                          edgeAttr.Weight=Int32.Parse( attrVal.val as String,AttributeBase.USCultureInfo);
                        else
                          edgeAttr.Weight=(int) attrVal.val;                    
                        break;
                    case AttributeTypeEnum.Ignore: {}
                        break;
                    case AttributeTypeEnum.ArrowSize: 
                        //Bug                    
                        break;
                    case AttributeTypeEnum.SameTail: 
                        //edgeAttr.Sametail=attrVal.val as String;                    
                        break;
                    case AttributeTypeEnum.SameHead: 
                        //edgeAttr.Samehead=attrVal.val as String;                    
                        break;
                    case AttributeTypeEnum.Constraint: 
                        //do nothing
                        //edgeAttr.Constraint=(bool)attrVal.val;                    
                        break;
                    default:
                        throw new Exception(string.Format("The attribute \"{0}\" is not supported for edges", attrVal.attributeTypeEnum));
                }
            }

            edge.Attr = edgeAttr;
            if (!String.IsNullOrEmpty(label.Text))
                edge.Label = new Label(label.Text) { Owner = edge };
            
        }

        static void AddBezieSegsToEdgeFromPosData(Edge edge, List<P2> list) {
            var curve = new Curve();
            for (int i = 0; i + 3 < list.Count; i += 3)
                curve.AddSegment(new CubicBezierSegment(list[i], list[i + 1], list[i + 2], list[i + 3]));
            InitGeomEdge(edge);
            edge.GeometryEdge.Curve = curve;
        }

        static void InitGeomEdge(Edge edge) {
            if (edge.GeometryEdge == null)
                edge.GeometryEdge = new Microsoft.Msagl.Core.Layout.Edge();
        }

        static void AddStyles(AttributeBase a, object o)
        {
            if (o is Styles)
            {
                Styles styles = o as Styles;
                foreach (var s in styles.styles)
                {
                    a.AddStyle(s);
                }
                if (styles.lineWidth != 0) 
                {
                    a.LineWidth = styles.lineWidth;
                }
            }
        }

        /// <summary>
        /// A convenient way to set attributes.
        /// </summary>
        /// <param name="couple"></param>
        /// <param name="arrayList"></param>
        /// <param name="geomNode"></param>
        /// <param name="nodeAttr"></param>
        /// <returns></returns>
        public static CoupleLabelNodeAttr AddNodeAttrs(CoupleLabelNodeAttr couple, ArrayList arrayList, out Node geomNode) {
            if (couple == null) couple = new CoupleLabelNodeAttr(new Label(), new NodeAttr());
            double width = 0;
            double height = 0;
            P2 pos = new P2();

            var label = couple.Item1;
            var nodeAttr = couple.Item2;

            foreach (AttributeValuePair attrVal in arrayList) {
                if (AddAttributeKeyVal(new CoupleLabelBaseAttr(couple.Item1, nodeAttr), attrVal))
                    continue;

                switch (attrVal.attributeTypeEnum) {
                    case AttributeTypeEnum.Regular: 
                        // nodeAttr.regular=(Boolean)attrVal.val;
                        break;
                    case AttributeTypeEnum.Shape: 
                        nodeAttr.Shape = (Shape) attrVal.val;
                        break;
                    case AttributeTypeEnum.Fontname: 
                        label.FontName = attrVal.val as String;
                        break;
                    case AttributeTypeEnum.Style: 
                        AddStyles(nodeAttr, attrVal.val);
                        break;
                    case AttributeTypeEnum.Fontcolor: 
                        label.FontColor = StringToMsaglColor((string) attrVal.val);
                        break;
                    case AttributeTypeEnum.FillColor: 
                        nodeAttr.FillColor = StringToMsaglColor((string) attrVal.val);
                        break;
                    case AttributeTypeEnum.Width: 
                        width = (double)attrVal.val;
                        break;
                    case AttributeTypeEnum.Height: 
                        height = (double)attrVal.val;
                        break;
                    case AttributeTypeEnum.Pos: 
                           PosData posData = attrVal.val as PosData;
                           pos = (posData.ControlPoints as List<P2>)[0];
                        break;
                    case AttributeTypeEnum.Rects: 
                        //nodeAttr.Rects=attrVal.val as ArrayList;
                        break;
                    case AttributeTypeEnum.Label: 
                        label.Text = attrVal.val as String;
                        break;
                    case AttributeTypeEnum.Sides: 
                        // nodeAttr.sides=(int)attrVal.val;
                        break;
                    case AttributeTypeEnum.Orientation: 
                        //nodeAttr.orientation=Double.Parse(attrVal.val as String,BaseAttr.USCultureInfo);
                        break;
                    case AttributeTypeEnum.Skew: 
                        //nodeAttr.skew=(Double)attrVal.val;
                        break;
                    case AttributeTypeEnum.Distortion: 
                        //nodeAttr.distortion=(Double)attrVal.val;
                        break;
                    case AttributeTypeEnum.Id:
                        // couple.Second.Id = attrVal.val as String; //used for svg, postscript, map only as the docs say
                        break;
                    case AttributeTypeEnum.Ignore: {}
                        break;
                    case AttributeTypeEnum.Weight:
//                        if (attrVal.val is String)
//                            weight = Int32.Parse(attrVal.val as String, AttributeBase.USCultureInfo);
//                        else
//                            weight = (int) attrVal.val;
                        break;
                    case AttributeTypeEnum.XRad: 
                        nodeAttr.XRadius = (float) attrVal.val;
                        break;
                    case AttributeTypeEnum.YRad: 
                        nodeAttr.YRadius = (float) attrVal.val;
                        break;
                    case AttributeTypeEnum.Padding: 
                        nodeAttr.Padding = (float) attrVal.val;
                        break;
                    case AttributeTypeEnum.Margin: 
                        nodeAttr.LabelMargin = (int) ((double) attrVal.val);
                        break;
                    case AttributeTypeEnum.BGColor:
                        break;
                    default:
                        throw new Exception("The attribute \"" + attrVal.attributeTypeEnum + "\" is not supported on nodes");
                }
            }
            geomNode = TryToCreateGeomNode(width, height, pos, nodeAttr);
            
            return couple;
        }

        static Node TryToCreateGeomNode(double width, double height, P2 center, NodeAttr nodeAttr) {
            if (width == 0 || height == 0)
                return null;
           
            var curve = CreateCurveByShape(width, height, center, nodeAttr);
            if (curve != null)
                return new Node(curve);
            return null;
        }

        static ICurve CreateCurveByShape(double width, double height, P2 center, NodeAttr nodeAttr) {
            ICurve curve = null;
            switch (nodeAttr.Shape) {
                case Shape.Diamond:
                    curve = CurveFactory.CreateDiamond(width, height, center);
                    break;
                case Shape.Ellipse:
                    break;
                case Shape.Box:
                    curve = CurveFactory.CreateRectangleWithRoundedCorners(width, height, nodeAttr.XRadius,
                                                                           nodeAttr.YRadius, center);
                    break;
                case Shape.Circle:
                    curve = CurveFactory.CreateCircle(width/2, center);
                    break;
                case Shape.Record:
                    return null;
                case Shape.Plaintext:
                    return null;
                case Shape.Point:
                    break;
                case Shape.Mdiamond:
                    break;
                case Shape.Msquare:
                    break;
                case Shape.Polygon:
                    break;
                case Shape.DoubleCircle:
                    curve = CurveFactory.CreateCircle(width/2, center);
                    break;
                case Shape.House:
                    curve = CurveFactory.CreateHouse(width, height, center);
                    break;
                case Shape.InvHouse:
                    curve = CurveFactory.CreateInvertedHouse(width, height, center);
                    break;
                case Shape.Parallelogram:
                    break;
                case Shape.Octagon:
                    curve = CurveFactory.CreateOctagon(width, height, center);
                    break;
                case Shape.TripleOctagon:
                    break;
                case Shape.Triangle:
                    break;
                case Shape.Trapezium:
                    break;
                case Shape.DrawFromGeometry:
                    break;
#if TEST_MSAGL
                case Shape.TestShape:
                    break;
#endif
                case Shape.Hexagon:
                    curve = CurveFactory.CreateHexagon(width, height, center);
                    break;

            }
            return curve ?? CurveFactory.CreateEllipse(width/2, height/2, center);
        }


        /// <summary>
        /// Parsing from a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static P2 ParseP2(string s) {
            string[] numbers = Split(s);
            var p = new P2();
            p.X = GetNumber(Get(numbers,0));
            p.Y = GetNumber(Get(numbers,1));
            return p;
        }

        static string Get(string[] a, int i)
        {
            if (i < a.Length) return a[i];
            return "";
        }
        /// <summary>
        /// Constructs PostData from a string keeping Bezier curve
        /// and the arrows info.
        /// </summary>
        /// <param name="s"></param>
        internal static PosData CreatePosData(string s) {
            string[] tokens = Split(s);
            var ret = new PosData();
            bool eMet = false;
            bool sMet = false;
            for (int i = 0; i + 1 < tokens.Length; ++i)
            {
                string t = tokens[i];
                switch (t)
                {
                    case "e":
                        eMet = true;
                        ret.ArrowAtTarget = true;
                        break;
                    case "s":
                        sMet = true;
                        ret.ArrowAtSource = true;
                        break;
                    default:
                        P2 p = new P2();
                        string x = tokens[i];
                        string y = tokens[i + 1];
                        p.X = Double.Parse(x, AttributeBase.USCultureInfo);
                        p.Y = Double.Parse(y, AttributeBase.USCultureInfo);;
                        if (sMet)
                        {
                            sMet = false;
                            ret.ArrowAtSourcePosition = p;
                        }
                        else if (eMet)
                        {
                            eMet = false;
                            ret.ArrowAtTargetPosition = (P2)p;
                        }
                        else
                            (ret.ControlPoints as List<P2>).Add(p);
                        ++i;
                        break;
                }
            }
            return ret;
        }


        internal static bool AddAttributeKeyVal(CoupleLabelBaseAttr couple, AttributeValuePair attrVal) {
            Label label = couple.Item1;
            AttributeBase attributeBase = couple.Item2;
            switch (attrVal.attributeTypeEnum) {
                case AttributeTypeEnum.Uri:
                    attributeBase.Uri = (string) attrVal.val;
                    break;
                case AttributeTypeEnum.Ignore:
                    break;
                case AttributeTypeEnum.Color:
                    attributeBase.Color = StringToMsaglColor((string) attrVal.val);
                    break;
                case AttributeTypeEnum.LabelFontSize:
                    label.FontSize = Int32.Parse(attrVal.val as String);
                    break;
                case AttributeTypeEnum.Fontcolor:
                    label.FontColor = StringToMsaglColor((string) attrVal.val);
                    break;

                case AttributeTypeEnum.Label:

                    label.Text = attrVal.val as String;
                    break;

                case AttributeTypeEnum.LabelLocation:
                    SetGeomLabelCenter(label, ((List<P2>) ((PosData) attrVal.val).ControlPoints)[0]);
                    break;


                case AttributeTypeEnum.Style:
                    AddStyles(attributeBase, attrVal.val);
                    break;


                case AttributeTypeEnum.RGBColor: {
                    attributeBase.Color = StringToMsaglColor((string) attrVal.val);
                    break;
                }
                case AttributeTypeEnum.FontSize: {
                    label.FontSize = (int) attrVal.val;
                    break;
                }
                case AttributeTypeEnum.Fontname: {
                    label.FontName = attrVal.val as String;
                    break;
                }

                default:
                    return false; //not handled
            }
            return true;
        }

        static void SetGeomLabelCenter(Label label, P2 point) {
            InitGeomLabel(label);
            label.GeometryLabel.Center = point;
        }

        static void InitGeomLabel(Label label) {
            if (label.GeometryLabel == null)
                label.GeometryLabel = new Microsoft.Msagl.Core.Layout.Label();
        }

        public static Microsoft.Msagl.Drawing.Color StringToMsaglColor(string val) {
            return DrawingColorToGLEEColor(GetDrawingColorFromString(val));
        }

        internal static Microsoft.Msagl.Drawing.Color DrawingColorToGLEEColor(Color drawingColor) {
            return new Microsoft.Msagl.Drawing.Color(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }

        static Color GetDrawingColorFromString(string val) {
            // object o=FindInColorTable(val);
            //	if(o==null)	//could be an RGB color

            try {
                if (val.IndexOf(" ") != -1) {
                    string[] nums = Split(val);
                    double H = GetNumber(Get(nums, 0)); //hue
                    double S = GetNumber(Get(nums, 1)); //saturation
                    double V = GetNumber(Get(nums, 2)); //value
                    double r, g, b;

                    H *= 360.0;
                    if (S == 0) r = g = b = V;
                    else {
                        int Hi = ((int) (H + 0.5))/60;
                        double f = H/60.0 - Hi;
                        double p = V*(1.0 - S);
                        double q = V*(1.0 - (S*f));
                        double t = V*(1.0 - (S*(1.0 - f)));

                        if (Hi == 0) {
                            r = V;
                            g = t;
                            b = p;
                        }
                        else if (Hi == 1) {
                            r = q;
                            g = V;
                            b = p;
                        }
                        else if (Hi == 2) {
                            r = p;
                            g = V;
                            b = t;
                        }
                        else if (Hi == 3) {
                            r = p;
                            g = q;
                            b = V;
                        }
                        else if (Hi == 4) {
                            r = t;
                            g = p;
                            b = V;
                        }
                        else if (Hi == 5) {
                            r = V;
                            g = p;
                            b = q;
                        }
                        else throw new Exception("unexpected value of Hi " + Hi);
                    }
                    return Color.FromArgb(ToByte(r), ToByte(g), ToByte(b));
                }
                else if (val[0] == '#') //could be #%2x%2x%2x or #%2x%2x%2x%2x
                    if (val.Length == 7) {
                        int r = FromX(val.Substring(1, 2));
                        int g = FromX(val.Substring(3, 2));
                        int b = FromX(val.Substring(5, 2));

                        return Color.FromArgb(r, g, b);
                    }
                    else if (val.Length == 9) {
                        int r = FromX(val.Substring(1, 2));
                        int g = FromX(val.Substring(3, 2));
                        int b = FromX(val.Substring(5, 2));
                        int a = FromX(val.Substring(7, 2));

                        return Color.FromArgb(a, r, g, b);
                    }
                    else
                        throw new Exception("unexpected color " + val);
                else
                    return FromNameOrBlack(val);
            }
            catch {
                return FromNameOrBlack(val);
            }
        }

        static Color FromNameOrBlack(string val) {
            Color ret = Color.FromName(val);
            if (ret.A == 0 && ret.R == 0 && ret.B == 0 && ret.G == 0) //the name is not recognized
                return Color.Black;
            return ret;
        }

        static string[] Split(string txt)
        {
            return txt.Split(new char[] { ' ', ',', '\n', '\r', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        static double GetNumber(string txt)
        {
            int i;
            double d;
            if (Int32.TryParse(txt, out i))
            {
                return i;
            }
            if (Double.TryParse(txt, out d))
            {
                return d;
            }
            if (txt == "") return 0;
            throw new Exception(String.Format("Cannot convert \"{0}\" to a number", txt));
        }
    }
}