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
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Base class for graph objects  
    /// </summary>
    [Serializable]
    public abstract class DrawingObject {
        /// <summary>
        /// 
        /// </summary>
        public event Action<DrawingObject> IsVisibleChanged;
        object userData;
        /// <summary>
        /// This field can be used as a backpointer to the user data associated with the object
        /// </summary>
        public object UserData {
            get { return userData; }
            set { userData = value; }
        }


        /// <summary>
        /// gets the bounding box of the object
        /// </summary>
        abstract public Rectangle BoundingBox { get;}
        /// <summary>
        /// gets the geometry object corresponding to the drawing object
        /// </summary>
        public abstract GeometryObject GeometryObject { get; set; }

        bool isVisible = true;

        /// <summary>
        /// gets or sets the visibility of an object
        /// </summary>
        virtual public bool IsVisible {
            get { return isVisible; }
            set {
                var was = isVisible;
                isVisible = value;
                if (was != isVisible && IsVisibleChanged != null)
                    IsVisibleChanged(this);
                
            }
        }
    }
}
