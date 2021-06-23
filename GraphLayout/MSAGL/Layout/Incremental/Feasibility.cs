using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Layout.Incremental
{
    static internal class Feasibility
    {
        /// <summary>
        /// Very small extra padding used for VPad to ensure feasibility
        /// </summary>
        public const double Pad = 1e-4;

        /// <summary>
        /// Obtain a starting configuration that is feasible with respect to the structural 
        /// constraints.  This is necessary to avoid e.g. cycles in the constraint graph;
        /// for example, dragging the root of a downward-pointing tree downward below other
        /// nodes of the tree can result in auto-generation of constraints generating some
        /// constraints with the root on the right-hand side, and the structural constraints
        /// have it on the left-hand side.
        /// 
        /// When AvoidOverlaps==true and we reach ConstraintLevel>=2 then we also need to remove
        /// overlaps... prior to this we need to force horizontal resolving of overlaps 
        /// between *all* nodes involved in vertical equality constraints (i.e. no skipping), 
        /// and then vertical overlap resolution of all nodes involved in horizontal equality
        /// constraints
        /// </summary>
        static internal void Enforce(FastIncrementalLayoutSettings settings, int currentConstraintLevel, IEnumerable<FiNode> nodes, List<IConstraint> horizontalConstraints, List<IConstraint> verticalConstraints, IEnumerable<Cluster> clusterHierarchies, Func<Cluster, LayoutAlgorithmSettings> clusterSettings)
        {
            foreach (LockPosition l in settings.locks)
            {
                l.Project();
            }
            ResetPositions(nodes);
            double dblVpad = settings.NodeSeparation + Pad;
            double dblHpad = settings.NodeSeparation;
            double dblCVpad = settings.ClusterMargin + Pad;
            double dblCHpad = settings.ClusterMargin;
            for (int level = settings.MinConstraintLevel; level <= currentConstraintLevel; ++level)
            {
                // to obtain a feasible solution when equality constraints are present we need to be extra careful
                // but the solution below is a little bit crummy, is not currently optimized when there are no
                // equality constraints and we do not really have any scenarios involving equality constraints at
                // the moment, and also the fact that it turns off DeferToVertical causes it to resolve too
                // many overlaps horizontally, so let's skip it for now.
                var hsSolver = new AxisSolver(true, nodes, clusterHierarchies,
                                             level >= 2 && settings.AvoidOverlaps, level, clusterSettings);
                hsSolver.structuralConstraints = horizontalConstraints;
                hsSolver.OverlapRemovalParameters = new Core.Geometry.OverlapRemovalParameters
                {
                    AllowDeferToVertical = true,
                    ConsiderProportionalOverlap = settings.IdealEdgeLength.Direction != Core.Geometry.Direction.None
                };
                hsSolver.Initialize(dblHpad, dblVpad, dblCHpad, dblCVpad, v => v.Center);
                hsSolver.SetDesiredPositions();
                hsSolver.Solve();
                ResetPositions(nodes);
                var vsSolver = new AxisSolver(false, nodes, clusterHierarchies,
                                             level >= 2 && settings.AvoidOverlaps, level, clusterSettings);
                vsSolver.structuralConstraints = verticalConstraints;
                vsSolver.Initialize(dblHpad, dblVpad, dblCHpad, dblCVpad, v => v.Center);
                vsSolver.SetDesiredPositions();
                vsSolver.Solve();
                ResetPositions(nodes);
            }
        }

        ///// <summary>
        ///// When AvoidOverlaps==true and we reach ConstraintLevel>=2 then we also need to remove
        ///// overlaps... prior to this we need to force horizontal resolving of overlaps 
        ///// between *all* nodes involved in vertical equality constraints (i.e. no skipping), 
        ///// and then vertical overlap resolution of all nodes involved in horizontal equality
        ///// constraints
        ///// </summary>
        ///// <param name="dblVpad"></param>
        ///// <param name="dblHpad"></param>
        ///// <param name="horizontalConstraints"></param>
        ///// <param name="verticalConstraints"></param>
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        //static private void RemoveOverlapsOnEqualityConstraints(double dblVpad, double dblHpad, List<IConstraint> horizontalConstraints, List<IConstraint> verticalConstraints)
        //{
        //    var verticalEqualityConstraints = from c in verticalConstraints
        //                                      let sc = c as VerticalSeparationConstraint
        //                                      where sc != null && sc.IsEquality
        //                                      select new { sc.BottomNode, sc.TopNode };
        //    var vvs = (from c in verticalEqualityConstraints
        //               select (FiNode)c.BottomNode.AlgorithmData).Union(
        //               from c in verticalEqualityConstraints
        //               select (FiNode)c.TopNode.AlgorithmData).AsEnumerable();
        //    var hSolver = new AxisSolver(true, vvs, null, true, 2);
        //    hSolver.OverlapRemovalParameters = new Core.Geometry.OverlapRemovalParameters
        //    {
        //        AllowDeferToVertical = false
        //    };
        //    hSolver.Initialize(dblHpad + Pad, dblVpad + Pad, 0, v=>v.Center);
        //    hSolver.SetDesiredPositions();
        //    hSolver.Solve();
        //    var horizontalEqualityConstraints = from c in horizontalConstraints
        //                                        let sc = c as HorizontalSeparationConstraint
        //                                        where sc != null && sc.IsEquality
        //                                        select new { sc.LeftNode, sc.RightNode };
        //    var hvs = (from c in horizontalEqualityConstraints
        //               select (FiNode)c.LeftNode.AlgorithmData).Union(
        //               from c in horizontalEqualityConstraints
        //               select (FiNode)c.RightNode.AlgorithmData).AsEnumerable();
        //    var vSolver = new AxisSolver(false, hvs, null, true, 2);
        //    vSolver.Initialize(dblHpad + Pad, dblVpad + Pad, 0, v=>v.Center);
        //    vSolver.SetDesiredPositions();
        //    vSolver.Solve();
        //}

        private static void ResetPositions(IEnumerable<FiNode> nodes)
        {
            foreach (FiNode v in nodes)
            {
                v.previousCenter = v.desiredPosition = v.mNode.Center;
            }
        }
    }
}
