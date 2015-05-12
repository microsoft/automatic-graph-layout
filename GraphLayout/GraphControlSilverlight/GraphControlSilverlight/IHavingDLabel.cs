using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// an interface for objects having a label
    /// </summary>
    public interface IHavingDLabel
    {
        /// <summary>
        /// gets or sets the label
        /// </summary>
        DLabel Label
        {
            get;
            set;
        }
    }
}