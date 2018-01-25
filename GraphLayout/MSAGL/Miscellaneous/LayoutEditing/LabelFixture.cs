namespace Microsoft.Msagl.Miscellaneous.LayoutEditing {
    /// <summary>
    /// this class charachterizes how a label is attached to its edge
    /// </summary>
    internal class LabelFixture {
        //to put a label go to RelativeLengthOnCurve position, take normal accordingly to the RightSide and follow NormalLength this direction

        internal double RelativeLengthOnCurve;
        internal bool RightSide;
        internal double NormalLength;
    }
}