#region Using directives

using System;
using System.Collections.Generic;

#endregion

namespace Microsoft.Msagl.Core.Geometry.Curves
{

    /// <summary>
    /// An internal node of ParallelogramNodeOverICurve hierarchy.
    /// Is used in curve intersections routines.
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
  internal class ParallelogramInternalTreeNode : ParallelogramNodeOverICurve
  {
    List<ParallelogramNodeOverICurve> children = new List<ParallelogramNodeOverICurve>();

    internal ParallelogramInternalTreeNode(ICurve seg, double leafBoxesOffset):base(seg,leafBoxesOffset)
    {
    }

    /// <summary>
    /// children of the node
    /// </summary>
    /// <value></value>
    internal List<ParallelogramNodeOverICurve> Children
    {
      get { return children; }
    }

    internal void AddChild(ParallelogramNodeOverICurve node)
    {
      children.Add(node);
    }
  }

}
