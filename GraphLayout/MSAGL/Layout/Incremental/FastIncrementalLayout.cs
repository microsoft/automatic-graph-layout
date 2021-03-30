using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Routing;
using Node = Microsoft.Msagl.Core.Layout.Node;

namespace Microsoft.Msagl.Layout.Incremental {
    /// <summary>
    /// Fast incremental layout is a force directed layout strategy with approximate computation of long-range node-node repulsive forces to achieve O(n log n) running time per iteration.
    /// It can be invoked on an existing layout (for example, as computed by MDS) to beautify it.  See docs for CalculateLayout method (below) to see how to use it incrementally.
    /// 
    /// Note that in debug mode lots of numerical checking is applied, which slows things down considerably.  So, run in Release mode unless you're actually debugging!
    /// </summary>
    public class FastIncrementalLayout : AlgorithmBase {
        readonly BasicGraphOnEdges<FiEdge> basicGraph;
        readonly List<FiNode[]> components;
        internal readonly Dictionary<int, List<IConstraint>> constraints = new Dictionary<int, List<IConstraint>>();
        readonly List<FiEdge> edges = new List<FiEdge>();

        /// <summary>
        /// Returns the derivative of the cost function calculated in the most recent iteration.
        /// It's a volatile float so that we can potentially access it from other threads safely,
        /// for example during test.
        /// </summary>
        internal volatile float energy;

        readonly GeometryGraph graph;

        readonly AxisSolver horizontalSolver;

        /// <summary>
        /// Construct a graph by adding nodes and edges to these lists
        /// </summary>
        readonly FiNode[] nodes;

        int progress;
        readonly FastIncrementalLayoutSettings settings;
        double stepSize;

        readonly AxisSolver verticalSolver;

        readonly Func<Cluster, LayoutAlgorithmSettings> clusterSettings;

        List<Edge> clusterEdges = new List<Edge>();

        /// <summary>
        /// Create the graph data structures.
        /// </summary>
        /// <param name="geometryGraph"></param>
        /// <param name="settings">The settings for the algorithm.</param>
        /// <param name="initialConstraintLevel">initialize at this constraint level</param>
        /// <param name="clusterSettings">settings by cluster</param>
        internal FastIncrementalLayout(GeometryGraph geometryGraph, FastIncrementalLayoutSettings settings,
                                       int initialConstraintLevel,
                                       Func<Cluster, LayoutAlgorithmSettings> clusterSettings) {
            graph = geometryGraph;
            this.settings = settings;
            this.clusterSettings = clusterSettings;
            int i = 0;
            ICollection<Node> allNodes = graph.Nodes;
            nodes = new FiNode[allNodes.Count];
            foreach (Node v in allNodes) {
                v.AlgorithmData = nodes[i] = new FiNode(i, v);
                i++;
            }

            clusterEdges.Clear();
            edges.Clear();

            foreach (Edge e in graph.Edges) {
                if (e.Source is Cluster || e.Target is Cluster)
                    clusterEdges.Add(e);
                else
                    edges.Add(new FiEdge(e));
                foreach (var l in e.Labels)
                    l.InnerPoints = l.OuterPoints = null;
            }
            SetLockNodeWeights();
            components = new List<FiNode[]>();
            if (!settings.InterComponentForces) {
                basicGraph = new BasicGraphOnEdges<FiEdge>(edges, nodes.Length);
                foreach (var componentNodes in ConnectedComponentCalculator<FiEdge>.GetComponents(basicGraph)) {
                    var vs = new FiNode[componentNodes.Count()];
                    int vi = 0;
                    foreach (int v in componentNodes) {
                        vs[vi++] = nodes[v];
                    }
                    components.Add(vs);
                }
            }
            else // just one big component (regardless of actual edges)
                components.Add(nodes);
            horizontalSolver = new AxisSolver(true, nodes, new[] {geometryGraph.RootCluster}, settings.AvoidOverlaps,
                                              settings.MinConstraintLevel, clusterSettings) {
                                                  OverlapRemovalParameters =
                                                      new OverlapRemovalParameters {
                                                          AllowDeferToVertical = true,
                   // use "ProportionalOverlap" mode only when iterative apply forces layout is being used.
                   // it is not necessary otherwise.
                                                          ConsiderProportionalOverlap = settings.ApplyForces
                                                      }
                                              };
            verticalSolver = new AxisSolver(false, nodes, new[] {geometryGraph.RootCluster}, settings.AvoidOverlaps,
                                            settings.MinConstraintLevel, clusterSettings);

            SetupConstraints();
            geometryGraph.RootCluster.ComputeWeight();

            foreach (
                Cluster c in geometryGraph.RootCluster.AllClustersDepthFirst().Where(c => c.RectangularBoundary == null)
                ) {
                c.RectangularBoundary = new RectangularClusterBoundary();
            }

            CurrentConstraintLevel = initialConstraintLevel;
        }

