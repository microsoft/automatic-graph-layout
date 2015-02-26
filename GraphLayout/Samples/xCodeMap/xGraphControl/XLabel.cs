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
﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Routing;

namespace xCodeMap.xGraphControl
{
    internal class XLabel:IViewerObjectX,IInvalidatable {
        private FrameworkElement _visualObject;
        public FrameworkElement VisualObject
        {
            get { return _visualObject; }
        }

        public XLabel(Edge edge)
        {
            if(false)
            {
                TextBlock tb = null;
                if (edge.Label != null)
                {
                    tb = new TextBlock { Text = edge.Label.Text };
                    System.Windows.Size size = CommonX.Measure(tb);
                    tb.Width = size.Width;
                    tb.Height = size.Height;
                    CommonX.PositionElement(tb, edge.Label.Center, 1);
                }

                _visualObject = tb;
            }
            
            DrawingObject = edge;
        }

        public DrawingObject DrawingObject { get; private set; }
        public bool MarkedForDragging { get; set; }
        public event EventHandler MarkedForDraggingEvent;
        public event EventHandler UnmarkedForDraggingEvent;
        public void Invalidate(double scale = 1)
        {
            if (_visualObject != null)
            {
                double fontSize = Math.Min(12 / scale, 12);
                ((TextBlock)_visualObject).FontSize = fontSize;

                _visualObject.InvalidateVisual();
            }
        }
    }
}