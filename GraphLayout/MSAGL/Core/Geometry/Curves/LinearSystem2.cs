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

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// solves a linear system of two equations with to unknown variables
    /// </summary>
    internal class LinearSystem2 {
        LinearSystem2() { }
        static double eps = 1.0E-8;

        internal static double Eps {
            get { return LinearSystem2.eps; }
         //   set { LinearSystem2.eps = value; }
        }

        internal static bool Solve(double a00, double a01, double b0, double a10, double a11, double b1, out double x, out double y) {
            double d = a00 * a11 - a10 * a01;

            if (Math.Abs(d) < Eps) {
                x = y = 0; //to avoid the compiler bug
                return false;
            }

            x = (b0 * a11 - b1 * a01) / d;
            y = (a00 * b1 - a10 * b0) / d;

            return true;

        }
    }
}
