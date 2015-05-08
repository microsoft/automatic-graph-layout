using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public class NodeTypeEntry
    {
        public string Name { get; set; }
        public Drawing.Shape Shape { get; set; }
        public double XRadius { get; set; }
        public double YRadius { get; set; }
        public object Tag { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
