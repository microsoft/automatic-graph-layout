namespace Microsoft.Msagl.Core.Geometry.Curves {

    /// <summary>
    /// Point positions relative to a closed curve enumeration
    /// </summary>
    public enum PointLocation {
        /// <summary>
        /// The point is outside of the curve
        /// </summary>
        Outside = 0,
        /// <summary>
        /// The point is on the curve boundary
        /// </summary>
        Boundary = 1,
        /// <summary>
        /// The point is inside of the curve
        /// </summary>
        Inside = 2
    }
}
