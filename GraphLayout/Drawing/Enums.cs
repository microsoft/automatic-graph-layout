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
#if TEST_MSAGL
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
