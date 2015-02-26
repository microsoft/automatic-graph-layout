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
using Microsoft.Msagl.Core.Geometry;


namespace Microsoft.Msagl.Core.Layout {
    /// <summary>
    /// The base class of the Graph,Node and Edge classes
    /// </summary>
#if TEST_MSAGL
    [Serializable]
#endif
    abstract public class GeometryObject {
       
        GeometryObject geometryParent;
        /// <summary>
        /// the parent of the object
        /// </summary>
        public GeometryObject GeometryParent {
            get { return geometryParent; }
            set { geometryParent = value; }
        }

        /// <summary>
        /// Storage for any data algorithms may want to store temporarily.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
#if TEST_MSAGL
        [NonSerialized]
#endif

        public object AlgorithmData;

        /// <summary>
        /// keeps the back pointer to the user data
        /// </summary>
        public object UserData { get; set; }

        event EventHandler<LayoutChangeEventArgs> beforeLayoutChangeEvent;
        
        /// <summary>
        /// event signalling that the layout is about to change
        /// </summary>
        virtual public event EventHandler<LayoutChangeEventArgs> BeforeLayoutChangeEvent {
            add { beforeLayoutChangeEvent += value; }
            remove { beforeLayoutChangeEvent -= value; }
        }
        
        /// <summary>
        /// gets or sets the boundary box of a GeometryObject
        /// </summary>
        public abstract Rectangle BoundingBox { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newValue"></param>
        public virtual void RaiseLayoutChangeEvent(object newValue) {
            if (beforeLayoutChangeEvent != null)
                beforeLayoutChangeEvent(this, new LayoutChangeEventArgs {DataAfterChange = newValue});
        }
        
        
    }

}