        void SetupConstraints() {
            AddConstraintLevel(0);
            if (settings.AvoidOverlaps) {
                AddConstraintLevel(2);
            }
            foreach (IConstraint c in settings.StructuralConstraints) {
                AddConstraintLevel(c.Level);
                if (c is VerticalSeparationConstraint) {
                    verticalSolver.AddStructuralConstraint(c);
                }
                else if (c is HorizontalSeparationConstraint) {
                    horizontalSolver.AddStructuralConstraint(c);
                }
                else {
                    AddConstraint(c);
                }
            }
            EdgeConstraintGenerator.GenerateEdgeConstraints(graph.Edges, settings.IdealEdgeLength, horizontalSolver,
                                                            verticalSolver);
        }

        int currentConstraintLevel;

        /// <summary>
        /// Controls which constraints are applied in CalculateLayout.  Setter enforces feasibility at that level.
        /// </summary>
        internal int CurrentConstraintLevel {
            get { return currentConstraintLevel; }
            set {
                currentConstraintLevel = value;
                horizontalSolver.ConstraintLevel = value;
                verticalSolver.ConstraintLevel = value;
                Feasibility.Enforce(settings, value, nodes, horizontalSolver.structuralConstraints,
                                    verticalSolver.structuralConstraints, new[] {graph.RootCluster}, clusterSettings);
                settings.Unconverge();
            }
        }

        /// <summary>
        /// Add constraint to constraints lists.  Warning, no check that dictionary alread holds a list for the level.
        /// Make sure you call AddConstraintLevel first (perf).
        /// </summary>
        /// <param name="c"></param>
        void AddConstraint(IConstraint c) {
            if (!constraints.ContainsKey(c.Level)) {
                constraints[c.Level] = new List<IConstraint>();
            }
            constraints[c.Level].Add(c);
        }

        /// <summary>
        /// Check for constraint level in dictionary, if it doesn't exist add the list at that level.
        /// </summary>
        /// <param name="level"></param>
        void AddConstraintLevel(int level) {
            if (!constraints.ContainsKey(level)) {
                constraints[level] = new List<IConstraint>();
            }
        }

        internal void SetLockNodeWeights() {
            foreach (LockPosition l in settings.locks) {
                l.SetLockNodeWeight();
            }
        }

        internal void ResetNodePositions() {
            foreach (FiNode v in nodes) {
                v.ResetBounds();
            }
            foreach (var e in edges) {
                foreach (var l in e.mEdge.Labels) {
                    l.InnerPoints = l.OuterPoints = null;
                }
            }
        }

        void AddRepulsiveForce(FiNode v, Point repulsion) {
            // scale repulsion
            v.force = 10.0*settings.RepulsiveForceConstant*repulsion;
        }

        void AddLogSpringForces(FiEdge e, Point duv, double d) {
            double l = duv.Length,
                   f = 0.0007*settings.AttractiveForceConstant*l*Math.Log((l + 0.1)/(d + 0.1));
            e.source.force += f*duv;
            e.target.force -= f*duv;
        }

