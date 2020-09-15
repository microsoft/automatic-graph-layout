using System;
using System.Linq;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient {
    /// <summary>
    /// Solver for a linear system of equations of the form Ax=b.
    /// </summary>
    public class LinearSystemSolver {
        /// <summary>
        ///     Conjugate Gradient method for solving Sparse linear system of the form Ax=b with an iterative procedure. Matrix A should be positive semi-definite, otherwise several solutions could exist and convergence is not guaranteed.
        ///     see article for algorithm description: An Introduction to the Conjugate Gradient Method
        ///     Without the Agonizing Pain by Jonathan Richard Shewchuk
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <param name="x">initial guess for x</param>
        /// <param name="iMax"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
         static Vector SolveConjugateGradient(SparseMatrix A, Vector b, Vector x, int iMax, double epsilon) {
            Vector r = b - (A*x); // r= b-Ax
            Vector d = r.Clone(); // d=r
            double deltaNew = r*r;
            
            double normRes = Math.Sqrt(deltaNew)/A.NumRow;
            double normRes0 = normRes;
            int i = 0;
            while ((i++) < iMax && normRes > epsilon*normRes0) {
                Vector q = A*d;
                double alpha = deltaNew/(d*q);
                x.Add(alpha*d);
                // correct residual to avoid error accumulation of floating point computation, other methods are possible.
//                if (i == Math.Max(50,(int)Math.Sqrt(A.NumRow)))
//                    r = b - (A*x);
//                else 
                    r.Sub(alpha*q);
                double deltaOld = deltaNew;
                deltaNew = r*r;
                normRes = Math.Sqrt(deltaNew)/A.NumRow; 
                double beta = deltaNew/deltaOld;
                d = r + beta*d;
            }

            return x;
        }

        /// <summary>
        /// Preconditioned Conjugate Gradient Method <see cref="SolveConjugateGradient(Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.SparseMatrix,Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.Vector,Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.Vector,int,double)"/>
        /// Preconditioner: Jacobi Preconditioner.
        /// This method should generally be preferred, due to its faster convergence.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <param name="x"></param>
        /// <param name="iMax"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static Vector SolvePrecondConjugateGradient(SparseMatrix A, Vector b, Vector x, int iMax, double epsilon) {
            //create Jacobi (diagonal) preconditioner
            // M=diagonal(A), M^-1=1/M(i,i), for i=0,...,n
            // since M^-1 is only applied to vectors, we can just keep the diagonals as a vector
            Vector Minv = A.DiagonalPreconditioner();

            Vector r = b - (A*x);
            Vector d = Minv.CompProduct(r);
            double deltaNew = r*d;

            double normRes = Math.Sqrt(r*r) / A.NumRow;
            double normRes0 = normRes;
            int i = 0;
            while ((i++) < iMax && normRes > epsilon * normRes0) {
                Vector q = A*d;
                double alpha = deltaNew/(d*q);
                x.Add(alpha*d);
                // correct residual to avoid error accumulation of floating point computation, other methods are possible.
//                if (i == Math.Max(50, (int)Math.Sqrt(A.NumRow)))
//                    r = b - (A*x);
//                else 
                    r.Sub(alpha*q);
                Vector s = Minv.CompProduct(r);
                double deltaOld = deltaNew;
                normRes = Math.Sqrt(r*r) / A.NumRow; 
                deltaNew = r*s;
                double beta = deltaNew/deltaOld;
                d = s + beta*d;
            }
            return x;
        }



        /// <summary>
        /// Conjugate Gradient Method which is guaranteed to converge in n steps, <see cref="SolveConjugateGradient(Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.SparseMatrix,Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.Vector,Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.Vector,int,double)"/>
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <param name="x"></param>
        /// <param name="iMax"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static double[] SolveConjugateGradient(SparseMatrix A, double[] b, double[] x, int iMax,
                                                      double epsilon) {
            return SolveConjugateGradient(A, new Vector(b), new Vector((double[]) x.Clone()), iMax, epsilon).array;
        }

        /// <summary>
        ///     Preconditioned Conjugate Gradient method, where the preconditioner M is the diagonal of A (Jacobi Preconditioner), <seealso cref="SolvePrecondConjugateGradient(Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.SparseMatrix,Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.Vector,Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient.Vector,int,double)"/>
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <param name="x"></param>
        /// <param name="iMax"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static double[] SolvePrecondConjugateGradient(SparseMatrix A, double[] b, double[] x, int iMax,
                                                             double epsilon) {
            return
                SolvePrecondConjugateGradient(A, new Vector(b), new Vector((double[]) x.Clone()), iMax, epsilon).array;
        }

        /// <summary>
        /// </summary>
        public static void TestConjugateGradientMethod() {
            //matrix A={{4,1}{1,3}}

            var values = new double[4] {4, 1, 1, 3};
            var col_ind = new int[4] {0, 1, 0, 1};
            var row_ptr = new int[3] {0, 2, 4};

            var b = new double[2] {1, 2};

            var xStart = new double[2] {2, 1};
            var A = new SparseMatrix(values, col_ind, row_ptr, 2);

            double[] res = SolveConjugateGradient(A, b, xStart, 1000, 1E-4);
            System.Diagnostics.Debug.WriteLine("Solution: x: {0}, y={1}", res[0], res[1]);
            res = SolvePrecondConjugateGradient(A, b, xStart, 1000, 1E-4);
            System.Diagnostics.Debug.WriteLine("SolutionPreconditioned: x: {0}, y={1}", res[0], res[1]);
        }

        /// <summary>
        /// </summary>
        public static void TestConjugateGradientMethod2() {
            var values = new double[28] {
                4, 1,
                1, 16, 1,
                1, 64, 1,
                1, 256, 1,
                1, 1024, 1,
                1, 4096, 1,
                1, 16384, 1,
                1, 65536, 1,
                1, 262144, 1,
                1, 1048576
            };
            var colInd = new int[28] {
                0, 1,
                0, 1, 2,
                1, 2, 3,
                2, 3, 4,
                3, 4, 5,
                4, 5, 6,
                5, 6, 7,
                6, 7, 8,
                7, 8, 9,
                8, 9
            };
            var rowPtr = new int[11] {0, 2, 5, 8, 11, 14, 17, 20, 23, 26, 28};

            var A = new SparseMatrix(values, colInd, rowPtr, rowPtr.Length - 1);

            var b = new double[10] {5, 18, 66, 258, 1026, 4098, 16386, 65538, 262146, 1048577};

            double[] result1 = SolvePrecondConjugateGradient(A, b, new double[10], 1000, 1E-6);
            string res = result1.Aggregate("", (s, t) => string.Format("{0},\t{1}", s, t));

            //Result should be 1 =(1,1,1,.....,1,1)
            System.Diagnostics.Debug.WriteLine(res);
        }
    }
}
