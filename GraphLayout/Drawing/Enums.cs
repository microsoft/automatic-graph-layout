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
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Microsoft.Msagl.Drawing {

    /// <summary>
    /// 
    /// </summary>
    public enum LayerDirection {
        /// <summary>
        /// top to bottom
        /// </summary>
        TB,
        /// <summary>
        /// left to right
        /// </summary>
        LR,
        /// <summary>
        /// bottom top
        /// </summary>
        BT,
        /// <summary>
        /// right to left
        /// </summary>
        RL,
        /// <summary>
        /// 
        /// </summary>
        None
    }

    ///<summary>
    ///Defines a shape of the node
    ///</summary>
    [Description("Shape of the node") ]

    public enum Shape {
        /// <summary>
        /// draws a diamond
        /// </summary>
        Diamond,
        /// <summary>
        /// 
        /// </summary>
        Ellipse,
        /// <summary>
        /// 
        /// </summary>
        Box,
        /// <summary>
        /// 
        /// </summary>
        Circle,
        /// <summary>
        /// 
        /// </summary>
        Record,
        /// <summary>
        ///does not provide any boundary 
        /// </summary>
        Plaintext,
        /// <summary>
        /// draws a solid point
        /// </summary>
        Point,
        /// <summary>
        /// Draws a dot
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mdiamond")]
        Mdiamond,
        /// <summary>
        /// Not supported.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msquare")]
        Msquare,
        /// <summary>
        /// Not supported.
        /// </summary>
        Polygon,
        /// <summary>
        /// double circle
        /// </summary>
        DoubleCircle,
        /// <summary>
        /// Draws a box with a roof
        /// </summary>
        House,
        /// <summary>
        /// Draws an inverterted house
        /// </summary>
        InvHouse,
        /// <summary>
        /// Not supported.
        /// </summary>
        Parallelogram,
        /// <summary>
        /// octagon
        /// </summary>
        Octagon,
        /// <summary>
        /// Not supported.
        /// </summary>
        TripleOctagon,
        /// <summary>
        /// Not supported.
        /// </summary>
        Triangle,
        /// <summary>
        /// Not supported.
        /// </summary>
        Trapezium,
        /// <summary>
        /// the curve is given by the geometry node
        /// </summary>
        DrawFromGeometry,
#if DEBUG
        /// <summary>
        /// testing only, don't use
        /// </summary>
        TestShape,
#endif
        /// <summary>
        /// hexagon
        /// </summary>
        Hexagon
    }
}
