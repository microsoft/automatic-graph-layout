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

        public DTextLabel(DObject parentPar)
            : this(parentPar, new DrawingLabel())
        {
        }

        public DTextLabel(DObject parentPar, string text)
            : this(parentPar)
        {
            Text = text;
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
                DealWithLabelContentChanged();
            }
        }

        public override void MeasureLabel()
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
            }

            base.MeasureLabel();
        }
    }

    public class DNestedGraphLabel : DLabel
    {
        public DNestedGraphLabel(DObject parentPar, FrameworkElement content)
            : base(parentPar, content)
        {
            Graphs = new List<DGraph>();
            if (content is DGraph)
            {
                Graphs.Add(content as DGraph);
                (content as DGraph).ParentObject = this;
            }
        }

        public List<DGraph> Graphs { get; set; }

        private Point PointZero = new Point();
        internal Point GetDGraphOffset(DGraph g)
        {
            if (g == Content)
                return PointZero;

            // This may throw an ArgumentException if the graph has not been loaded into the visual tree.
            GeneralTransform transform = g.TransformToVisual(this);
            
            Point ret = transform.Transform(PointZero);
            return ret;
        }
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

        public DLabel(DObject parentPar)
            : this(parentPar, new DrawingLabel())
        {
        }

        public DLabel(DObject parentPar, FrameworkElement content)
            : this(parentPar, new DrawingLabel(), content)
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
            if ((label.Owner is DrawingNode) && ((label.Owner as DrawingNode).GeometryObject as GeometryNode).BoundaryCurve != null)
            {
                Label.GeometryLabel.Center = ((Label.Owner as DrawingNode).GeometryObject as GeometryNode).Center;
                MakeVisual();
            }
            else if ((label.Owner is DrawingEdge) && ((label.Owner as DrawingEdge).GeometryObject as GeometryEdge).Curve != null)
            {
                Label.GeometryLabel.Center = ((Label.Owner as DrawingEdge).GeometryObject as GeometryEdge).BoundingBox.Center;
                MakeVisual();
            }

            //if (content != null)
            //content.SizeChanged += content_SizeChanged;

            Canvas.SetZIndex(this, 10001);
        }

        void content_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var s = e.NewSize;
            if (s == e.PreviousSize)
                return;
            if (s.Width > 100000 || s.Height > 100000)
            {
                (sender as FrameworkElement).Measure();
                s = (sender as FrameworkElement).RenderSize;
            }
            DealWithLabelContentChanged(s);
        }

        protected void DealWithLabelContentChanged()
        {
            if (Content is FrameworkElement)
            {
                FrameworkElement fe = Content as FrameworkElement;
                DealWithLabelContentChanged(fe.RenderSize);
            }
        }

        protected void DealWithLabelContentChanged(System.Windows.Size newSize)
        {
            Label.Width = newSize.Width;
            Label.Height = newSize.Height;
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
                SetValue(Canvas.LeftProperty, owner.BoundingBox.Center.X - Label.Width / 2.0);
                SetValue(Canvas.TopProperty, owner.BoundingBox.Center.Y - Label.Height / 2.0);
            }
        }

        public virtual void MeasureLabel()
        {
            if (Content is FrameworkElement)
            {
                FrameworkElement fe = Content as FrameworkElement;
                fe.Measure();
                Label.Width = fe.ActualWidth;
                Label.Height = fe.ActualHeight;
            }
            Width += Margin.Left + Margin.Right;
            Height += Margin.Top + Margin.Bottom;
            Label.Width += Margin.Left + Margin.Right;
            Label.Height += Margin.Top + Margin.Bottom;

            //this.Measure();
        }

        public override void MakeVisual()
        {
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