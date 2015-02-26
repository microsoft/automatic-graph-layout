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
using System.Net;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public static class FrameworkExtensions
    {
        // Performs Measure and Arrange steps, so that the element's ActualHeight and ActualWidth are properly calculated. Note that setting the alignment is required, otherwise
        // some elements will just grow indefinitely if they're left with Stretch as their alignment.
        public static void Measure(this FrameworkElement fe)
        {
            if (fe == null)
                return;
            VerticalAlignment oldV = fe.VerticalAlignment;
            HorizontalAlignment oldH = fe.HorizontalAlignment;
            fe.VerticalAlignment = VerticalAlignment.Top;
            fe.HorizontalAlignment = HorizontalAlignment.Left;
            fe.UpdateLayout();
            fe.Measure(new Size(double.MaxValue, double.MaxValue));
            fe.Arrange(new Rect(0.0, 0.0, double.MaxValue, double.MaxValue));
            fe.VerticalAlignment = oldV;
            fe.HorizontalAlignment = oldH;
        }

        public static string ToStringDebug(this GeometryGraph graph)
        {
            using (StringWriter sw = new StringWriter())
            {
                sw.WriteLine("Nodes:");
                foreach (Node n in graph.Nodes)
                    sw.WriteLine(n.BoundingBox == null ? "no bb" : n.BoundingBox.ToString());
                sw.WriteLine("Edges:");
                foreach (Edge e in graph.Edges)
                    sw.WriteLine(e.BoundingBox == null ? "no bb" : e.BoundingBox.ToString());
                return sw.ToString();
            }
        }
    }
}
