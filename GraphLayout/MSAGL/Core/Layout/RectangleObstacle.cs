using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Core.Layout
{
    internal class RectangleObstacle : IObstacle
    {
        internal RectangleObstacle(Rectangle r)
        {
            this.Rectangle = r;
        }

        internal RectangleObstacle(Rectangle r, object data)
        {
            this.Rectangle = r;
            this.Data = data;
        }

        public Rectangle Rectangle
        {
            get;
            private set;
        }

        public object Data
        {
            get;
            private set;
        }
    }
}