namespace Microsoft.Msagl.DebugHelpers.Persistence {
    internal class CharStreamElement : CurveStreamElement {
        internal CharStreamElement(char ch) {
            Value = ch;
        }

        internal char Char { get { return (char)Value; } }
    }
}