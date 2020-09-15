using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Visibility {
    internal abstract class SegmentBase {
        abstract internal Point Start { get; }
        abstract internal Point End { get; }
        internal Point Direction { get { return End - Start; } }
#if TEST_MSAGL
        public override string ToString() {
            return Start + " " + End;
        }
#endif
    }
}