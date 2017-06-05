using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Msagl.Core.DataStructures {
    /// <summary>
    /// various utilities for collections
    /// </summary>
    public class CollectionUtilities {
        /// <summary>
        /// Add	value to dictionary
        /// </summary>
        internal static void AddToMap<TS, T, TC>(Dictionary<T, TC> dictionary, T key, TS value)
            where TC : ICollection<TS>, new() {
            TC tc;
            if (!dictionary.TryGetValue(key, out tc))
                dictionary[key] = tc = new TC();

            tc.Add(value);
        }

        /// <summary>
        /// Remove value from dictionary
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T"),
         SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "S"),
         SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        internal static void RemoveFromMap<TS, T, TC>(Dictionary<T, TC> dictionary, T key, TS value)
            where TC : ICollection<TS> {
            var tc = dictionary[key];
            tc.Remove(value);
            if (tc.Count == 0)
                dictionary.Remove(key);
        }
    }
}
