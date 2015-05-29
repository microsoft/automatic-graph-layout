using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// the basis class for layering algorithms
    /// </summary>
    public interface LayerCalculator
    {
		/// <summary>
		/// the main method
		/// </summary>
		int[] GetLayers();
    }
}
