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
using System.ComponentModel;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MST;

namespace Microsoft.Msagl.Layout.MDS
{
    /// <summary>
    /// MDL layout settings
    /// </summary>
#if PROPERTY_GRID_SUPPORT
    [DisplayName("MDS layout settings")]
    [Description("Setting for Multi Dimensional Scaling algorithm"),
    TypeConverterAttribute(typeof(ExpandableObjectConverter))]
#endif
    public class MdsLayoutSettings : LayoutAlgorithmSettings
    {

        /// <summary>
        /// the setting of Multi-Dimensional Scaling layout
        /// </summary>
        public MdsLayoutSettings(){}
        // private double epsilon = Math.Pow(10,-8);
        private int pivotNumber = 50;
        private int iterationsWithMajorization = 30;
        private double scaleX = 200;
        private double scaleY = 200;
        private double exponent = -2;
        private double rotationAngle;

        private bool removeOverlaps = true;

        private OverlapRemovalMethod overlapMethod=OverlapRemovalMethod.Pmst;
        /// <summary>
        /// 
        /// </summary>
#if SHARPKIT // no multithreading in JS
        public bool RunInParallel = false;
#else
        public bool RunInParallel = true;
#endif

        /// remove overlaps between node boundaries
        public bool RemoveOverlaps
        {
            set { removeOverlaps = value; }
            get { return removeOverlaps; }
        }
        
        /*
        /// <summary>
        /// Level of convergence accuracy (the closer to zero, the more accurate).
        /// </summary>
        [Description("this is the epsilon")]
        public double Epsilon {
            set { epsilon=value; }
            get { return epsilon; }
        }
        */

        /// <summary>
        /// Number of pivots in Landmark Scaling (between 3 and number of objects).
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Number of pivots")]
        [Description("Number of pivots in MDS")]
        [DefaultValue(50)]
#endif
        public int PivotNumber
        {
            set { pivotNumber = value; }
            get { return pivotNumber; }
        }

        /// <summary>
        /// Number of iterations in distance scaling
        /// </summary>

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Majorization")]
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Number of iterations with majorization")]
        [Description("If this number is positive then the majorization method will be used with the initial solution taken from the landmark method")]
        [DefaultValue(0)]
#endif
        public int IterationsWithMajorization
        {
            set { iterationsWithMajorization = value; }
            get { return iterationsWithMajorization; }
        }

        /// <summary>
        /// X Scaling Factor.
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Scale by x-axis")]
        [Description("The resulting layout will be scaled in the x-axis by this number")]
        [DefaultValue(200.0)]
#endif
        public double ScaleX
        {
            set { scaleX = value; }
            get { return scaleX; }
        }

        /// <summary>
        /// Y Scaling Factor.
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Scale by y-axis")]
        [Description("The resulting layout will be scaled by y-axis by this number")]
        [DefaultValue(200.0)]
#endif
        public double ScaleY
        {
            set { scaleY = value; }
            get { return scaleY; }
        }

        /// <summary>
        /// Weight matrix exponent.
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [Description("The power to raise the distances to in the majorization step")]
        [DefaultValue(-2.00)]
#endif
        public double Exponent
        {
            set { exponent = value; }
            get { return exponent; }
        }

        /// <summary>
        /// rotation angle
        /// </summary>
#if PROPERTY_GRID_SUPPORT
        [DisplayName("Rotation angle")]
        [Description("The resulting layout will be rotated by this angle")]
        [DefaultValue(0.0)]
#endif
        public double RotationAngle
        {
            set { rotationAngle = value % 360; }
            get { return rotationAngle; }
        }
        /// <summary>
        /// Clones the object
        /// </summary>
        /// <returns></returns>
        public override LayoutAlgorithmSettings Clone()
        {
            return MemberwiseClone() as LayoutAlgorithmSettings;
        }

        /// <summary>
        /// Settings for calculation of ideal edge length
        /// </summary>
        public IdealEdgeLengthSettings IdealEdgeLength { get; set; }

        /// <summary>
        /// Adjust the scale of the graph if there is not enough whitespace between nodes
        /// </summary>
        public bool AdjustScale { get; set; }

      
        /// <summary>
        /// The method which should be used to remove the overlaps.
        /// </summary>
        public OverlapRemovalMethod OverlapRemovalMethod {
            get { return overlapMethod;} 
            set { overlapMethod = value; } 
        }
    }
}
