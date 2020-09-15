using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using P2 = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.Drawing
{
    /// <summary>
    /// a helper class for creation of node boundary curves
    /// </summary>
  public sealed class NodeBoundaryCurves
  {
    NodeBoundaryCurves() { }
      
      /// <summary>
      /// a helper function to creat a node boundary curve 
      /// </summary>
      /// <param name="node">the node</param>
      /// <param name="width">the node width</param>
      /// <param name="height">the node height</param>
      /// <returns></returns>
    public  static ICurve GetNodeBoundaryCurve(Node node, double width, double height)
    {
      if (node == null)
        throw new InvalidOperationException();
      NodeAttr nodeAttr = node.Attr;

      switch (nodeAttr.Shape)
      {
        case Shape.Ellipse:
        case Shape.DoubleCircle:
          return CurveFactory.CreateEllipse(width, height, new P2(0, 0));
        case Shape.Circle:
          {
            double r = Math.Max(width / 2, height / 2);
            return CurveFactory.CreateEllipse(r, r, new P2(0, 0));
          }

        case Shape.Box:
              if (nodeAttr.XRadius != 0 || nodeAttr.YRadius != 0)
                  return CurveFactory.CreateRectangleWithRoundedCorners(width, height, nodeAttr.XRadius,
                                                                       nodeAttr.YRadius, new P2(0, 0));
              return CurveFactory.CreateRectangle(width, height, new P2(0, 0));


          case Shape.Diamond:
          return CurveFactory.CreateDiamond(
            width, height, new P2(0, 0));
              
          case Shape.House:
              return CurveFactory.CreateHouse(width, height, new P2());

          case Shape.InvHouse:
              return CurveFactory.CreateInvertedHouse(width, height, new P2());
          case Shape.Hexagon:
              return CurveFactory.CreateHexagon(width, height, new P2());
          case Shape.Octagon:
              return CurveFactory.CreateOctagon(width, height, new P2());
#if TEST_MSAGL
          case Shape.TestShape:
              return CurveFactory.CreateTestShape(width, height);
#endif
      
        default:
          {
            return new Ellipse(
              new P2(width / 2, 0), new P2(0, height / 2), new P2());
          }
      }
    }

  }
}
