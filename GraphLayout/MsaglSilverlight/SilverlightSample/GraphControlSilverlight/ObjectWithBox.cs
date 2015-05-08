using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// a base class for object that are drawn
    /// </summary>
    abstract public class ObjectWithBox : UserControl
    {
        abstract internal Rectangle Box { get; }
    }
}