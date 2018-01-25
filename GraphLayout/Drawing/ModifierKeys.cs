using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
 
    /// <summary>
    ///    Specifies the set of modifier keys.
    /// </summary>
    [Flags]
    public enum ModifierKeys {
      /// <summary>
      /// No modifiers are pressed.
      /// </summary>
        None = 0,
        /// <summary>
        /// THE alt key
        /// </summary>
        Alt = 1,
       /// <summary>
       /// the control key
       /// </summary>
        Control = 2,
        /// <summary>
        /// the shift key
        /// </summary>
        Shift = 4,
     /// <summary>
     /// the window logo key
     /// </summary>
        Windows = 8,
    }
}

