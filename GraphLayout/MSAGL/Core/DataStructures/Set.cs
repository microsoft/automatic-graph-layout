using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Msagl.Core.DataStructures {
    /// <summary>
    /// Implementation of Set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Set")]
    public class Set<T> : MarshalByRefObject, ICollection<T> {
        HashSet<T> hashSet = new HashSet<T>();
        /// <summary>
        /// inserts an element into the set
        /// </summary>
        /// <param name="element"></param>
        public void Insert(T element) {
            hashSet.Add(element);
#if SHARPKIT
            UpdateHashKey();
#endif
        }
        void System.Collections.Generic.ICollection<T>.Add(T t) { Insert(t); }
        /// <summary>
        /// returns true when the set contains the element
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item) { return hashSet.Contains(item); }
        /// <summary>
        /// deletes the element from the set
        /// </summary>
        /// <param name="item"></param>
        public void Delete(T item) {
            hashSet.Remove(item);
#if SHARPKIT
            UpdateHashKey();
#endif
        }
        /// <summary>
        /// deletes the element from the set
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item) {
            var ret = hashSet.Remove(item);
#if SHARPKIT
            UpdateHashKey();
#endif
            return ret;
        }
        /// <summary>
        /// returns the number elements in the set
        /// </summary>
        public int Count { get { return hashSet.Count; } }
        /// <summary>
        /// returns the set entities enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() { return hashSet.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return hashSet.GetEnumerator();
        }



        /// <summary>
        /// the read only flag
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        //		public bool  IsSynchronized {get {return table.IsSynchronized; }}

        //public T SyncRoot {get {return table.SyncRoot;}}

        /// <summary>
        /// copies to an array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex) {
            hashSet.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            string r = "{";
            int i = 0;
            foreach (T o in this) {
                r += o.ToString();
                i++;
                if (i < Count)
                    r += ",";
            }
            r += "}";
            return r;
        }

        /// <summary>
        /// cleans the set
        /// </summary>
        public void Clear() {
            this.hashSet.Clear();
#if SHARPKIT
            UpdateHashKey();
#endif
        }
        /// <summary>
        /// clones the set
        /// </summary>
        /// <returns></returns>
        public Set<T> Clone() {
            Set<T> ret = new Set<T>();
            foreach (T i in this) {
                ret.Insert(i);
            }
            return ret;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="enumerableCollection"></param>
        public Set(IEnumerable<T> enumerableCollection) {
            ValidateArg.IsNotNull(enumerableCollection, "enumerableCollection");
            foreach (T j in enumerableCollection)
                this.Insert(j);
#if SHARPKIT
            UpdateHashKey();
#endif
        }

        /// <summary>
        /// an empty constructor
        /// </summary>
        public Set() {
        }

        /// <summary>
        /// creates an array from the set elements
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public System.Array ToArray(System.Type type) {
            System.Array ret = System.Array.CreateInstance(type, this.Count);
            int i = 0;
            foreach (T o in this) {
                ret.SetValue(o, i++);
            }
            return ret;
        }
        /// <summary>
        /// overloading plus operator
        /// </summary>
        /// <param name="set0"></param>
        /// <param name="set1"></param>
        /// <returns></returns>
        static public Set<T> operator +(Set<T> set0, Set<T> set1) {
            ValidateArg.IsNotNull(set1, "set1");
            Set<T> ret = new Set<T>(set0);
            foreach (T t in set1)
                ret.Insert(t);
            return ret;
        }

        /// <summary>
        /// overloading plus operator
        /// </summary>
        /// <param name="set0"></param>
        /// <param name="set1"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        static public Set<T> operator -(Set<T> set0, Set<T> set1) {
            ValidateArg.IsNotNull(set1, "set1");
            Set<T> ret = new Set<T>(set0);
            foreach (T t in set1)
                ret.Remove(t);
            return ret;
        }
        /// <summary>
        /// overloading the == operator
        /// </summary>
        /// <param name="set0"></param>
        /// <param name="set1"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        static public bool operator ==(Set<T> set0, Set<T> set1) {
            if ((object)set0 == null && (object)set1 == null)
                return true;
            if ((object)set0 == null && (object)set1 != null)
                return false;

            if ((object)set1 == null && (object)set0 != null)
                return false;

            foreach (T t in set0)
                if (!set1.Contains(t))
                    return false;

            foreach (T t in set1)
                if (!set0.Contains(t))
                    return false;

            return true;
        }

        /// <summary>
        /// overloading the == operator
        /// </summary>
        /// <param name="set0"></param>
        /// <param name="set1"></param>
        /// <returns></returns>
        static public bool operator !=(Set<T> set0, Set<T> set1) {
            return !(set0 == set1);
        }
        /// <summary>
        /// intersection
        /// </summary>
        /// <param name="set0"></param>
        /// <param name="set1"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        static public Set<T> operator *(Set<T> set0, Set<T> set1) {
            ValidateArg.IsNotNull(set0, "set0");
            ValidateArg.IsNotNull(set1, "set1");
            return
                new Set<T>(set0.Count < set1.Count
                               ? set0.Where(a => set1.Contains(a))
                               : set1.Where(a => set0.Contains(a)));
        }
        /// <summary>
        /// the equality
        /// </summary>
        /// <param name="obj">the object to compare to</param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            Set<T> set = obj as Set<T>;
            if (set == null)
                return false;
            return set == this;
        }

        /// <summary>
        /// hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            int ret = 0;
            foreach (T t in this)
                ret |= t.GetHashCode();
            return ret;
        }

        /// <summary>
        /// a friendly add, union, operator
        /// </summary>
        /// <param name="set0"></param>
        /// <param name="set1"></param>
        /// <returns></returns>
        public Set<T> Add(Set<T> set0, Set<T> set1) {
            return set0 + set1;
        }

        /// <summary>
        /// inserts a range of elements into the set
        /// </summary>
        /// <param name="elements">elements to insert</param>
        public void InsertRange(IEnumerable<T> elements )
        {
            ValidateArg.IsNotNull(elements,"elements");
            foreach (var element in elements)
                Insert(element);
        }

        ///<summary>
        /// checks the set is contained in the "otherSet"
        ///</summary>
        ///<param name="otherSet"></param>
        ///<returns></returns>
        public bool IsContained(Set<T> otherSet) { return this.All(p => otherSet.Contains(p)); }

        ///<summary>
        /// checks that this set conains the "otherSet"
        ///</summary>
        ///<param name="otherSet"></param>
        ///<returns></returns>
        public bool Contains(Set<T> otherSet) { return otherSet.All(p => Contains(p)); }

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289
        private SharpKit.JavaScript.JsString _hashKey;
        private void UpdateHashKey()
        {
            _hashKey = GetHashCode().ToString();
        }
#endif
    }
}
