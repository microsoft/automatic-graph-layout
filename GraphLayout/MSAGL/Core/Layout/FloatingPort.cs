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
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// floating port: specifies that the edge is routed to the Location 
    /// </summary>
    public class FloatingPort : Port {
        ICurve curve; //a curve associated with the port

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="location"></param>
        /// <param name="curve">the port curve, can be null</param>
        public FloatingPort(ICurve curve, Point location){
            this.curve = curve;
            this.location = location;
        }


        
        Point location;
        /// <summary>
        /// the location of the port
        /// </summary>
        override public Point Location {
            get { return location; }
        }
        /// <summary>
        /// translate the port location by delta
        /// </summary>
        /// <param name="delta"></param>
        public virtual void Translate(Point delta)
        {
            location += delta;
        }
/// <summary>
/// the port's curve
/// </summary>
        public override ICurve Curve {
            get { return curve; }
            set { curve = value; }
        }

        /// <summary>
        /// Return a string representation of the Port location
        /// </summary>
        /// <returns>a string representation of the Port location</returns>
        public override string ToString() {
            return Location.ToString();
        }
    }
}
