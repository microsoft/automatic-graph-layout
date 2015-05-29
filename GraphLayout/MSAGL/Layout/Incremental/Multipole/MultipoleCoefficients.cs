using System.Diagnostics;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core;

namespace Microsoft.Msagl.Layout.Incremental
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multipole")]
    public class MultipoleCoefficients
    {
        Complex z0;
        Complex[] a;
        int p;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="precision"></param>
        /// <param name="center"></param>
        /// <param name="points"></param>
        public MultipoleCoefficients(int precision, Point center, Point[] points)
        {
#if DET
#else
            this.p = precision;
            z0 = new Complex(center.X, center.Y);
            a = new Complex[precision];
            for (int k = 0; k < precision; ++k)
            {
                a[k] = compute(k, points);
            }
#endif
        }
/// <summary>
/// 
/// </summary>
/// <param name="center"></param>
/// <param name="m1"></param>
/// <param name="m2"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "m")]
        public MultipoleCoefficients(Point center, MultipoleCoefficients m1, MultipoleCoefficients m2)
        {
            ValidateArg.IsNotNull(m1, "m1");
            ValidateArg.IsNotNull(m2, "m2");
            Debug.Assert(m1.p == m2.p);
            this.p = m1.p;
            z0 = new Complex(center.X, center.Y);
            Complex[] m1a = m1.shift(z0), m2a = m2.shift(z0);
            a = new Complex[p];
            for (int i = 0; i < p; ++i)
            {
                a[i] = m1a[i] + m2a[i];
            }
        }
        static double factorial(double n)
        {
            double f = 1;
            for (int i = 2; i <= n; ++i)
            {
                f *= i;
            }
            return f;
        }
        static double binomial(int n, int k)
        {
            return factorial(n) / (factorial(k) * factorial(n - k));
        }
        Complex sum(int l, Complex z0_minus_z1)
        {
            Complex s = new Complex(0.0);
            for (int k = 1; k <= l; ++k)
            {
                Complex bi = new Complex(binomial(l - 1, k - 1));
                s += a[k] * Complex.Pow(z0_minus_z1, l - k) * bi;
            }
            return s;
        }
        Complex[] shift(Complex z1)
        {
            Complex[] b = new Complex[p];
            Complex a0 = b[0] = a[0];
            Complex z0_minus_z1 = z0 - z1;
            for (int l = 1; l < p; ++l)
            {
                Complex lz = new Complex(l);
                b[l] = -a0 * Complex.Pow(z0_minus_z1, l) / lz + sum(l, z0_minus_z1);
            }
            return b;
        }
        /// <summary>
        /// Compute kth multipole coefficient of a set of points ps around a centre z0
        /// </summary>
        /// <param name="k">Coefficient index</param>
        /// <param name="ps">Set of points</param>
        /// <returns></returns>
        private Complex compute(int k, Point[] ps)
        {
            int m = ps.Length;
            Complex ak = new Complex(0.0);
            if (k == 0)
            {
                ak.re = m;
            }
            else
            {
                for (int i = 0; i < m; ++i)
                {
                    Point q = ps[i];
                    Complex pc = new Complex(q.X, q.Y);
                    ak -= Complex.Pow(pc - z0, k);
                }
                ak.divideBy(k);
            }
            return ak;
        }
        /// <summary>
        /// Compute approximate force at point v due to potential energy moments
        /// </summary>
        /// <param name="v"></param>
        /// <returns>Approximate force at v</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "v")]
        public Point ApproximateForce(Point v)
        {
            Complex z = new Complex(v.X, v.Y);
            Complex z_minus_z0 = z - z0;
            Complex fz = a[0] / z_minus_z0;
            Complex z_minus_z0_to_k_plus_1 = z_minus_z0;
            int k = 0;
            while(true) {
                fz -= (a[k] * (double)k) / z_minus_z0_to_k_plus_1;
                ++k;
                if(k==p) {
                    break;
                }
                z_minus_z0_to_k_plus_1 *= z_minus_z0;
            }
            return new Point(fz.re, -fz.im);
        }    
        /// <summary>
        /// Force on point u due to point v.
        /// If v and u at the same position it returns a small vector to separate them
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "u"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "v")]
        public static Point Force(Point u, Point v)
        {
            Point duv = v - u;
            double l = duv.LengthSquared;
            if (l < 0.1)
            {
                if (l != 0) {
                    return duv/0.1;
                }
                return new Point(1, 0);
            }
            return duv / l;
        }

        struct Complex
        {
            public Complex(double re, double im)
            {
                this.re = re;
                this.im = im;
            }
            public Complex(double re)
            {
                this.re = re;
                this.im = 0;
            }
            public static Complex operator +(Complex a, Complex b)
            {
                return (new Complex(a.re + b.re, a.im + b.im));
            }
            public static Complex operator -(Complex a)
            {
                return (new Complex(-a.re, -a.im));
            }
            public static Complex operator -(Complex a, Complex b)
            {
                return (new Complex(a.re - b.re, a.im - b.im));
            }
            public static Complex operator *(Complex a, Complex b)
            {
                return (new Complex(a.re * b.re - a.im * b.im, a.re * b.im + b.re * a.im));
            }
            public static Complex operator *(Complex a, double b)
            {
                return (new Complex(a.re * b, b * a.im));
            }
            public static Complex operator /(Complex a, Complex b)
            {
                double c1, c2, d;
                d = b.re * b.re + b.im * b.im;
                if (d == 0)
                {
                    return (new Complex(0.0));
                }
                c1 = a.re * b.re + a.im * b.im;
                c2 = a.im * b.re - a.re * b.im;
                return (new Complex(c1 / d, c2 / d));
            }
            public void divideBy(double r) {
                re /= r;
                im /= r;
            }
            public static Complex Pow(Complex a, int k)
            {
                Debug.Assert(k >= 0);
                switch (k)
                {
                    // we only really need to hard code 0 here, but giving 1,2 and 3 explicitly also may make it a bit faster
                    case 0: return new Complex(1.0);
                    case 1: return a;
                    case 2: return a * a;
                    case 3: return a * a * a;
#if SHARPKIT //https://github.com/SharpKit/SharpKit/issues/4 integer rounding issue
                    default: return Pow(a, k / 2) * Pow(a, (k / 2) + (k % 2));
#else
                    default: return Pow(a, k / 2) * Pow(a, k / 2 + k % 2);
#endif
                }
            }
            public double re;
            public double im;
        }
    }
}