        void AddSquaredSpringForces(FiEdge e, Point duv, double d) {
            double l = duv.Length,
                   d2 = d*d + 0.1,
                   f = settings.AttractiveForceConstant*(l - d)/d2;
            e.source.force += f*duv;
            e.target.force -= f*duv;
        }

        void AddSpringForces(FiEdge e) {
            Point duv;
            if (settings.RespectEdgePorts) {
                var sourceLocation = e.source.Center;
                var targetLocation = e.target.Center;
                var sourceFloatingPort = e.mEdge.SourcePort as FloatingPort;
                if (sourceFloatingPort != null) {
                    sourceLocation = sourceFloatingPort.Location;
                }
                var targetFloatingPort = e.mEdge.TargetPort as FloatingPort;
                if (targetFloatingPort != null) {
                    targetLocation = targetFloatingPort.Location;
                }
                duv = sourceLocation - targetLocation;
            }
            else {
                duv = e.vector();
            }
            if (settings.LogScaleEdgeForces) {
                AddLogSpringForces(e, duv, e.mEdge.Length);
            }
            else {
                AddSquaredSpringForces(e, duv, e.mEdge.Length);
            }
        }

        static void AddGravityForce(Point origin, double gravity, FiNode v) {
            // compute and add gravity
            v.force -= 0.0001*gravity*(origin - v.Center);
        }

        void ComputeRepulsiveForces(FiNode[] vs) {
            int n = vs.Length;
            if (n > 16 && settings.ApproximateRepulsion) {
                var ps = new KDTree.Particle[vs.Length];
                // before calculating forces we perturb each center by a small vector in a unique
                // but deterministic direction (by walking around a circle in n steps) - this allows
                // the KD-tree to decompose even when some nodes are at exactly the same position
                double angle = 0, angleDelta = 2.0*Math.PI/n;
                for (int i = 0; i < n; ++i) {
                    ps[i] = new KDTree.Particle(vs[i].Center + 1e-5*new Point(Math.Cos(angle), Math.Sin(angle)));
                    angle += angleDelta;
                }
                var kdTree = new KDTree(ps, 8);
                kdTree.ComputeForces(5);
                for (int i = 0; i < vs.Length; ++i) {
                    AddRepulsiveForce(vs[i], ps[i].force);
                }
            }
            else {
                foreach (FiNode u in vs) {
                    var fu = new Point();
                    foreach (FiNode v in vs) {
                        if (u != v) {
                            fu += MultipoleCoefficients.Force(u.Center, v.Center);
                        }
                    }
                    AddRepulsiveForce(u, fu);
                }
            }
        }

        void AddClusterForces(Cluster root) {
            if (root == null)
                return;
            // SetBarycenter is recursive.
            root.SetBarycenter();
            // The cluster edges draw the barycenters of the connected clusters together
            foreach (var e in clusterEdges) {
                // foreach cluster keep a force vector.  Replace ForEachNode calls below with a simple
                // addition to this force vector.  Traverse top down, tallying up force vectors of children
                // to be the sum of their parents.
                var c1 = e.Source as Cluster;
                var c2 = e.Target as Cluster;
                var n1 = e.Source.AlgorithmData as FiNode;
                var n2 = e.Target.AlgorithmData as FiNode;
                Point center1 = c1 != null ? c1.Barycenter : n1.Center;
                Point center2 = c2 != null ? c2.Barycenter : n2.Center;
                Point duv = center1 - center2;
                double l = duv.Length,
                       f = 1e-8*settings.AttractiveInterClusterForceConstant*l*Math.Log(l + 0.1);
                if (c1 != null) {
                    c1.ForEachNode(v => {
                        var fv = v.AlgorithmData as FiNode;
                        fv.force += f*duv;
                    });
                }
                else {
                    n1.force += f*duv;
                }
                if (c2 != null) {
                    c2.ForEachNode(v => {
                        var fv = v.AlgorithmData as FiNode;
                        fv.force -= f*duv;
                    });
                }
                else {
                    n2.force -= f*duv;
                }
            }
            foreach (Cluster c in root.AllClustersDepthFirst())
                if (c != root) {
                    c.ForEachNode(v => AddGravityForce(c.Barycenter, settings.ClusterGravity, (FiNode) v.AlgorithmData));
                }
        }

