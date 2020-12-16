using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// 2 by 3 matrix of plane affine transformations
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    public class PlaneTransformation {
        
        readonly double[][] elements = new double[2][];
        

        /// <summary>
        /// the matrix elements
        /// </summary>
        internal double[][] Elements {
            get { return elements; }
        }
        /// <summary>
        /// i,j th element
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional")]
        public double this[int rowIndex, int columnIndex] {
            get { return Elements[rowIndex][columnIndex]; }
            set { Elements[rowIndex][columnIndex] = value; }
        }

        /// <summary>
        /// constructs the an identity transformation
        /// </summary>
        public PlaneTransformation() {
            elements[0] = new double[3];
            elements[1] = new double[3];
            this[0, 0] = this[1, 1] = 1; //create a unit transform
        }

        /// <summary>
        /// first row, second row
        /// </summary>
        /// <param name="matrixElement00">0,0</param>
        /// <param name="matrixElement01">0,1</param>
        /// <param name="matrixElement02">0,2</param>
        /// <param name="matrixElement10">1,0</param>
        /// <param name="matrixElement11">1,1</param>
        /// <param name="matrixElement12">1,2</param>
        [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
        public PlaneTransformation(double matrixElement00, double matrixElement01, double matrixElement02,
            double matrixElement10, double matrixElement11, double matrixElement12) {
            elements[0] = new double[3];
            elements[1] = new double[3];
            elements[0][0] = matrixElement00; elements[0][1] = matrixElement01; elements[0][2] = matrixElement02;
            elements[1][0] = matrixElement10; elements[1][1] = matrixElement11; elements[1][2] = matrixElement12;
        }
        
        /// <summary>
        /// the matrix by point multiplication
        /// </summary>
        /// <param name="transformation"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        static public Point operator *(PlaneTransformation transformation, Point point) {
            return Multiply(transformation, point);
        }
        /// <summary>
        /// Point by matrix multiplication
        /// </summary>
        /// <param name="transformation"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        static public Point Multiply(PlaneTransformation transformation, Point point) {
            if (transformation != null)
                return new Point(transformation[0, 0] * point.X + transformation[0, 1] * point.Y + transformation[0, 2], transformation[1, 0] * point.X + transformation[1, 1] * point.Y + transformation[1, 2]);
            return new Point();
        }
        /// <summary>
        /// matrix matrix multiplication
        /// </summary>
        /// <param name="transformation"></param>][
        /// <param name="transformation0"></param>
        /// <returns></returns>
        static public PlaneTransformation operator *(PlaneTransformation transformation, PlaneTransformation transformation0) {
            return Multiply(transformation, transformation0);
        }

        /// <summary>
        /// matrix matrix multiplication
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public PlaneTransformation Multiply(PlaneTransformation a, PlaneTransformation b) {
            if (a != null && b != null)
                return new PlaneTransformation(
                   a[0, 0] * b[0, 0] + a[0, 1] * b[1, 0], a[0, 0] * b[0, 1] + a[0, 1] * b[1, 1], a[0, 0] * b[0, 2] + a[0, 1] * b[1, 2] + a[0, 2],
                   a[1, 0] * b[0, 0] + a[1, 1] * b[1, 0], a[1, 0] * b[0, 1] + a[1, 1] * b[1, 1], a[1, 0] * b[0, 2] + a[1, 1] * b[1, 2] + a[1, 2]);
            return null;
        }

        /// <summary>
        /// matrix divided by matrix
        /// </summary>
        /// <param name="transform0"></param>
        /// <param name="transform1"></param>
        /// <returns></returns>
        static public PlaneTransformation operator /(PlaneTransformation transform0, PlaneTransformation transform1) {
            if (transform0 != null && transform1 != null)
                return transform0 * transform1.Inverse;
            return null;

        }

        /// <summary>
        /// Divid matrix by a matrix
        /// </summary>
        /// <param name="transformation0"></param>
        /// <param name="transformation1"></param>
        /// <returns></returns>
        static public PlaneTransformation Divide(PlaneTransformation transformation0, PlaneTransformation transformation1) {
            return transformation0 / transformation1;
        }

/// <summary>
/// returns the inversed matrix
/// </summary>
        public PlaneTransformation Inverse {
            get {
                double det = Elements[0][0] * Elements[1][1] - Elements[1][0] * Elements[0][1];
//                if (ApproximateComparer.Close(det, 0))
//                    throw new InvalidOperationException();//"trying to reverse a singular matrix");

                double a00 = Elements[1][1] / det;
                double a01 = -Elements[0][1] / det;
                double a10 = -Elements[1][0] / det;
                double a11 = Elements[0][0] / det;
                double a02 = -a00 * Elements[0][2] - a01 * Elements[1][2];
                double a12 = -a10 * Elements[0][2] - a11 * Elements[1][2];


                return new PlaneTransformation(a00, a01, a02, a10, a11, a12);
            }
        }

        /// <summary>
        /// unit matrix
        /// </summary>
        static public PlaneTransformation UnitTransformation {
            get { return new PlaneTransformation(1, 0, 0, 0, 1, 0); }
        }

        /// <summary>
        /// Rotation matrix
        /// </summary>
        /// <param name="angle">the angle of rotation</param>
        /// <returns></returns>
        static public PlaneTransformation Rotation(double angle) {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new PlaneTransformation(cos, -sin, 0, sin, cos, 0);
        }

        /// <summary>
        /// checks if the matrix is an identity one
        /// </summary>
        public bool IsIdentity {
            get {
                return
             ApproximateComparer.Close(Elements[0][0], 1) &&
             ApproximateComparer.Close(Elements[0][1], 0) &&
             ApproximateComparer.Close(Elements[0][2], 0) &&
             ApproximateComparer.Close(Elements[1][0], 0) &&
             ApproximateComparer.Close(Elements[1][1], 1) &&
             ApproximateComparer.Close(Elements[1][2], 0);
            }
        }

        /// <summary>
        /// returns the point of the matrix offset
        /// </summary>
        public Point Offset {
            get {
                return new Point(Elements[0][2], Elements[1][2]);
            }
        }

        
        /// <summary>
        /// clones the transform
        /// </summary>
        /// <returns></returns>
        public PlaneTransformation Clone() {
            return new PlaneTransformation(this[0, 0], this[0, 1], this[0, 2], this[1, 0], this[1, 1], this[1, 2]);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        public static PlaneTransformation ScaleAroundCenterTransformation(double scale, Point center) {
            /*var toOrigin = new PlaneTransformation(1, 0, -center.X, 0, 1, -center.Y);
            var scaleTr = new PlaneTransformation(scale, 0, 0,
                                                  0, scale, 0);
            var toCenter = new PlaneTransformation(1, 0, center.X, 0, 1, center.Y);
            var t = toCenter*scaleTr*toOrigin;
            return t;*/
            var d = 1 - scale;
            return new PlaneTransformation(scale, 0, d * center.X, 0, scale, d * center.Y);
        }

        public static PlaneTransformation ScaleAroundCenterTransformation(double xScale, double yScale, Point center)
        {
            /*var toOrigin = new PlaneTransformation(1, 0, -center.X, 0, 1, -center.Y);
            var scaleTr = new PlaneTransformation(scale, 0, 0,
                                                  0, scale, 0);
            var toCenter = new PlaneTransformation(1, 0, center.X, 0, 1, center.Y);
            var t = toCenter*scaleTr*toOrigin;
            return t;*/
            var dX = 1 - xScale;
            var dY = 1 - yScale;
            return new PlaneTransformation(xScale, 0, dX * center.X, 0, yScale, dY * center.Y);
        }
    }
}
