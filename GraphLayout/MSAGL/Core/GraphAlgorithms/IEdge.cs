namespace Microsoft.Msagl.Core.GraphAlgorithms {
    /// <summary>
    /// an edge interface
    /// </summary>
    public interface IEdge {
        /// <summary>
        /// source
        /// </summary>
        int Source { get; set; }
        /// <summary>
        /// target
        /// </summary>
        int Target { get; set; }
    }
}
