/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
        /// Add	key,value to dictionary if the key is not yet presented
        /// </summary>
        internal static void SafeAdd<T, TC>(Dictionary<T, TC> dictionary, T key, TC value) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }

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
