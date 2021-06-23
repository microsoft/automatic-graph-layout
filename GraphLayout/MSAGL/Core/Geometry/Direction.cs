using System;

namespace Microsoft.Msagl.Core.Geometry {
 /// <summary>
 /// enumerates the compass directions
 /// </summary>
    [Flags]
    public enum Direction {
        /// <summary>
        /// no direction defined
        /// </summary>
        None=0,
        /// <summary>
        /// North
        /// </summary>
        North=1,
        /// <summary>
        /// East
        /// </summary>
        East=2,
        /// <summary>
        /// South
        /// </summary>
        South=4,
        /// <summary>
        /// West
        /// </summary>
        West=8
    }
}