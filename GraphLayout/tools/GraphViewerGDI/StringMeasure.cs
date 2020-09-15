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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// 
    /// </summary>
    sealed public class StringMeasure {
        StringMeasure() { }

        static Graphics graphics;
        static Font defaultFont;
/// <summary>
/// 
/// </summary>
/// <param name="text"></param>
/// <param name="font"></param>
/// <param name="width"></param>
/// <param name="height"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        static public void MeasureWithFont(string text, Font font, out double width, out double height) {
            if (String.IsNullOrEmpty(text)) {
                width = 0;
                height = 0;
                return;
            }

            if (graphics == null)
                graphics = (new Form()).CreateGraphics();

            Measure(text, font, graphics, out width, out height);
        }


        static internal void Measure(string text,
          Font font, object graphics_, out double width, out double height) {
            Graphics graphics = graphics_ as Graphics;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            if (font == null)
                if (defaultFont == null)
                    font =
                        defaultFont =
                        new Font(Drawing.Label.DefaultFontName, Drawing.Label.DefaultFontSize);
                else
                    font = defaultFont;

            using (StringFormat sf = StringFormat.GenericTypographic) {
                SizeF s = graphics.MeasureString(text, font, 1000000, sf);
                width = s.Width;
                height = s.Height;
            }
        }
    }
}
