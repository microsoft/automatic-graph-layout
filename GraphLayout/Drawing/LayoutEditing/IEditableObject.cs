namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// an interface for an editable object 
    /// </summary>
    public interface IEditableObject {
        /// <summary>
        /// gets or sets the corresponding DrawingObject
        /// </summary>
        DrawingObject DrawingObject { get;}

        /// <summary>
        /// is set to true when the object is selected for editing
        /// </summary>
        bool SelectedForEditing { get;set;}      
    }
}
