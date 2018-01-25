using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Msagl.Core.Geometry
{
    /// <summary>
    /// A rectangle and associated data that need to be packed
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public class RectangleToPack<TData>
    {
        /// <summary>
        /// Rectangle to be packed - packing will translate the rectangle
        /// </summary>
        public Rectangle Rectangle { get; internal set; }

        /// <summary>
        /// data associated with rectangle
        /// </summary>
        public TData Data { get; private set; }

        /// <summary>
        /// Associate a rectangle with a data item to be packed
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="data"></param>
        public RectangleToPack(Rectangle rectangle, TData data)
        {
            this.Rectangle = rectangle;
            this.Data = data;
        }
    }
}
