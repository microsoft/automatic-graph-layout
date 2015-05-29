using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient {
    /// <summary>
    /// Simple Vector class which allows to easily use operators.
    /// </summary>
    public class Vector {
        /// <summary>
        /// Value array
        /// </summary>
        public double[] array;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="array"></param>
        public Vector(double[] array) {
            this.array = array;
        }

        /// <summary>
        /// Substracts vector b directly from the current vector (without creating a new vector).
        /// </summary>
        /// <param name="b">vector to be substracted</param>
        /// <returns></returns>
        public void Sub(Vector b) {
            for (int i = 0; i < array.Length; i++) {
                array[i] -= b.array[i];
            }
        }

        /// <summary>
        /// Substracts vector b and returns the result in a new vector.
        /// </summary>
        /// <param name="a">vector</param>
        /// <param name="b">vector to be substracted</param>
        /// <returns></returns>
        public static Vector operator -(Vector a,Vector b) {
            double[] res = new double[a.array.Length];
            for (int i = 0; i < a.array.Length; i++) {
                res[i] = a.array[i] - b.array[i];
            }
            return new Vector(res);
        }


        /// <summary>
        /// Adds vector b without creating a new vector.
        /// </summary>
        /// <param name="b">vector</param>
        /// <returns></returns>
        public void Add(Vector b) {
            for (int i = 0; i < array.Length; i++) {
                array[i] += b.array[i];
            }   
        }

        /// <summary>
        /// Adds vector b to vector a and returns the result in a new vector.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">vector</param>
        /// <returns></returns>
        public static Vector operator +(Vector a,Vector b) {
            double[] res = new double[a.array.Length];
            for (int i = 0; i < a.array.Length; i++) {
                res[i] = a.array[i] + b.array[i];
            }
            return new Vector(res);
        }

        /// <summary>
        /// Creates a copy of the vector
        /// </summary>
        /// <returns></returns>
        public Vector Clone() {
           return new Vector((double[]) array.Clone());
        }

      /// <summary>
      /// Multiplies two vectors of same length.
      /// </summary>
      /// <param name="v1"></param>
      /// <param name="v2"></param>
      /// <returns>vector product</returns>
        public static double operator *(Vector v1, Vector v2) {
          double res = 0;
            for (int i = 0; i < v1.array.Length; i++) {
                res  += v1.array[i] * v2.array[i];
            }
            return res;
        }

       /// <summary>
       /// Multiplies two vectors component wise and return the result in a new vector.
       /// </summary>
       /// <param name="v"></param>
       /// <returns></returns>
        public Vector CompProduct(Vector v) {
            double[] res = new double[array.Length];
            for (int i = 0; i < array.Length; i++) {
                res[i] = array[i] * v.array[i];
            }
            return new Vector(res);
        }

        /// <summary>
        /// Multiplies the vector by a scalar and return the result in a new vector.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector operator *(double scalar, Vector v) {
            double[] res = new double[v.array.Length];
            for (int i = 0; i < v.array.Length; i++) {
                res[i] = v.array[i]*scalar;
            }
            return new Vector(res);  
        }
    }
}
