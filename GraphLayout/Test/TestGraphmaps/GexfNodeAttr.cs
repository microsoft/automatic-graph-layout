using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry;

namespace TestGraphmaps {
    internal class GexfNodeAttr {
        internal Point Position;
        internal double Size;
        internal Dictionary<String, String> Attvalues = new Dictionary<string, string>();
    }
}