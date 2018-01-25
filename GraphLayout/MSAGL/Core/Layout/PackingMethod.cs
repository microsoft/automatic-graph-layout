using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Layout
{
    /// <summary>
    /// the packing method to be used by InitialLayoutByCluster
    /// </summary>
    public enum PackingMethod
    {
        /// <summary>
        /// biggest to smallest with nested wrapping
        /// </summary>
        Compact,

        /// <summary>
        /// Pack to desired ratio in columns
        /// </summary>
        Columns
    }
}
