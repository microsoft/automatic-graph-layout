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
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Layout.LargeGraphLayout;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// facilitates large graph browsing
    /// </summary>
    public class LgNodeInfo : LgInfoBase {
        //these needed for shortest path calculations
        internal Edge Prev; 
        internal double Cost;
        internal bool InQueue;
        internal bool Processed;
        /// <summary>
        /// underlying geometry node
        /// </summary>
        public Node GeometryNode { get; set; }

        double scale = 1;

        /// <summary>
        /// if scale is big enough then we should treat this node as having edges, text box etc.
        /// </summary>
        public bool ScaleIsBigEnough {
            get { return scale >= 0.8; }
        }

        

        /// <summary>
        /// 
        /// </summary>
        public double Scale {
            get { return scale; }
            set { 
                scale = value;
//                if (OriginalCurveOfGeomNode != null)
//                    GeometryNode.BoundaryCurve =
//                        OriginalCurveOfGeomNode.Clone()
//                                               .Transform(PlaneTransformation.ScaleAroundCenterTransformation(Scale,
//                                                                                                              GeometryNode
//                                                                                                                  .Center));
                GeometryNode.RaiseLayoutChangeEvent(value); //todo: see that VNode.Invalidate is called on this event
            }
        }
        /// <summary>
        /// overrides the base function, can return the scaled boundary
        /// </summary>
        public ICurve BoundaryCurve {
            get { return GeometryNode.BoundaryCurve; }            
        }

        
        internal LgNodeInfo Parent;

        
        int connectedComponentId = -1;//
       
        internal LgNodeInfo(Node geometryNode) {
            GeometryNode = geometryNode;
            //OriginalCurveOfGeomNode = geometryNode.BoundaryCurve.Clone();
        }

        internal int ConnectedComponentId {
            get { return connectedComponentId; }
            set { connectedComponentId = value; }
        }
        
        /// <summary>
        /// the center of the node
        /// </summary>
        public Point Center {
            get { return GeometryNode.Center; }
        }

        LgNodeInfoKind _kind;
        /// <summary>
        /// if a node is open it might be rendered by the viewer
        /// </summary>
        public LgNodeInfoKind Kind
        {
            get { return _kind; }
            set { _kind = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Rectangle BoundingBox {
            get { return GeometryNode.BoundaryCurve.BoundingBox; }
        }

        /// <summary>
        /// override the string method
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return GeometryNode.ToString();
        }
    }
}