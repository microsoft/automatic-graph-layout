using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Prototype.LayoutEditing;
using GeomNode = Microsoft.Msagl.Core.Layout.Node;
using GeomEdge = Microsoft.Msagl.Core.Layout.Edge;
using GeomLabel = Microsoft.Msagl.Core.Layout.Label;

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Undoes/redoes the node dragging operation. Works for multiple nodes.
    /// </summary>
    public class ObjectDragUndoRedoAction : UndoRedoAction {
        
        /// <summary>
        /// returns true if the bounding box changes
        /// </summary>
        public bool BoundingBoxChanges { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="geometryGraph"></param>
        public ObjectDragUndoRedoAction(GeometryGraph geometryGraph) : base(geometryGraph) {            
        }


        /// <summary>
        /// Undoes the editing
        /// </summary>
        public override void Undo() {
            base.Undo();
            ClearAffectedObjects();

            var restDictionary = CloneRestoreDictionary();

            foreach (var kv in restDictionary) {
                RestoreOnKevValue(kv);
            }
        }

        static void RestoreOnKevValue(KeyValuePair<GeometryObject, RestoreData> kv) {
            if (kv.Value.Action != null) {
                kv.Value.Action();
                return;
            }

            var geomObj = kv.Key;
            var node = geomObj as GeomNode;
            if (node != null) {
                node.BoundaryCurve = ((NodeRestoreData) kv.Value).BoundaryCurve;
            }
            else {
                var edge = geomObj as GeomEdge;
                if (edge != null) {
                    var erd = (EdgeRestoreData) kv.Value;
                    edge.EdgeGeometry.Curve = erd.Curve;
                    edge.UnderlyingPolyline = erd.UnderlyingPolyline;
                    if (edge.EdgeGeometry.SourceArrowhead != null)
                        edge.EdgeGeometry.SourceArrowhead.TipPosition = erd.ArrowheadAtSourcePosition;
                    if (edge.EdgeGeometry.TargetArrowhead != null)
                        edge.EdgeGeometry.TargetArrowhead.TipPosition = erd.ArrowheadAtTargetPosition;
                }
                else {
                    var label = geomObj as GeomLabel;
                    if (label != null) {
                        var lrd = (LabelRestoreData) kv.Value;
                        label.Center = lrd.Center;
                    }
                    else
                        throw new System.NotImplementedException();
                }
            }
        }

        Dictionary<GeometryObject, RestoreData> CloneRestoreDictionary() {
            return new Dictionary<GeometryObject, RestoreData>(restoreDataDictionary);
        }

        /// <summary>
        /// redoes the editing
        /// </summary>
        public override void Redo() {
            base.Redo();
            ClearAffectedObjects();
            var dict = CloneRestoreDictionary();
            foreach (var restoreData in dict)
                RestoreOnKevValue(restoreData);
        }
    }
}