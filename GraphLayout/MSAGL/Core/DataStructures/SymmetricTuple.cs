using System;

namespace Microsoft.Msagl.Core.DataStructures {
    /// <summary>
    /// a tuple such that (a,b)==(b,a) for any a and b
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SymmetricTuple<T> {
        bool Equals(SymmetricTuple<T> other) {
            return (A.Equals(other.A) && B.Equals(other.B)) ||
                   (A.Equals(other.B) && B.Equals(other.A));
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode() {

#if !SHARPKIT
            unchecked {
#endif
                return A.GetHashCode() ^ B.GetHashCode(); // we need a symmetric hash code
#if !SHARPKIT
            }
#endif
        }

        public T A { get; private set; }
        public T B { get; private set; }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public SymmetricTuple(T a, T b) {
            A = a;
            B = b;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SymmetricTuple<T>)obj);
        }

        public override string ToString() {
            return String.Format("({0},{1})", A, B);
        }
    }

}