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
﻿using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Msagl.Routing.Rectilinear {
    /// <summary>
    /// Used in merge-type operations to track whether MoveNext has returned false.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class EnumeratorWrapper<T> {
        /// <summary>
        /// False iff MoveNext has not been called, or has returned false.
        /// </summary>
        internal bool HasCurrent { get { return this.state == MoveNextState.True; } }

        /// <summary>
        /// State of MoveNext.  MoveNext is not automatically called on ctor for consistency with unwrapped enumerators.
        /// </summary>
        private enum MoveNextState {
            NotCalled,
            True,
            False
        }

        private MoveNextState state = MoveNextState.NotCalled;

        private readonly IEnumerator<T> enumerator;

        internal EnumeratorWrapper(IEnumerable<T> enumerable) {
            this.enumerator = enumerable.GetEnumerator();
        }

        internal T Current
        {
            get {
                Debug.Assert(this.HasCurrent, "MoveNext has not been called or has returned false");
                return this.enumerator.Current;
            }
        }

        internal bool MoveNext() {
            Debug.Assert(this.state != MoveNextState.False, "MoveNext has returned false");
            if (this.enumerator.MoveNext()) {
                this.state = MoveNextState.True;
                return true;
            } 
            this.state = MoveNextState.False;
            return false;
        }
    }
}