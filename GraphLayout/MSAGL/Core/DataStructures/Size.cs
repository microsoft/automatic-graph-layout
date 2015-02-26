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

namespace Microsoft.Msagl.Core.DataStructures
{


  /// <summary>
  /// Size structure
  /// </summary>
  public struct Size
  {
    double width;
    /// <summary>
    /// width
    /// </summary>
    public double Width
    {
      get { return width; }
      set { width = value; }
    }
    double height;
    /// <summary>
    /// Height
    /// </summary>
    public double Height
    {
      get { return height; }
      set { height = value; }
    }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public Size(double width, double height)
    {
      this.width = width;


      this.height = height;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="s"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static Size operator /(Size s, double d) { return new Size(s.Width / d, s.Height / d); }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="s"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static Size operator *(Size s, double d) { return new Size(s.Width * d, s.Height * d); }


      /// <summary>
      /// padding the size ( from both sides!)
      /// </summary>
      /// <param name="padding"></param>
      public void Pad(double padding) {
          width += 2*padding;
          height += 2*padding;
      }
  }


}
