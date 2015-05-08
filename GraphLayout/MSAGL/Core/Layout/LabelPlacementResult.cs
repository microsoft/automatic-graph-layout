namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// The possible possible results of a label placement.
    /// OverlapsOtherLabels is the worst result, while OverlapsNothing is the best result.
    /// </summary>
    public enum LabelPlacementResult
    {
        /// <summary>
        /// Placement result meaning that another label was overlapped
        /// </summary>
        OverlapsOtherLabels = 0,
        /// <summary>
        /// Placement result meaning that the label overlaps a node, but not a label
        /// </summary>
        OverlapsNodes = 1,
        /// <summary>
        /// Placement result meaning that the label overlaps an edge, but not a node or label.
        /// </summary>
        OverlapsEdges = 2,
        /// <summary>
        /// Placement result meaning that the label overlaps nothing.
        /// </summary>
        OverlapsNothing = int.MaxValue,
    }
}