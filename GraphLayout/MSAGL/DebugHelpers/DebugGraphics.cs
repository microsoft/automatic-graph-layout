#if DEBUG && !SHARPKIT
using System.Collections.Generic;

namespace Microsoft.Msagl.DebugHelpers {
    class DebugGraphics : IDebugGraphics {
        List<DebugShape> shapes = new List<DebugShape>();
        public IList<DebugShape> Shapes {
            get { return shapes; }
        }

        public void Clear() {
            shapes.Clear();
        }
    }
}
#endif