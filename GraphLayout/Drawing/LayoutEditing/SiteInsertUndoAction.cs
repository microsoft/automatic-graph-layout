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
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Prototype.LayoutEditing;
using Microsoft.Msagl.Routing;
using GeomEdge = Microsoft.Msagl.Core.Layout.Edge;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// undoes/redoes edge editing when dragging the smoothed polyline corner
    /// </summary>
    public class SiteInsertUndoAction : UndoRedoAction {
        Site insertedSite;
        Point insertionPoint;
        Site prevSite;

        internal Site PrevSite {
            get { return prevSite; }
            set { prevSite = value; }
        }

        double siteKPrevious;

        /// <summary>
        /// k - the coefficient giving the start and the end spline points
        /// </summary>
        public double SiteKPrevious {
            get { return siteKPrevious; }
            set { siteKPrevious = value; }
        }

        double siteKNext;

        /// <summary>
        /// k - the coefficient giving the start and the end spline points
        /// </summary>
        public double SiteKNext {
            get { return siteKNext; }
            set { siteKNext = value; }
        }

        /// <summary>
        /// The point where the new polyline corner was inserted
        /// </summary>
        public Point InsertionPoint {
            get { return insertionPoint; }
            set { insertionPoint = value; }
        }

        internal Site InsertedSite {
            get { return insertedSite; }
            set {
                insertedSite = value;
                this.InsertionPoint = insertedSite.Point;
                this.SiteKNext = insertedSite.NextBezierSegmentFitCoefficient;
                this.SiteKPrevious = insertedSite.PreviousBezierSegmentFitCoefficient;
                this.PrevSite = insertedSite.Previous;
            }
        }

        GeomEdge editedEdge;

        /// <summary>
        /// Constructor. At the moment of the constructor call the site should not be inserted yet
        /// </summary>
        /// <param name="edgeToEdit"></param>
        public SiteInsertUndoAction(GeomEdge edgeToEdit)
            : base((GeometryGraph)edgeToEdit.GeometryParent) {
            this.editedEdge = edgeToEdit;
            this.AddRestoreData(editedEdge, RestoreHelper.GetRestoreData(editedEdge));
        }
        /// <summary>
        /// undoes the editing
        /// </summary>
        public override void Undo() {
            Site prev = InsertedSite.Previous;
            Site next = InsertedSite.Next;
            prev.Next = next;
            next.Previous = prev;
            GeometryGraphEditor.DragEdgeWithSite(new Point(0, 0), editedEdge, prev);
        }

        /// <summary>
        /// redoes the editing
        /// </summary>
        public override void Redo() {
            insertedSite = new Site(PrevSite, InsertionPoint, PrevSite.Next);
            insertedSite.NextBezierSegmentFitCoefficient = this.SiteKNext;
            insertedSite.PreviousBezierSegmentFitCoefficient = this.SiteKPrevious;
            GeometryGraphEditor.DragEdgeWithSite(new Point(0, 0), editedEdge, insertedSite);
        }
    }
}