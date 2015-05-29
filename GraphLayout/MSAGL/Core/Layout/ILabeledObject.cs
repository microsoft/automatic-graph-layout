namespace Microsoft.Msagl.Core.Layout {

    /// <summary>
    /// interface for geometry objects with labels
    /// </summary>
    public interface ILabeledObject {
        /// <summary>
        /// the label of the object 
        /// </summary>
        Label Label { get; set; }
    }
}


