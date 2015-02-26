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
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public abstract class DObject : UserControl, Drawing.IViewerObject
    {
        internal DObject()
        {
            MouseMove += DObject_MouseMove;
        }

        private static MouseEventArgs m_CurrentEvent;
        private static IViewerObject m_LastObjectSet;
        void DObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (e != m_CurrentEvent)
            {
                m_CurrentEvent = e;
                m_LastObjectSet = null;
            }
            if (m_LastObjectSet == null)
            {
                DObject obj = this;
                if (obj is DLabel && !(obj.ParentObject is DEdge))
                    obj = obj.ParentObject;
                if (obj is DGraph)
                    obj = null;
                m_LastObjectSet = ParentGraph.ObjectUnderMouseCursor = obj;
            }
        }

        protected DObject(DObject parent)
            : this()
        {
            ParentObject = parent;
            Canvas.SetZIndex(this, parent == null ? 0 : Canvas.GetZIndex(parent) + 1);
        }

        public DObject ParentObject { get; internal set; }

        // Returns itself if this is a DGraph.
        public DGraph ParentGraph
        {
            get
            {
                DObject ret = this;
                while (ret.ParentObject != null && !(ret is DGraph))
                    ret = ret.ParentObject;
                return ret as DGraph;
            }
        }

        public bool IsAncestorOf(DObject obj)
        {
            if (obj == null)
                return false;
            return obj.ParentObject == this || IsAncestorOf(obj.ParentObject);
        }

        /// <summary>
        /// get the underlying drawing object
        /// </summary>
        public abstract Drawing.DrawingObject DrawingObject
        {
            get;
        }

        public Microsoft.Msagl.Core.Layout.GeometryObject GeometryObject { get { return DrawingObject.GeometryObject; } }

        internal float[] DashPatternArray { get; set; }

        // Copy geometry from the Geometry object to the rendering object.
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