        /// <summary>
        /// Aggregate all the forces affecting each node
        /// </summary>
        void ComputeForces() {
            if (components != null) {
                components.ForEach(ComputeRepulsiveForces);
            }
            else {
                ComputeRepulsiveForces(nodes);
            }
            edges.ForEach(AddSpringForces);
            foreach (var c in components) {
                var origin = new Point();
                for (int i = 0; i < c.Length; ++i) {
                    origin += c[i].Center;
                }
                origin /= (double) c.Length;
                double maxForce = double.NegativeInfinity;
                for (int i = 0; i < c.Length; ++i) {
                    FiNode v = c[i];
                    AddGravityForce(origin, settings.GravityConstant, v);
                    if (v.force.Length > maxForce) {
                        maxForce = v.force.Length;
                    }
                }
                if (maxForce > 100.0) {
                    for (int i = 0; i < c.Length; ++i) {
                        c[i].force *= 100.0/maxForce;
                    }
                }
            }
            // This is the only place where ComputeForces (and hence verletIntegration) considers clusters.
            // It's just adding a "gravity" force on nodes inside each cluster towards the barycenter of the cluster.
            AddClusterForces(graph.RootCluster);
        }

        void SatisfyConstraints() {
            for (int i = 0; i < settings.ProjectionIterations; ++i) {
                foreach (var level in constraints.Keys) {
                    if (level > CurrentConstraintLevel) {
                        break;
                    }
                    foreach (var c in constraints[level]) {
                        c.Project();
                        // c.Project operates only on MSAGL nodes, so need to update the local FiNode.Centers
                        foreach (var v in c.Nodes) {
                            ((FiNode) v.AlgorithmData).Center = v.Center;
                        }
                    }
                }

                foreach (LockPosition l in settings.locks) {
                    l.Project();
                    // again, project operates only on MSAGL nodes, we'll also update FiNode.PreviousPosition since we don't want any inertia in this case
                    foreach (var v in l.Nodes) {
                        FiNode fiNode = v.AlgorithmData as FiNode;

                        // the locks should have had their AlgorithmData updated, but if (for some reason)
                        // the locks list is out of date we don't want to null ref here.
                        if (fiNode != null && v.AlgorithmData != null) {
                            fiNode.ResetBounds();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if solvers need to be applied, i.e. if there are user constraints or 
        /// generated constraints (such as non-overlap) that need satisfying
        /// </summary>
        /// <returns></returns>
        bool NeedSolve() {
            return horizontalSolver.NeedSolve || verticalSolver.NeedSolve;
        }

        /// <summary>
        /// Force directed layout is basically an iterative approach to solving a bunch of differential equations.
        /// Different integration schemes are possible for applying the forces iteratively.  Euler is the simplest:
        ///  v_(i+1) = v_i + a dt
        ///  x_(i+1) = x_i + v_(i+1) dt
        /// 
        /// Verlet is much more stable (and not really much more complicated):
        ///  x_(i+1) = x_i + (x_i - x_(i-1)) + a dt dt
        /// </summary>
        double VerletIntegration() {
            // The following sets the Centers of all nodes to a (not necessarily feasible) configuration that reduces the cost (forces)
            float energy0 = energy;
            energy = (float) ComputeDescentDirection(1.0);
            UpdateStepSize(energy0);
            SolveSeparationConstraints();

            double displacementSquared = 0;
            for (int i = 0; i < nodes.Length; ++i) {
                FiNode v = nodes[i];
                displacementSquared += (v.Center - v.previousCenter).LengthSquared;
            }
            return displacementSquared;
        }

        void SolveSeparationConstraints() {
            if (this.NeedSolve()) {
                // Increasing the padding effectively increases the size of the rectangle, so it will lead to more overlaps,
                // and therefore tighter packing once the overlap is removed and therefore more apparent "columnarity".
                // We don't want to drastically change the shape of the rectangles, just increase them ever so slightly so that
                // there is a bit more space in the horizontal than vertical direction, thus reducing the likelihood that
                // the vertical constraint generation will detect spurious overlaps, which should allow the nodes to slide
                // smoothly around each other.  ConGen padding args are:  First pad is in direction of the constraints being
                // generated, second pad is in the perpendicular direction.
                double dblVpad = settings.NodeSeparation;
                double dblHpad = dblVpad + Feasibility.Pad;
                double dblCVpad = settings.ClusterMargin;
                double dblCHpad = dblCVpad + Feasibility.Pad;

                // The centers are our desired positions, but we need to find a feasible configuration
                foreach (FiNode v in nodes) {
                    v.desiredPosition = v.Center;
                }
                // Set up horizontal non-overlap constraints based on the (feasible) starting configuration
                horizontalSolver.Initialize(dblHpad, dblVpad, dblCHpad, dblCVpad, v => v.previousCenter);
                horizontalSolver.SetDesiredPositions();
                horizontalSolver.Solve();

                // generate y constraints
                verticalSolver.Initialize(dblHpad, dblVpad, dblCHpad, dblCVpad, v => v.Center);
                verticalSolver.SetDesiredPositions();
                verticalSolver.Solve();

                // If we have multiple locks (hence multiple high-weight nodes), there can still be some
                // movement of the locked variables - so update all lock positions.
                foreach (LockPosition l in settings.locks.Where(l => !l.Sticky)) {
                    l.Bounds = l.node.BoundingBox;
                }
            }
        }

        double ComputeDescentDirection(double alpha) {
            ResetForceVectors();
            // velocity is the distance travelled last time step
            if (settings.ApplyForces) {
                ComputeForces();
            }
            double lEnergy = 0;
            foreach (FiNode v in nodes) {
                lEnergy += v.force.LengthSquared;
                Point dx = v.Center - v.previousCenter;
                v.previousCenter = v.Center;
                dx *= settings.Friction;
                Point a = -stepSize*alpha*v.force;
                Debug.Assert(!double.IsNaN(a.X), "!double.IsNaN(a.X)");
                Debug.Assert(!double.IsNaN(a.Y), "!double.IsNaN(a.Y)");
                Debug.Assert(!double.IsInfinity(a.X), "!double.IsInfinity(a.X)");
                Debug.Assert(!double.IsInfinity(a.Y), "!double.IsInfinity(a.Y)");
                dx += a;
                dx /= v.stayWeight;
                v.Center += dx;
            }
            SatisfyConstraints();
            return lEnergy;
        }

        void ResetForceVectors() {
            foreach (var v in nodes) {
                v.force = new Point();
            }
        }

        /// <summary>
        /// Adapt StepSize based on change in energy.  
        /// Five sequential improvements in energy mean we increase the stepsize.
        /// Any increase in energy means we reduce the stepsize.
        /// </summary>
        /// <param name="energy0"></param>
        void UpdateStepSize(float energy0) {
            if (energy < energy0) {
                if (++progress >= 3) {
                    progress = 0;
                    stepSize /= settings.Decay;
                }
            }
            else {
                progress = 0;
                stepSize *= settings.Decay;
            }
        }

        double RungeKuttaIntegration() {
            var y0 = new Point[nodes.Length];
            var k1 = new Point[nodes.Length];
            var k2 = new Point[nodes.Length];
            var k3 = new Point[nodes.Length];
            var k4 = new Point[nodes.Length];
            float energy0 = energy;
            SatisfyConstraints();
            for (int i = 0; i < nodes.Length; ++i) {
                y0[i] = nodes[i].previousCenter = nodes[i].Center;
            }
            const double alpha = 3;
            ComputeDescentDirection(alpha);
            for (int i = 0; i < nodes.Length; ++i) {
                k1[i] = nodes[i].Center - nodes[i].previousCenter;
                nodes[i].Center = y0[i] + 0.5*k1[i];
            }
            ComputeDescentDirection(alpha);
            for (int i = 0; i < nodes.Length; ++i) {
                k2[i] = nodes[i].Center - nodes[i].previousCenter;
                nodes[i].previousCenter = y0[i];
                nodes[i].Center = y0[i] + 0.5*k2[i];
            }
            ComputeDescentDirection(alpha);
            for (int i = 0; i < nodes.Length; ++i) {
                k3[i] = nodes[i].Center - nodes[i].previousCenter;
                nodes[i].previousCenter = y0[i];
                nodes[i].Center = y0[i] + k3[i];
            }
            energy = (float) ComputeDescentDirection(alpha);
            for (int i = 0; i < nodes.Length; ++i) {
                k4[i] = nodes[i].Center - nodes[i].previousCenter;
                nodes[i].previousCenter = y0[i];
                Point dx = (k1[i] + 2.0*k2[i] + 2.0*k3[i] + k4[i])/6.0;
                nodes[i].Center = y0[i] + dx;
            }
            UpdateStepSize(energy0);
            SolveSeparationConstraints();

            return this.nodes.Sum(v => (v.Center - v.previousCenter).LengthSquared);
        }

        /// <summary>
        /// Apply a small number of iterations of the layout.  
        /// The idea of incremental layout is that settings.minorIterations should be a small number (e.g. 3) and 
        /// CalculateLayout should be invoked in a loop, e.g.:
        /// 
        /// while(settings.RemainingIterations > 0) {
        ///    fastIncrementalLayout.CalculateLayout();
        ///    InvokeYourProcedureToRedrawTheGraphOrHandleInteractionEtc();
        /// }
        /// 
        /// In the verletIntegration step above, the RemainingIterations is used to control damping.
        /// </summary>
        protected override void RunInternal() {
            settings.Converged = false;
            settings.EdgeRoutesUpToDate = false;
            if (settings.Iterations++ == 0) {
                stepSize = settings.InitialStepSize;
                energy = float.MaxValue;
                progress = 0;
            }
            this.StartListenToLocalProgress(settings.MinorIterations);
            for (int i = 0; i < settings.MinorIterations; ++i) {                
                double d2 = settings.RungeKuttaIntegration ? RungeKuttaIntegration() : VerletIntegration();

                if (d2 < settings.DisplacementThreshold || settings.Iterations > settings.MaxIterations) {
                    settings.Converged = true;
                    this.ProgressComplete();
                    break;
                }

                ProgressStep();
            }
            FinalizeClusterBoundaries();
        }

        /// <summary>
        /// Simply does a depth first traversal of the cluster hierarchies fitting Rectangles to the contents of the cluster
        /// or updating the cluster BoundingBox to the already calculated RectangularBoundary
        /// </summary>
        void FinalizeClusterBoundaries() {
            foreach (var c in graph.RootCluster.AllClustersDepthFirst()) {
                if (c == graph.RootCluster) continue;

                if (!this.NeedSolve() && settings.UpdateClusterBoundariesFromChildren) {
                    // if we are not using the solver (e.g. when constraintLevel == 0) then we need to get the cluster bounds manually
                    c.CalculateBoundsFromChildren(this.settings.ClusterMargin);
                }
                else {
                    c.BoundingBox = c.RectangularBoundary.Rect;
                }
                c.RaiseLayoutDoneEvent();
            }
        }
    }
}
