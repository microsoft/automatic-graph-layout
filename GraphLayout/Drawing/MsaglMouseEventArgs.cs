using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// an abstract class supporting mouse events
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msagl")]
    public abstract class MsaglMouseEventArgs : EventArgs {

        /// <summary>
        ///  Gets the current state of the left mouse button.
        /// </summary>
        public abstract bool LeftButtonIsPressed { get; }

        /// <summary>
        ///  Gets the current state of the middle mouse button.
        /// </summary>
        public abstract bool MiddleButtonIsPressed { get; }

        /// <summary>
        ///    Gets the current state of the right mouse button.
        /// </summary>
        public abstract bool RightButtonIsPressed { get; }
        /// <summary>
        /// gets or sets the handled flag
        /// </summary>
        public abstract bool Handled { get; set; }
        /// <summary>
        /// X position of the mouse cursor
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "X")]
        public abstract int X { get; }
        /// <summary>
        /// Y position of the mouse cursor
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Y")]
        public abstract int Y { get; }
        /// <summary>
        /// gets the number of clicks of the button
        /// </summary>
        public abstract int Clicks { get; }
    }
}
