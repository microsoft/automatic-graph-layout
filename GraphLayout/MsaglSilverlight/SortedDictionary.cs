using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;

internal class SortedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IComparer<TKey> {
    RbTree<KeyValuePair<TKey, TValue>> tree = new RbTree<KeyValuePair<TKey, TValue>>(new KeyValuePairComparer<TKey, TValue>(null));
    /// <summary>
    /// the number of elements in the tree
    /// </summary>
    public int Count {
        get {
            return tree.Count();
        }
    }
    /// <summary>
    /// adds a pair of key-value to the dictionary
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(TKey key, TValue value) {
        if (key == null) {
            throw new ArgumentNullException();
        }
        tree.Insert(new KeyValuePair<TKey, TValue>(key, value));
    }


    public SortedDictionary() { }

    public SortedDictionary(IComparer<TKey> comparer) {
        tree = new RbTree<KeyValuePair<TKey, TValue>>(new KeyValuePairComparer<TKey, TValue>(comparer));
        Comparer = comparer;
    }

    public IEnumerable<TKey> Keys {
        get { return from v in tree select v.Key; }
    }

    public bool TryGetValue(TKey key, out TValue value) {
        value = default(TValue);
        var ret = tree.Find(new KeyValuePair<TKey, TValue>(key, value));
        if (ret != null) {
            value = ret.Item.Value;
            return true;
        }

        return false;
    }

    public TValue this[TKey key] {
        get {
            TValue v;
            if (TryGetValue(key, out v))
                return v;

            throw new IndexOutOfRangeException();
        }
        set {
            var ret = tree.Find(new KeyValuePair<TKey, TValue>(key, default(TValue)));
            if (ret != null)
                ret.Item = new KeyValuePair<TKey, TValue>(key, value);
            else
                tree.Insert(new KeyValuePair<TKey, TValue>(key, value));
        }

    }

    public void Remove(TKey key) {
        tree.Remove(new KeyValuePair<TKey, TValue>(key, default(TValue)));
    }

    public KeyValuePair<TKey, TValue> First() {
        return tree.TreeMinimum().Item;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        return tree.GetEnumerator();
    }

    internal bool ContainsKey(TKey key) {
        return tree.Contains(new KeyValuePair<TKey, TValue>(key, default(TValue)));
    }

    public IEnumerable<TValue> Values {
        get { return from v in tree select v.Value; }
    }

    IComparer<TKey> Comparer { get; set; }

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator() {
        return tree.GetEnumerator();
    }

    #endregion

    #region IEnumerable<KeyValuePair<TKey,TValue>> Members

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
        return tree.GetEnumerator();
    }

    #endregion

    /// <summary>
    /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <returns>
    /// Value Condition Less than zero<paramref name="x"/> is less than <paramref name="y"/>.Zero<paramref name="x"/> equals <paramref name="y"/>.Greater than zero<paramref name="x"/> is greater than <paramref name="y"/>.
    /// </returns>
    /// <param name="x">The first object to compare.</param><param name="y">The second object to compare.</param>
    public int Compare(TKey x, TKey y) {
        return Comparer.Compare(x, y);
    }
}

internal class KeyValuePairComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> {

    private IComparer<TKey> Comparer;
    public KeyValuePairComparer(IComparer<TKey> comparer) {
        Comparer = comparer;
    }

    public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) {
        if (Comparer != null)
            return Comparer.Compare(x.Key, y.Key);
        return ((IComparable<TKey>)x.Key).CompareTo(y.Key);
    }
}