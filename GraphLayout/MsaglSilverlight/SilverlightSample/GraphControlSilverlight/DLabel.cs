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
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Msagl.Drawing;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
using DrawingLabel = Microsoft.Msagl.Drawing.Label;
using IGeometryLabeledObject = Microsoft.Msagl.Core.Layout.ILabeledObject;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public class DTextLabel : DLabel
    {
        public DTextLabel(DObject parentPar, DrawingLabel label)
            : base(parentPar, label)
        {
        }

        public string Text
        {
            get
            {
                return Label.Text;
            }
            set
            {
                Label.Text = value;
                //MakeVisual();
                DealWithLabelContentChanged();
            }
        }

        public override void MakeVisual()
        {
            if (Label.Text != null && Label.Text.Length > 0)
            {
                TextBlock tb = new TextBlock();
                tb.FontFamily = new FontFamily(Label.FontName);
                tb.FontSize = Label.FontSize;
                tb.Foreground = Label.FontColor == Microsoft.Msagl.Drawing.Color.Black ? DGraph.BlackBrush : new SolidColorBrush(Draw.MsaglColorToDrawingColor(Label.FontColor));
                tb.Text = Label.Text;
                tb.TextAlignment = TextAlignment.Center;
                tb.VerticalAlignment = VerticalAlignment.Center;
                Content = tb;
                Label.Width = Width = tb.ActualWidth;
                Label.Height = Height = tb.ActualHeight;
            }
            else
            {
                Label.Width = Width = 0.0;
                Label.Height = Height = 0.0;
            }
            if (Label.GeometryLabel != null)
            {
                Label.GeometryLabel.Width = Math.Max(1.0, Label.Width);
                Label.GeometryLabel.Height = Math.Max(1.0, Label.Height);
            }

            CenterLabel();
        }

        /*

        public GetNewEditControlDelegate GetNewEditControlDelegate;
        public GetTextFromEditControlDelegate GetTextFromEditControlDelegate;

        protected FrameworkElement GetNewEditControl()
        {
            if (GetNewEditControlDelegate != null)
                return GetNewEditControlDelegate(this);
            return new TextBox() { MinWidth = Math.Max(Width, 60.0), MinHeight = 24.0, Text = (Text == null ? "" : Text) };
        }

        protected string GetTextFromEditControl()
        {
            if (GetTextFromEditControlDelegate != null)
                return GetTextFromEditControlDelegate(this);
            return (EditControl as TextBox).Text;
        }

        public FrameworkElement EditControl{get;set;}
        public bool EditMode
        {
            get
            {
                return EditControl != null;
            }
            set
            {
                if (value)
                {
                    EditControl = GetNewEditControl();
                    Content = EditControl;
                    EditControl.SizeChanged += (sender, args) => MakeVisual();
                }
                else
                {
                    if (EditControl != null)
                    {
                        string newText = GetTextFromEditControl();
                        EditControl = null;
                        Text = newText;
                        DealWithLabelContentChanged();
                    }
                }
            }
        }

        internal override void MakeVisual()
        {
            // Note that Draw.CreateGraphicsPath returns a curve with coordinates in graph space.
            if (Label.Text != null && Label.Text.Length > 0)
            {
                TextBlock tb = new TextBlock();
                tb.FontFamily = new FontFamily(Label.FontName);
                tb.FontSize = Label.FontSize;
                tb.Foreground = new SolidColorBrush(Draw.MsaglColorToDrawingColor(Label.FontColor));
                tb.Text = Label.Text;

                if (!EditMode)
                    Content = tb;
                Label.Width = Width = tb.ActualWidth;
                Label.Height = Height = tb.ActualHeight;
            }
            else
            {
                Label.Width = Width = 0.0;
                Label.Height = Height = 0.0;
            }

            if (EditMode)
            {
                Content = EditControl;
                Width = EditControl.ActualWidth;
                Height = EditControl.ActualHeight;
            }

            CenterLabel();
        }
         */
    }

    /// <summary>
    /// This class represents a rendering label. It contains the Microsoft.Msagl.Drawing.Label and Microsoft.Msagl.Label.
    /// The rendering label is a XAML user control. Its template is stored in Themes/generic.xaml.
    /// </summary>
    public class DLabel : DObject, IViewerObject
    {
        public DLabel(DObject parentPar, DrawingLabel label)
            : this(parentPar, label, null)
        {
        }

        public DLabel(DObject parentPar, DrawingLabel label, FrameworkElement content)
            : base(parentPar)
        {
            DrawingLabel = label;
            if (parentPar != null)
            {
                label.Owner = parentPar.DrawingObject;
                if (label.GeometryLabel != null)
                    label.GeometryLabel.GeometryParent = parentPar.DrawingObject.GeometryObject;
                if (parentPar.DrawingObject.GeometryObject is IGeometryLabeledObject)
                    ((IGeometryLabeledObject)parentPar.DrawingObject.GeometryObject).Label = label.GeometryLabel;
            }
            if (ParentObject is IHavingDLabel)
                ((IHavingDLabel)ParentObject).Label = this;
            Content = content;
            if ((label.Owner is Node) && ((label.Owner as DrawingNode).GeometryObject as GeometryNode).BoundaryCurve != null)
            {
                Label.GeometryLabel.Center = ((Label.Owner as DrawingNode).GeometryObject as GeometryNode).Center;
                MakeVisual();
            }
            else if ((label.Owner is Edge) && ((label.Owner as DrawingEdge).GeometryObject as GeometryEdge).Curve != null)
            {
                Label.GeometryLabel.Center = ((Label.Owner as DrawingEdge).GeometryObject as GeometryEdge).BoundingBox.Center;
                MakeVisual();
            }
        }

        protected void DealWithLabelContentChanged()
        {
            MakeVisual();
            if (ParentObject is DNode)
                ParentGraph.ResizeNodeToLabel(ParentObject as DNode);
        }

        protected void CenterLabel()
        {
            if (Label.Owner == null || (Label.Owner is DrawingEdge && ((Label.Owner as DrawingEdge).GeometryEdge).Curve != null))
            {
                SetValue(Canvas.LeftProperty, Label.BoundingBox.Left);
                SetValue(Canvas.TopProperty, Label.BoundingBox.Bottom);
            }
            else if (Label.Owner is DrawingNode && ((Label.Owner as DrawingNode).GeometryNode).BoundaryCurve != null)
            {
                DrawingNode owner = Label.Owner as DrawingNode;
                SetValue(Canvas.LeftProperty, owner.BoundingBox.Center.X - Width / 2.0);
                SetValue(Canvas.TopProperty, owner.BoundingBox.Center.Y - Height / 2.0);
            }
        }

        public  override void MakeVisual()
        {
            if (Content is FrameworkElement)
            {
                Label.Width = Width = (Content as FrameworkElement).ActualWidth;
                Label.Height = Height = (Content as FrameworkElement).ActualHeight;
                if (Label.GeometryLabel != null)
                {
                    Label.GeometryLabel.Width = Math.Max(1.0, Label.Width);
                    Label.GeometryLabel.Height = Math.Max(1.0, Label.Height);
                }
            }
            
            CenterLabel();
        }

        /// <summary>
        /// delivers the underlying label object
        /// </summary>
        override public DrawingObject DrawingObject
        {
            get { return Label; }
        }

        /// <summary>
        /// gets or set the underlying drawing label
        /// </summary>
        public DrawingLabel DrawingLabel { get; set; }

        /// <summary>
        /// returns the corresponding DrawingNode
        /// </summary>
        public DrawingLabel Label
        {
            get { return this.DrawingLabel; }
        }
    }
}