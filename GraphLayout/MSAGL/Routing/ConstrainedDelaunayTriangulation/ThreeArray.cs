using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation {
    ///<summary>
    /// an efficient class to simulate a three element array
    ///</summary>
    ///<typeparam name="T"></typeparam>
#if TEST_MSAGL
    [Serializable]
#endif

    public class ThreeArray<T>:IEnumerable<T> {
        T item0;
        T item1;
        T item2;

        internal bool Contains(T t) {
            return t.Equals(item0) || t.Equals(item1) || t.Equals(item2);
        }

        internal int Index(T t) {
            if(t.Equals(item0))
                return 0;
            if (t.Equals(item1))
                return 1;
            if (t.Equals(item2))
                return 2;
            return -1;
        }

        ///<summary>
        ///</summary>
        ///<param name="item0"></param>
        ///<param name="item1"></param>
        ///<param name="item2"></param>
        public ThreeArray(T item0, T item1, T item2) {
            this.item0 = item0;
            this.item1 = item1;
            this.item2 = item2;
        }

        ///<summary>
        ///</summary>
        public ThreeArray() {}

        ///<summary>
        ///</summary>
        ///<param name="i"></param>
        ///<exception cref="InvalidOperationException"></exception>
        public T this[int i] {
            get {
                switch (i) {
                    case 0:
                    case 3:
                    case -3: return item0;
                    case 1:
                    case 4:
                    case -2: return item1;
                    case 2:
                    case 5: 
                    case -1: return item2;
                    default: throw new InvalidOperationException();
                }
            }
            set {
                switch (i) {
                    case 0:
                    case 3:
                    case -3:item0 = value;break;
                    case 1:
                    case 4:
                    case -2:item1 = value;break;
                    case 2:
                    case 5:
                    case -1:item2 = value;break;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator() {
            yield return item0;
            yield return item1;
            yield return item2;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            yield return item0;
            yield return item1;
            yield return item2;
        }
    }
}