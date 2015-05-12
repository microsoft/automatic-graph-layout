using System;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core.Geometry;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// 
    /// </summary>
    internal class Line : Geometry
    {
        internal MsaglPoint start, end;
        double lineWidth;

        internal double LineWidth
        {
            get { return lineWidth; }
        }
        internal Line(DObject tag, MsaglPoint start, MsaglPoint end, double lw)
            : base(tag)
        {
            lineWidth = lw;
            MsaglPoint dir = end - start;
            if (lineWidth < 0)
                lineWidth = 1;

            double len = dir.Length;
            if (len > ApproximateComparer.IntersectionEpsilon)
            {
                dir /= (len / (lineWidth / 2));
                dir = dir.Rotate(Math.PI / 2);
            }
            else
            {
                dir.X = 0;
                dir.Y = 0;
            }

            this.bBox = new Rectangle(start + dir);
            this.bBox.Add(start - dir);
            this.bBox.Add(end + dir);
            this.bBox.Add(end - dir);
            this.start = start;
            this.end = end;

            if (this.bBox.LeftTop.X == this.bBox.RightBottom.X)
            {
                bBox.LeftTop = bBox.LeftTop + new MsaglPoint(-0.05f, 0);
                bBox.RightBottom = bBox.RightBottom + new MsaglPoint(0.05f, 0);
            }
            if (this.bBox.LeftTop.Y == this.bBox.RightBottom.Y)
            {
                bBox.LeftTop = bBox.LeftTop + new MsaglPoint(0, -0.05f);
                bBox.RightBottom = bBox.RightBottom + new MsaglPoint(0, 0.05f);
            }

        }
    }
}