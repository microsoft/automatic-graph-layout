namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// three type of nodes
    /// </summary>
    public enum LgNodeInfoKind {
        /// <summary>
        /// out of view
        /// </summary>
        OutOfView,

        /// <summary>
        /// fully visible
        /// </summary>
   
        FullyVisible, 
        /// <summary>
        /// becomes fully visible soon
        /// </summary>
        Satellite,
        
    }
}