namespace Microsoft.Msagl.Drawing{
    /// <summary>
    /// The usage of this delegate is 
    /// a) when dragging is false
    /// to find out if a combination of mouse buttons and pressed 
    /// modifier keys signals that the current selected entity should be added 
    /// (removed) to (from) the dragging group
    /// b) if the dragging is true to find out if we are selecting objects with the rectangle 
    /// </summary>
    /// <param name="modifierKeys"></param>
    /// <param name="mouseButtons"></param>
    /// <param name="dragging"></param>
    /// <returns></returns>
    public delegate bool MouseAndKeysAnalyzer(ModifierKeys modifierKeys, MouseButtons mouseButtons, bool dragging);
}