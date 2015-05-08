//
// Shape.cs
// MSAGL Shape class for Rectilinear Edge Routing.
//
// Copyright Microsoft Corporation.

using System;
using System.Collections.Generic;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core;
using System.Diagnostics;

namespace Microsoft.Msagl.Routing {
    /// <summary>
    /// A shape wrapping an ICurve, providing additional information.
    /// </summary>
    [DebuggerDisplay("Shape = {UserData}")]
    public class Shape {

        readonly Set<Shape> parents = new Set<Shape>();
        ///<summary>
        /// shape parents
        ///</summary>
        public IEnumerable<Shape> Parents {
            get { return parents; }
        }

        readonly Set<Shape> children = new Set<Shape>();
        /// <summary>
        /// shape children
        /// </summary>
        public IEnumerable<Shape> Children {
            get { return children; }
        }
        /// <summary>
        /// The curve of the shape.
        /// </summary>
        public virtual ICurve BoundaryCurve { 
            get { return boundaryCurve; }
            set {boundaryCurve = value; }
        }
        ICurve boundaryCurve;

        /// <summary>
        /// The bounding box of the shape.
        /// </summary>
        public Rectangle BoundingBox { get { return BoundaryCurve.BoundingBox; } }

        /// <summary>
        /// The set of Ports for this obstacle, usually RelativePorts.  In the event of overlapping
        /// obstacles, this identifies the obstacle to which the port applies.
        /// </summary>
        public Set<Port> Ports { get { return ports; } }

        private readonly Set<Port> ports = new Set<Port>();

        /// <summary>
        /// A location for storing user data associated with the Shape.
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Shape() : this (null) {
        }

        /// <summary>
        /// Constructor taking the ID and the curve of the shape.
        /// </summary>
        /// <param name="boundaryCurve"></param>
        public Shape(ICurve boundaryCurve) {
            this.boundaryCurve = boundaryCurve;     // RelativeShape throws an exception on BoundaryCurve_set so set _boundaryCurve directly.
        }

        /// <summary>
        /// A group is a shape that has children.
        /// </summary>
        public bool IsGroup {
            get { return children.Count > 0; }
        }

        internal bool IsTransparent { get; set; }

        internal IEnumerable<Shape> Descendants {
            get {
                var q = new Queue<Shape>();
                foreach (var shape in Children)
                    q.Enqueue(shape);
                while (q.Count > 0) {
                    var sh = q.Dequeue();
                    yield return sh;
                    foreach (var shape in sh.Children)
                        q.Enqueue(shape);
                }
            }
        }

        internal IEnumerable<Shape> Ancestors {
            get {
                var q = new Queue<Shape>();
                foreach (var shape in Parents)
                    q.Enqueue(shape);
                while (q.Count > 0) {
                    var sh = q.Dequeue();
                    yield return sh;
                    foreach (var shape in sh.Parents)
                        q.Enqueue(shape);
                }
            }
        }
        ///<summary>
        /// Adds a parent. A shape can have several parents
        ///</summary>
        ///<param name="shape"></param>
        public void AddParent(Shape shape) {
            ValidateArg.IsNotNull(shape, "shape");
            parents.Insert(shape);
            shape.children.Insert(this);
        }

        ///<summary>
        ///</summary>
        ///<param name="shape"></param>
        public void AddChild(Shape shape) {
            ValidateArg.IsNotNull(shape, "shape");
            shape.parents.Insert(this);
            children.Insert(shape);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shape"></param>
        public void RemoveChild(Shape shape) {
            ValidateArg.IsNotNull(shape, "shape");
            children.Remove(shape);
            shape.parents.Remove(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shape"></param>
        public void RemoveParent(Shape shape) {
            ValidateArg.IsNotNull(shape, "shape");
            parents.Remove(shape);
            shape.children.Remove(this);
        }

#if TEST_MSAGL
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return UserData == null ? "null" : UserData.ToString();
        }
#endif 
    }
}
