namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree {
    /// <summary>
    /// Enum containing the different overlap removal methods.
    /// </summary>
    public enum OverlapRemovalMethod {
        /// <summary>
        /// Proximity Stress Model
        /// </summary>
        Prism,
        /// <summary>
        /// Proximity Minimum Spanning Tree
        /// </summary>
        MinimalSpanningTree,        
    }
}