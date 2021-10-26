// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlappingGroupANdClumpMap.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Routing.Rectilinear;
namespace Microsoft.Msagl.UnitTests.Rectilinear
{
    
    /// <summary>
    /// Rectangular obstacles and clumps do not aggregate on the production side - groups are separate
    /// from clumps - but they may overlap to form a roadblock.
    /// </summary>
    internal class SuperClumpMap
    {
        private readonly ObstacleTree obstacleTree;
        private readonly Dictionary<Obstacle, SuperClump> mapObstacleToSuperClump = new Dictionary<Obstacle, SuperClump>();

        internal SuperClumpMap(ObstacleTree obstacleTree)
        {
            this.obstacleTree = obstacleTree;
        }

        private void MakeClumps()
        {
            // Accumulate overlapping rectangular groups and obstacles.
            foreach (var group in obstacleTree.GetAllGroups().Where(group => group.IsRectangle))
            {
                var groupClump = new SuperClump();
                groupClump.Insert(group);
                this.mapObstacleToSuperClump[group] = groupClump;

                var localGroup = group;
                foreach (var candidate in obstacleTree.Root.AllHitItems(
                                        group.PaddedBoundingBox,
                                        cand => cand.IsRectangle && ObstaclesOverlap(localGroup, cand)))
                {
                    SuperClump candClump;
                    if (this.mapObstacleToSuperClump.TryGetValue(candidate, out candClump) && (candClump != groupClump)) {
                        groupClump = JoinSuperClumps(groupClump, group, candClump);
                        continue;
                    }
                    AddObstacleToSuperClump(groupClump, candidate);
                    if (candidate.Clump != null) 
                    {
                        foreach (var sibling in candidate.Clump)
                        {
                            AddObstacleToSuperClump(groupClump, sibling);
                        }
                    }
                }
            }

            foreach (var superClump in this.mapObstacleToSuperClump.Values) 
            {
                superClump.CalculateHierarchy();
            }
        }

        private void AddObstacleToSuperClump(SuperClump groupClump, Obstacle candidate) {
            groupClump.Insert(candidate);
            this.mapObstacleToSuperClump[candidate] = groupClump;
        }

        private SuperClump JoinSuperClumps(SuperClump obsClump, Obstacle group, SuperClump candClump) {
            foreach (var clumpee in obsClump.Obstacles)
            {
                candClump.Insert(clumpee);
                this.mapObstacleToSuperClump[clumpee] = candClump;
            }

            obsClump = candClump;
            this.mapObstacleToSuperClump[group] = obsClump;
            return obsClump;
        }

        private static bool ObstaclesOverlap(Obstacle group, Obstacle obstacle)
        {
            // Touching is enough to allow this to pass as "overlapped" for our purposes.  We might pick
            // up some that touch from the inside, but it won't cause any difference in results.
            return (group == obstacle) ? false : (Curve.CurvesIntersect(group.VisibilityPolyline, obstacle.VisibilityPolyline));
        }

        internal SuperClump FindClump(Obstacle obstacleToFind)
        {
            if (0 == this.mapObstacleToSuperClump.Count)
            {
                MakeClumps();
            }
            SuperClump superClump;
            this.mapObstacleToSuperClump.TryGetValue(obstacleToFind, out superClump);
            return superClump;
        }
    }
}
