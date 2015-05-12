using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing;

namespace Microsoft.Msagl.Prototype.LayoutEditing {
    /// <summary>
    /// keeps a label restore data
    /// </summary>
    public class LabelRestoreData:RestoreData  {
        private Point center;

        /// <summary>
        /// the label center
        /// </summary>
        public Point Center {
            get { return center; }
            set { center = value; }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="centerP"></param>
        public LabelRestoreData(Point centerP) {
            this.center = centerP;
        }
    }
}
