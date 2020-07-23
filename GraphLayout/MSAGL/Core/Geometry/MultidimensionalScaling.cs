using System;

namespace Microsoft.Msagl.Core.Geometry {
    /// <summary>
    /// Classical Multidimensional Scaling. Given a set of proximities or
    /// dissimilarities ordistances between objects, multidimensional scaling
    /// recovers low-dimensional coordinates for these objects with these
    /// distances.
    /// </summary>
    sealed public class MultidimensionalScaling {
        MultidimensionalScaling() { }//suppressing the creation of the public constructor
        /// <summary>
        /// Double-centers a matrix in such a way that the center of gravity is zero.
        /// After double-centering, each row and each column sums up to zero.
        /// </summary>
        /// <param name="matrix"></param>
        public static void DoubleCenter(double[][] matrix) {
            ValidateArg.IsNotNull(matrix, "matrix");
            double[] rowMean=new double[matrix.Length];
            double[] colMean = new double[matrix[0].Length];
            double mean = 0;
            for (int i = 0; i < matrix.Length; i++) {
                for (int j = 0; j < matrix[0].Length; j++) {
                    rowMean[i] += matrix[i][j];
                    colMean[j] += matrix[i][j];
                    mean += matrix[i][j];
                }
            }
            for (int i = 0; i < matrix.Length; i++) rowMean[i] /= matrix.Length;
            for (int j = 0; j < matrix[0].Length; j++) colMean[j] /= matrix[0].Length;
            mean /= matrix.Length;
            mean /= matrix[0].Length;
            for (int i = 0; i < matrix.Length; i++) {
                for (int j = 0; j < matrix[0].Length; j++) {
                    matrix[i][j] -= rowMean[i] + colMean[j] - mean;
                }
            }
        }

        /// <summary>
        /// Squares all entries of a matrix.
        /// </summary>
        /// <param name="matrix">A matrix.</param>
        public static void SquareEntries(double[][] matrix) {
            ValidateArg.IsNotNull(matrix, "matrix");
            for (int i = 0; i < matrix.Length; i++) {
                for (int j = 0; j < matrix[0].Length; j++) {
                    matrix[i][j] = Math.Pow(matrix[i][j], 2);
                }
            }
        }

        /// <summary>
        /// Multiplies a matrix with a scalar factor.
        /// </summary>
        /// <param name="matrix">A matrix.</param>
        /// <param name="factor">A scalar factor.</param>
        public static void Multiply(double[][] matrix, double factor) {
            ValidateArg.IsNotNull(matrix, "matrix");
            for (int i = 0; i < matrix.Length; i++) {
                for (int j = 0; j < matrix[0].Length; j++) {
                    matrix[i][j] *= factor;
                }
            }
        }

        /// <summary>
        /// Multiply a square matrix and a vector. 
        /// Note that matrix width and vector length
        /// have to be equal, otherwise null is returned.
        /// </summary>
        /// <param name="A">A matrix.</param>
        /// <param name="x">A vector.</param>
        /// <returns>The resulting product vector, or null if matrix and vector
        /// are incompatible.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "A"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "A")]
        public static double[] Multiply(double[][] A, double[] x) {
            ValidateArg.IsNotNull(A, "A");
            ValidateArg.IsNotNull(x, "x");
            if(A[0].Length!=x.Length) return null;
            double[] y=new double[x.Length];
            for (int i = 0; i < A.Length; i++) {
                for (int j = 0; j < A[0].Length; j++) {
                    y[i]+=A[i][j]*x[j];
                }
            }
            return y;
        }

        /// <summary>
        /// Gives the norm of a vector, that is, its length in
        /// vector.length dimensional Euclidean space.
        /// </summary>
        /// <param name="x">A vector.</param>
        /// <returns>The norm of the vector.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public static double Norm(double[] x) {
            ValidateArg.IsNotNull(x, "x");
            double norm=0;
            for (int i = 0; i < x.Length; i++) {
                norm += Math.Pow(x[i], 2);
            }
            norm = Math.Sqrt(norm);
            return norm;
        }

