using System.Collections.Generic;
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

    internal T Current {
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