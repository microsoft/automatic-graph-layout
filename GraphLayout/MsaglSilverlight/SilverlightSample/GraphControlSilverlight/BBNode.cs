using System;
using Microsoft.Msagl.Drawing;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// a delegate type for filtering in the :  returns false on filtered entities and only on them
    /// </summary>
    /// <param name="dObject"></param>
    /// <returns></returns>
    internal delegate bool EntityFilterDelegate(DObject dObject);

    /// <summary>
    /// Summary description for BBNode.
    /// </summary>
    internal class BBNode
    {
        internal BBNode left;
        internal BBNode right;
        internal BBNode parent;
        internal MsaglRectangle bBox;
        internal Geometry geometry;
        internal MsaglRectangle Box
        {
            get
            {
                if (geometry != null)
                    return geometry.bBox;

                return bBox;
            }
        }



        internal BBNode() { }

        //when we check for inclusion we expand the box by slack
        internal Geometry Hit(MsaglPoint p, double slack, EntityFilterDelegate filter)
        {
            if (filter != null && this.geometry != null)
                if (filter(geometry.dObject) == false)
                    return null;
            if (left == null)
                if (Box.Contains(p, slack))
                {
                    Line line = geometry as Line;

                    if (line != null)
                    {
                        if (Tessellator.DistToSegm(p, line.start, line.end) < slack + line.LineWidth / 2)
                            return line;
                        return null;

                    }
                    else if (Box.Contains(p))
                        return geometry;

                    return null;
                }
                else
                    return null;

            if (left.Box.Contains(p, slack))
            {
                Geometry g = left.Hit(p, slack, filter);
                if (g != null)
                {
                    return g;
                }
            }

            if (right.Box.Contains(p, slack))
            {
                Geometry g = right.Hit(p, slack, filter);
                if (g != null)
                    return g;
            }

            return null;
        }
    }
}