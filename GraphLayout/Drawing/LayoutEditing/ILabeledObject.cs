using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// an interface for objects having a label
    /// </summary>
    public interface ILabeledObject {
        /// <summary>
        /// gets or sets the label
        /// </summary>
         Label Label {
            get;
            set;
        }
    }
}
