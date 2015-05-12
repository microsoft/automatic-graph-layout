using System;
using Microsoft.Msagl.Drawing;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// Summary description for Geometry.
    /// </summary>
    internal class Geometry : ObjectWithBox
    {
        internal DObject dObject;

        internal override MsaglRectangle Box { get { return bBox; } }

        internal MsaglRectangle bBox;

        internal Geometry(DObject dObject, MsaglRectangle box)
        {
            this.dObject = dObject;
            this.bBox = box;
        }
        internal Geometry(DObject dObject)
        {
            this.dObject = dObject;

            DNode dNode = dObject as DNode;
            if (dNode != null)
                bBox = dNode.DrawingNode.BoundingBox;
            else
            {
                DLabel dLabel = dObject as DLabel;
                if (dLabel != null)
                    bBox = dLabel.Label.BoundingBox;
            }
        }
    }
}