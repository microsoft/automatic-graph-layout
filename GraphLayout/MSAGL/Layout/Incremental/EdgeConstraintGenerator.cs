using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Layout.Incremental {
    class TNode {
        internal LinkedListNode<TNode> stackNode;
        internal Node v;
        internal bool visited;
        internal List<TNode> outNeighbours = new List<TNode>();
        internal List<TNode> inNeighbours = new List<TNode>();
        internal TNode(Node v) {
            this.v = v;
        }
    }

    /// <summary>
    /// Create separation constraints between the source and target of edges not involved in cycles
    /// in order to better show flow
    /// </summary>
    public class EdgeConstraintGenerator
    {
        EdgeConstraints settings;
        IEnumerable<Edge> edges;
        Dictionary<Node, TNode> nodeMap = new Dictionary<Node, TNode>();
        LinkedList<TNode> stack = new LinkedList<TNode>();
        List<TNode> component;
        AxisSolver horizontalSolver;
        AxisSolver verticalSolver;
        List<Set<Node>> cyclicComponents = new List<Set<Node>>();

        /// <summary>
        /// Creates a VerticalSeparationConstraint for each edge in the given set to structural constraints,
        /// to require these edges to be downward pointing.  Also checks for cycles, and edges involved
        /// in a cycle receive no VerticalSeparationConstraint, but can optionally receive a circle constraint.
        /// </summary>
        /// <param name="edges"></param>
        /// <param name="settings"></param>
        /// <param name="horizontalSolver"></param>
        /// <param name="verticalSolver"></param>
        internal static void GenerateEdgeConstraints(
            IEnumerable<Edge> edges,
            EdgeConstraints settings,
            AxisSolver horizontalSolver,
            AxisSolver verticalSolver)
        {
            if (settings.Direction == Direction.None)
            {
                return;
            }
            EdgeConstraintGenerator g = new EdgeConstraintGenerator(edges, settings, horizontalSolver, verticalSolver);
            g.GenerateSeparationConstraints();
        }

        internal EdgeConstraintGenerator(
            IEnumerable<Edge> edges,
            EdgeConstraints settings,
            AxisSolver horizontalSolver,
            AxisSolver verticalSolver)
        {
            // filter out self edges
            this.edges = edges.Where(e => e.Source != e.Target);

            this.settings = settings;
            this.horizontalSolver = horizontalSolver;
            this.verticalSolver = verticalSolver;

            foreach (var e in this.edges) {
                TNode u = CreateTNode(e.Source), v = CreateTNode(e.Target);
                u.outNeighbours.Add(v);
                v.inNeighbours.Add(u);
            }

            foreach (var v in nodeMap.Values) {
                if(v.stackNode==null) {
                    DFS(v);
                }
            }

            while (stack.Count > 0) {
                component = new List<TNode>();
                RDFS(stack.Last.Value);
                if (component.Count > 1) {
                    var cyclicComponent = new Set<Node>();
                    foreach (var v in component) {
                        cyclicComponent.Insert(v.v);
                    }
                    cyclicComponents.Add(cyclicComponent);
                }
            }

            switch (settings.Direction) {
                case Direction.South:
                    this.addConstraint = this.AddSConstraint;
                    break;
                case Direction.North:
                    this.addConstraint = this.AddNConstraint;
                    break;
                case Direction.West:
                    this.addConstraint = this.AddWConstraint;
                    break;
                case Direction.East:
                    this.addConstraint = this.AddEConstraint;
                    break;
            }
        }

        private delegate void AddConstraint(Node u, Node v);
        private AddConstraint addConstraint;

        private void AddSConstraint(Node u, Node v)
        {
            verticalSolver.AddStructuralConstraint(
                new VerticalSeparationConstraint(u, v, (u.Height + v.Height) / 2 + settings.Separation));
        }

        private void AddNConstraint(Node u, Node v)
        {
            verticalSolver.AddStructuralConstraint(
                new VerticalSeparationConstraint(v, u, (u.Height + v.Height) / 2 + settings.Separation));
        }

        private void AddEConstraint(Node u, Node v)
        {
            horizontalSolver.AddStructuralConstraint(
                new HorizontalSeparationConstraint(v, u, (u.Width + v.Width) / 2 + settings.Separation));
        }

        private void AddWConstraint(Node u, Node v)
        {
            horizontalSolver.AddStructuralConstraint(
                new HorizontalSeparationConstraint(u, v, (u.Width + v.Width) / 2 + settings.Separation));
        }

        /// <summary>
        /// For each edge not involved in a cycle create a constraint
        /// </summary>
        public void GenerateSeparationConstraints() {
            foreach (var e in edges) {
                bool edgeInCycle = false;
                Node u = e.Source, v = e.Target;
                foreach (var c in cyclicComponents) {
                    if (c.Contains(u) && c.Contains(v)) {
                        edgeInCycle = true;
                        break;
                    }
                }
                if (!edgeInCycle) {
                    this.addConstraint(u, v);
                }
            }
        }

        /// <summary>
        /// Get an Enumeration of CyclicComponents
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IEnumerable<IEnumerable<Node>> CyclicComponents {
            get {
                return from c in cyclicComponents
                       select c.AsEnumerable();
            }
        }
        private void DFS(TNode u) {
            u.visited = true;
            foreach (var v in u.outNeighbours) {
                if (!v.visited) {
                    DFS(v);
                }
            }
            PushStack(u);
        }
        private void RDFS(TNode u) {
            component.Add(u);
            PopStack(u);
            foreach (var v in u.inNeighbours) {
                if (v.stackNode != null) {
                    RDFS(v);
                }
            }
        }
        private TNode CreateTNode(Node v) {
            TNode tv;
            if (!nodeMap.ContainsKey(v)) {
                tv = new TNode(v);
                nodeMap[v] = tv;
            } else {
                tv = nodeMap[v];
            }
            return tv;
        }
        private void PushStack(TNode v) {
            v.stackNode = stack.AddLast(v);
        }
        private void PopStack(TNode v) {
            stack.Remove(v.stackNode);
            v.stackNode = null;
        }
    }
}
