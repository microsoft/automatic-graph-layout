using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Routing.Spline.ConeSpanner {
    internal interface IConeSweeper {
        Point ConeRightSideDirection { get; set; }
        Point ConeLeftSideDirection { get; set; }
        Point SweepDirection { get; set; }
        double Z { get; set; }
    }
}