        /// <summary>
        /// Normalizes a vector to unit length (1.0) in
        /// vector.length dimensional Euclidean space.
        /// If the vector is the 0-vector, nothing is done.
        /// </summary>
        /// <param name="x">A vector.</param>
        /// <returns>The norm of the vector.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public static double Normalize(double[] x) {
            ValidateArg.IsNotNull(x, "x");
            double lambda = Norm(x);
            if (lambda <= 0) return 0;
            for (int i = 0; i < x.Length; i++) {
                x[i] /= lambda;
            }
            return lambda;
        }

        /// <summary>`
        /// Gives a random unit Euclidean length vector of a given size.
        /// </summary>
        /// <param name="n">The size ofthe vector.</param>
        /// <param name="seed">A seed for the random number generator.</param>
        /// <returns>A random vector.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "n")]
        public static double[] RandomUnitLengthVector(int n, int seed) {
            double[] result=new double[n];
            Random random=new Random(seed);
            for (int i = 0; i < n; i++) {
                result[i] = random.NextDouble();
            }
            Normalize(result);
            return result;
        }

        /// <summary>
        /// Computes the two dominant eigenvectors and eigenvalues of a symmetric
        /// square matrix.
        /// </summary>
        /// <param name="A">A matrix.</param>
        /// <param name="u1">First eigenvector.</param>
        /// <param name="lambda1">First eigenvalue.</param>
        /// <param name="u2">Second eigenvector.</param>
        /// <param name="lambda2">Second eigenvalue.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "u"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "A"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "A"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "4#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static void SpectralDecomposition(double[][] A,
            out double[] u1, out double lambda1,
            out double[] u2, out double lambda2) {
            SpectralDecomposition(A, out u1, out lambda1, out u2, out lambda2, 30, 1e-6);
        }

        /// <summary>
        /// Computes the two dominant eigenvectors and eigenvalues of a symmetric
        /// square matrix.
        /// </summary>
        /// <param name="A">A matrix.</param>
        /// <param name="u1">First eigenvector.</param>
        /// <param name="lambda1">First eigenvalue.</param>
        /// <param name="u2">Second eigenvector.</param>
        /// <param name="lambda2">Second eigenvalue.</param>
        /// <param name="maxIterations"></param>
        /// <param name="epsilon"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "u"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "A"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "A"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "4#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static void SpectralDecomposition(double[][] A,
            out double[] u1, out double lambda1,
            out double[] u2, out double lambda2, int maxIterations, double epsilon) {
                ValidateArg.IsNotNull(A, "A");
            int n = A[0].Length;
            u1 = RandomUnitLengthVector(n, 0); lambda1 = 0;
            u2 = RandomUnitLengthVector(n, 1); lambda2 = 0;
            double r = 0;
            double limit = 1.0 - epsilon;
            // iterate until convergence but at most 30 steps
            for (int i = 0; (i < maxIterations && r < limit); i++) {
                double[] x1 = Multiply(A, u1);
                double[] x2 = Multiply(A, u2);

                lambda1 = Normalize(x1);
                lambda2 = Normalize(x2);
                MakeOrthogonal(x2, x1);
                Normalize(x2);

                // convergence is assumed if the inner product of
                // two consecutive (unit length) iterates is close to 1
                r = Math.Min(DotProduct(u1, x1), DotProduct(u2, x2));
                u1 = x1;
                u2 = x2;
            }
        }
        /// <summary>
        /// Gives the inner product of two vectors of the same size.
        /// </summary>
        /// <param name="x">A vector.</param>
        /// <param name="y">A vector.</param>
        /// <returns>The inner product of the two vectors.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public static double DotProduct(double[] x, double[] y) {
            ValidateArg.IsNotNull(x, "x");
            ValidateArg.IsNotNull(y, "y");
            if (x.Length != y.Length) return 0;
            double result = 0;
            for (int i = 0; i < x.Length; i++) {
                result += x[i]*y[i];
            }
            return result;
        }

        /// <summary>
        /// Orthogonalizes a vector against another vector, so that
        /// their scalar product is 0.
        /// </summary>
        /// <param name="x">Vector to be orthogonalized.</param>
        /// <param name="y">Vector to orthogonalize against.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public static void MakeOrthogonal(double[] x, double[] y) {
            ValidateArg.IsNotNull(x, "x");
            ValidateArg.IsNotNull(y, "y");
            if (x.Length != y.Length) return;
            double prod = DotProduct(x, y) / DotProduct(y, y);            
            for (int i = 0; i < x.Length; i++) {
                x[i] -= prod*y[i];
            }
        }

        /// <summary>
        /// Classical multidimensional scaling.  Computes two-dimensional coordinates
        /// for a given distance matrix by computing the two largest eigenvectors
        /// and eigenvalues of a matrix assiciated with the distance matrix (called
        /// "fitting inner products").
        /// </summary>
        /// <param name="d">The distance matrix.</param>
        /// <param name="x">The first eigenvector (scaled by the root of its eigenvalue)</param>
        /// <param name="y">The second eigenvector (scaled by the root of its eigenvalue)</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public static void ClassicalScaling(double[][] d, out double[] x, out double[] y) {
            ValidateArg.IsNotNull(d, "d");
            double[][] b = new double[d.Length][];
            for (int i = 0; i < d.Length; i++) {
                b[i] = new double[d[0].Length];
                d[i].CopyTo(b[i], 0);
            }
            SquareEntries(b);
            DoubleCenter(b);
            Multiply(b, -.5);
            double lambda1;
            double lambda2;
            SpectralDecomposition(b, out x, out lambda1, out y, out lambda2);
            lambda1=Math.Sqrt(Math.Abs(lambda1));
            lambda2=Math.Sqrt(Math.Abs(lambda2));
            for (int i = 0; i < x.Length; i++) {
                x[i] *= lambda1;
                y[i] *= lambda2;
            }
        }

        /// <summary>
        /// Multidimensional scaling.  Computes two-dimensional coordinates
        /// for a given distance matrix by fitting the coordinates to these distances
        /// iteratively by majorization (called "distance fitting").
        /// Only objects that have rows in the distance/weight matrix
        /// is subject to iterative relocation.
        /// </summary>
        /// <param name="d">A distance matrix.</param>
        /// <param name="x">Coordinate vector.</param>
        /// <param name="y">Coordinate vector.</param>
        /// <param name="w">Weight matrix.</param>
        /// <param name="numberOfIterations">Number of iteration steps.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "w"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d")]
        public static void DistanceScalingSubset(double[][] d, double[] x, double[] y, double[][] w, int numberOfIterations) {
            ValidateArg.IsNotNull(d, "d");
            ValidateArg.IsNotNull(x, "x");
            ValidateArg.IsNotNull(y, "y");
            ValidateArg.IsNotNull(w, "w");
            int n = x.Length;
            int k = d.Length;
            int[] index = new int[k];
            for (int i = 0; i < k; i++) {
                for (int j = 0; j < n; j++) {
                    if (d[i][j] == 0) {
                        index[i] = j;
                    }
                }
            }

            double[] wSum = new double[k];
            for (int i = 0; i < k; i++) {
                for (int j = 0; j < n; j++) {
                    if(index[i]!=j) {
                        wSum[i] += w[i][j];
                    }
                }
            }
            for (int c = 0; c < numberOfIterations; c++) {
                for (int i = 0; i < k; i++) {
                    double xNew = 0;
                    double yNew = 0;
                    for (int j = 0; j < n; j++) {
                        if (i != j) {
                            double inv = Math.Sqrt(Math.Pow(x[index[i]] - x[j], 2) + Math.Pow(y[index[i]] - y[j], 2));
                            if (inv > 0) inv = 1 / inv;
                            xNew += w[i][j] * (x[j] + d[i][j] * (x[index[i]] - x[j]) * inv);
                            yNew += w[i][j] * (y[j] + d[i][j] * (y[index[i]] - y[j]) * inv);
                        }
                    }
                    x[index[i]] = xNew / wSum[i];
                    y[index[i]] = yNew / wSum[i];
                }
            }
        }

        /// <summary>
        /// Multidimensional scaling.  Computes two-dimensional coordinates
        /// for a given distance matrix by fitting the coordinates to these distances
        /// iteratively by majorization (called "distance fitting").
        /// (McGee, Kamada-Kawai)
        /// </summary>
        /// <param name="d">A distance matrix.</param>
        /// <param name="x">Coordinate vector.</param>
        /// <param name="y">Coordinate vector.</param>
        /// <param name="w">Weight matrix.</param>
        /// <param name="iter">Number of iteration steps.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Diagnostics.Debug.Write(System.String)"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "w"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "iter")]
        public static void DistanceScaling(double[][] d, double[] x, double[] y, double[][] w, int iter) {
            ValidateArg.IsNotNull(d, "d");
            ValidateArg.IsNotNull(x, "x");
            ValidateArg.IsNotNull(y, "y");
            ValidateArg.IsNotNull(w, "w");
            int n = x.Length;
            double[] wSum = new double[n];
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    if (i != j)
                        wSum[i] += w[i][j];
                }
            }
            for (int c = 0; c < iter; c++) {
                for (int i = 0; i < n; i++) {
                    double xNew = 0;
                    double yNew = 0;
                    for (int j = 0; j < n; j++) {
                        if(i!=j) {
                            double inv = Math.Sqrt(Math.Pow(x[i] - x[j], 2) + Math.Pow(y[i] - y[j], 2));
                            if (inv > 0) inv=1/inv;
                            xNew += w[i][j] * (x[j] + d[i][j] * (x[i] - x[j]) * inv);
                            yNew += w[i][j] * (y[j] + d[i][j] * (y[i] - y[j]) * inv);
                        }
                    }
                    x[i] = xNew/wSum[i];
                    y[i] = yNew/wSum[i];
                }
            }
        }

        /// <summary>
        /// Convenience method for generating a weight matrix from a distance matrix.
        /// Each output entry is the corresponding input entry powered by a constant
        /// exponent.
        /// </summary>
        /// <param name="d">A distance matrix.</param>
        /// <param name="exponent">The exponent.</param>
        /// <returns>A weight matrix.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d")]
        public static double[][] ExponentialWeightMatrix(double[][] d, double exponent) {
            ValidateArg.IsNotNull(d, "d");
            double[][] w=new double[d.Length][];
            for (int i = 0; i < d.Length; i++) {
                w[i] = new double[d[i].Length];
                for (int j = 0; j < d[i].Length; j++) {
                    if(d[i][j]>0)
                        w[i][j] = Math.Pow(d[i][j], exponent);
                }
            }
            return w;
        }


        /// <summary>
        /// Convenience method for all Euclidean distances within two-dimensional
        /// positions.
        /// </summary>
        /// <param name="x">Coordinates.</param>
        /// <param name="y">Coordinates.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x")]
        public static double[][] EuclideanDistanceMatrix(double[] x, double[] y) {
            ValidateArg.IsNotNull(x, "x");
            ValidateArg.IsNotNull(y, "y");
            double[][] d = new double[x.Length][];
            for (int i = 0; i < x.Length; i++) {
                d[i] = new double[x.Length];
                for (int j = 0; j < x.Length; j++) {
                    d[i][j] = Math.Sqrt(Math.Pow(x[i] - x[j], 2) + Math.Pow(y[i] - y[j], 2));
                }
            }
            return d;
        }





        /// <summary>
        /// Approximation to classical multidimensional scaling.
        /// Computes two-dimensional coordinates
        /// for a given rectangular distance matrix.
        /// </summary>
        /// <param name="d">The distance matrix.</param>
        /// <param name="x">The first eigenvector (scaled by the root of its eigenvalue)</param>
        /// <param name="y">The second eigenvector (scaled by the root of its eigenvalue)</param>
        /// <param name="pivotArray">index of pivots</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "d"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        internal static void LandmarkClassicalScaling(double[][] d, out double[] x, out double[] y, int[] pivotArray ) {
         
            double[][] c=new double[d.Length][];
            for (int i = 0; i < d.Length; i++) {
                c[i]=new double[d.Length];
                for (int j = 0; j < d.Length; j++) {
                    c[i][j]=d[i][pivotArray[j]];
                }
            }
            SquareEntries(c);
            double[] mean=new double[d.Length];
            for (int i = 0; i < d.Length; i++) {
                for (int j = 0; j < d.Length; j++) {
                    mean[i] += c[i][j];
                }
                mean[i] /= d.Length;
            }
            DoubleCenter(c);
            Multiply(c, -.5);
            double[] u1, u2;
            double lambda1, lambda2;
            SpectralDecomposition(c, out u1, out lambda1, out u2, out lambda2);
            lambda1 = Math.Sqrt(Math.Abs(lambda1));
            lambda2 = Math.Sqrt(Math.Abs(lambda2));

            // place non-pivots by weighted barycenter
            x=new double[d[0].Length];
            y=new double[d[0].Length];
            for (int i = 0; i < x.Length; i++) {
                for (int j = 0; j < c.Length; j++) {
                    x[i] -= u1[j] * (Math.Pow(d[j][i], 2) - mean[j]) / 2;
                    y[i] -= u2[j] * (Math.Pow(d[j][i], 2) - mean[j]) / 2;
                }
            }
        }
    }
}
