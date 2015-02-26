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

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public abstract class DObject : ObjectWithBox, Drawing.IViewerObject
    {
        internal DObject()
        {
        }

        protected DObject(DObject parent)
        {
            ParentObject = parent;
        }

        public DObject ParentObject { get; internal set; }

        public DGraph ParentGraph
        {
            get
            {
                DObject ret = this;
                while (ret.ParentObject != null)
                    ret = ret.ParentObject;
                return ret as DGraph;
            }
        }

        /// <summary>
        /// get the underlying drawing object
        /// </summary>
        public abstract Drawing.DrawingObject DrawingObject
        {
            get;
        }

        internal float[] DashPatternArray { get; set; }

        internal virtual BBNode BBNode { get; set; }

        override internal Rectangle Box { get { return BBNode.Box; } }

        public abstract void MakeVisual();

        bool markedForDragging;
        /// <summary>
        /// Implements a property of an interface IEditViewer
        /// </summary>
        public bool MarkedForDragging
        {
            get
            {
                return markedForDragging;
            }
            set
            {
                markedForDragging = value;
                if (value)
                {
                    if (MarkedForDraggingEvent != null)
                        MarkedForDraggingEvent(this, null);
                }
                else
                {
                    if (UnmarkedForDraggingEvent != null)
                        UnmarkedForDraggingEvent(this, null);
                }
            }
        }

        /// <summary>
        /// raised when the entity is marked for dragging
        /// </summary>
        public event EventHandler MarkedForDraggingEvent;

        /// <summary>
        /// raised when the entity is unmarked for dragging
        /// </summary>
        public event EventHandler UnmarkedForDraggingEvent;
    }
}