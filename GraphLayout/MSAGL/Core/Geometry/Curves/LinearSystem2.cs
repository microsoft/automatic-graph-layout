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
