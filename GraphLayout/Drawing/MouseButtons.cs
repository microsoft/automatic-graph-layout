using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Mouse button enum
    /// </summary>
    [Flags]
    public enum MouseButtons {
       /// <summary>
       /// No button was pressed
       /// </summary>
        None = 0,
        /// <summary>
        /// The left mouse button was pressed.
        /// </summary>
        Left = 1048576,
        /// <summary>
        /// The right mouse button was pressed.
        /// </summary>
        Right = 2097152,
        
        /// <summary>
        ///The middle mouse button was pressed. 
        /// </summary>
        Middle = 4194304,
        
        /// <summary>
        ///  The first XButton was pressed.
        /// </summary>
        XButton1 = 8388608,
        /// <summary>
        ///    The second XButton was pressed.
        /// </summary>
        XButton2 = 16777216,

    }
}
