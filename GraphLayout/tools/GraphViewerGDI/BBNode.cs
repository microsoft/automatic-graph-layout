/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections.Generic;
using Microsoft.Msagl.Drawing;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;
using P2=Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphViewerGdi
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
        internal BBox bBox;
        internal Geometry geometry;

        internal BBox Box
        {
            get
            {
                if (geometry != null)
                    return geometry.bBox;

                return bBox;
            }
        }


        //when we check for inclusion we expand the box by slack
        internal Geometry Hit(P2 p, double slack, EntityFilterDelegate filter, List<Geometry> subgraphCandidates)
        {
            if (filter != null && geometry != null)
                if (filter(geometry.dObject) == false)
                    return null;
            if (left == null)
                if (Box.Contains(p, slack))
                {
                    Line line = geometry as Line;

                    if (line != null)
                    {
                        if (Tessellator.DistToSegm(p, line.start, line.end) < slack + line.LineWidth/2)
                            return line;
                        return null;

                    }
                    if (Box.Contains(p))
                    {
                        var subg = geometry.dObject.DrawingObject as Subgraph;
                        if (subg != null)
                            subgraphCandidates.Add(geometry);
                        else
                            return geometry;
                    }

                    return null;
                }
                else
                    return null;

            if (left.Box.Contains(p, slack))
            {
                Geometry g = left.Hit(p, slack, filter, subgraphCandidates);
                if (g != null)
                {
                    return g;
                }
            }

            if (right.Box.Contains(p, slack))
            {
                Geometry g = right.Hit(p, slack, filter, subgraphCandidates);
                if (g != null)
                    return g;
            }

            return null;
        }
    }
}
