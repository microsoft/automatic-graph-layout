using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Drawing
{
    /// <summary>
    /// FontStyle enum
    /// </summary>
    [Flags]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
    public enum FontStyle
    {
        /// <summary>
        ///    Normal text.
        /// </summary>
        Regular = 0,
        /// <summary>
        ///    Bold text.
        /// </summary>
        Bold = 1,
        /// <summary>
        ///    Italic text.
        /// </summary>
        Italic = 2,
        /// <summary>
        ///    Underlined text.
        /// </summary>
        Underline = 4,
        /// <summary>
        ///    Text with a line through the middle.
        /// </summary>
        Strikeout = 8,
    }
}
