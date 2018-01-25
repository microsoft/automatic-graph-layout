using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Routing.Visibility;

namespace Microsoft.Msagl.Routing.Rectilinear {
  internal class MsmtRectilinearPath {
    private readonly double bendPenaltyAsAPercentageOfDistance = SsstRectilinearPath.DefaultBendPenaltyAsAPercentageOfDistance;

    // Waypoints define path stages: source -> wp1, wp1 -> wp2 ... wpn -> target.
    class WaypointEntry {
      internal readonly VisibilityVertex[] waypointVector = new VisibilityVertex[1];
      internal readonly VertexEntry[] entryVector = new VertexEntry[4];

      internal WaypointEntry(VisibilityVertex waypoint) {
        this.waypointVector[0] = waypoint;
      }
    }

    // Temporary for accumulating target entries.
    private readonly VertexEntry[] currentPassTargetEntries = new VertexEntry[4];

    public MsmtRectilinearPath(double bendPenalty) {
      this.bendPenaltyAsAPercentageOfDistance = bendPenalty;
    }

    /// <summary>
    /// Get the lowest-cost path from one of one or more sources to one of one or more targets, without waypoints.
    /// </summary>
    /// <param name="sources">One or more source vertices</param>
    /// <param name="targets">One or more target vertices</param>
    /// <returns>A single enumeration of path points.</returns>
    internal IEnumerable<Point> GetPath(IEnumerable<VisibilityVertex> sources, IEnumerable<VisibilityVertex> targets) {
      return SsstRectilinearPath.RestorePath(GetPathStage(null, sources, null, targets));
    }

    /// <summary>
    /// Get the lowest-cost path from one of one or more sources to one of one or more targets, with waypoints.
    /// </summary>
    /// <param name="sources">One or more source vertices</param>
    /// <param name="waypoints">One or more waypoint vertices</param>
    /// <param name="targets">One or more target vertices</param>
    /// <returns>An enumeration of each stage's enumeration of path points</returns>
    internal IEnumerable<IEnumerable<Point>> GetPath(IEnumerable<VisibilityVertex> sources, IEnumerable<VisibilityVertex> waypoints,
                                        IEnumerable<VisibilityVertex> targets) {
      var waypointEntries = new List<WaypointEntry>();

      var waypointEnum = waypoints.GetEnumerator();
      waypointEnum.MoveNext();
      var sourceWaypointEntry = new WaypointEntry(waypointEnum.Current);
      waypointEntries.Add(sourceWaypointEntry);

      VertexEntry lastEntry;
      if (!GetFirstPathStage(sourceWaypointEntry, sources)
              || !GetWaypointPathStages(waypointEnum, waypointEntries, ref sourceWaypointEntry)
              || !GetLastPathStage(sourceWaypointEntry, targets, out lastEntry)) {
        return null;
      }
      return RestorePathStages(waypointEntries, lastEntry);
    }

    private bool GetFirstPathStage(WaypointEntry sourceWaypointEntry, IEnumerable<VisibilityVertex> sources) {
      // Get the first stage: no prior entries to the sources; this target waypoint becomes the first waypoint stage's source.
      return null != this.GetPathStage(null, sources, sourceWaypointEntry.entryVector, sourceWaypointEntry.waypointVector);
    }

    private bool GetWaypointPathStages(IEnumerator<VisibilityVertex> waypointEnum, List<WaypointEntry> waypointEntries, ref WaypointEntry sourceWaypointEntry) {
      // Get the path stages between each waypoint: send the prior entries to source and get the entries to target.
      while (waypointEnum.MoveNext()) {
        var targetWaypointEntry = new WaypointEntry(waypointEnum.Current);
        waypointEntries.Add(targetWaypointEntry);

        if (null == this.GetPathStage(sourceWaypointEntry.entryVector, sourceWaypointEntry.waypointVector,
                targetWaypointEntry.entryVector, targetWaypointEntry.waypointVector)) {
          return false;
        }
        sourceWaypointEntry = targetWaypointEntry;
      }
      return true;
    }

    private bool GetLastPathStage(WaypointEntry sourceWaypointEntry, IEnumerable<VisibilityVertex> targets, out VertexEntry lastEntry) {
      // Final stage: no need to get separate entries to the targets.  And because we send null for the
      // target entry vector, this will return the final vertexEntry of the path.
      lastEntry = this.GetPathStage(sourceWaypointEntry.entryVector, sourceWaypointEntry.waypointVector, null, targets);
      return lastEntry != null;
    }

    private static IEnumerable<IEnumerable<Point>> RestorePathStages(List<WaypointEntry> waypointEntries, VertexEntry lastEntry) {
      // Back up to restore the path stages back to each waypoint.
      var paths = new List<IEnumerable<Point>>();
      waypointEntries.Reverse();
      foreach (var waypointEntry in waypointEntries) {
        paths.Add(SsstRectilinearPath.RestorePath(ref lastEntry, waypointEntry.waypointVector[0]));
      }

      // Add the first stage.
      paths.Add(SsstRectilinearPath.RestorePath(lastEntry));
      paths.Reverse();
      return paths;
    }

    /// <summary>
    /// Route a single stage of a possibly multi-stage (due to waypoints) path.
    /// </summary>
    /// <param name="sourceVertexEntries">The VertexEntry array that was in the source vertex if it was the target of a prior stage.</param>
    /// <param name="sources">The enumeration of source vertices; must be only one if sourceVertexEntries is non-null.</param>
    /// <param name="targets">The enumeration of target vertex entries; must be only one if targetVertexEntries is non-null.</param>
    /// <param name="targetVertexEntries">The VertexEntry array that is in the target at the end of the stage.</param>
    private VertexEntry GetPathStage(VertexEntry[] sourceVertexEntries, IEnumerable<VisibilityVertex> sources,
                                            VertexEntry[] targetVertexEntries, IEnumerable<VisibilityVertex> targets) {
      var ssstCalculator = new SsstRectilinearPath();
      VertexEntry bestEntry = null;

      // This contains the best (lowest) path cost after normalizing origins to the center of the sources
      // and targets.  This is used to avoid selecting a vertex pair whose path has more bends than another pair of
      // vertices, but the bend penalty didn't total enough to offset the additional length between the "better" pair.
      // This also plays the role of an upper bound on the path length; if a path cost is greater than adjustedMinCost 
      // then we stop exploring it, which saves considerable time after low-cost paths have been found.
      double bestCost = double.MaxValue / ScanSegment.OverlappedWeight;
      double bestPathCostRatio = double.PositiveInfinity;

      // Calculate the bend penalty multiplier.  This is a percentage of the distance between the source and target,
      // so that we have the same relative importance if we have objects of about size 20 that are about 100 apart
      // as for objects of about size 200 that are about 1000 apart.
      Point sourceCenter = GetBarycenterOfUniquePortLocations(sources);
      Point targetCenter = GetBarycenterOfUniquePortLocations(targets);
      var distance = SsstRectilinearPath.ManhattanDistance(sourceCenter, targetCenter);
      ssstCalculator.BendsImportance = Math.Max(0.001, distance * (this.bendPenaltyAsAPercentageOfDistance * 0.01));

      // We'll normalize by adding (a proportion of) the distance (only; not bends) from the current endpoints to
      // their centers. This is similar to routeToCenter, but routing multiple paths like this means we'll always
      // get at least a tie for the best vertex pair, whereas routeToCenter can introduce extraneous bends
      // if the sources/targets are not collinear with the center (such as an E-R diagram).
      // interiorLengthAdjustment is a way to decrease the cost adjustment slightly to allow a bend if it saves moving
      // a certain proportion of the distance parallel to the object before turning to it.
      var interiorLengthAdjustment = ssstCalculator.LengthImportance;

      // VertexEntries for the current pass of the current stage, if multistage.
      var tempTargetEntries = (targetVertexEntries != null) ? this.currentPassTargetEntries : null;

      // Process closest pairs first, so we can skip longer ones (jump out of SsstRectilinear sooner, often immediately).
      // This means that we'll be consistent on tiebreaking for equal scores with differing bend counts (the shorter
      // path will win).  In overlapped graphs the shortest path may have more higher-weight edges. 
      foreach (var pair in
              from VisibilityVertexRectilinear source in sources
              from VisibilityVertexRectilinear target in targets
              orderby SsstRectilinearPath.ManhattanDistance(source.Point, target.Point)
              select new { sourceV = source, targetV = target }) {
        var source = pair.sourceV;
        var target = pair.targetV;
        if (PointComparer.Equal(source.Point, target.Point)) {
          continue;
        }
        var sourceCostAdjustment = SsstRectilinearPath.ManhattanDistance(source.Point, sourceCenter) * interiorLengthAdjustment;
        var targetCostAdjustment = SsstRectilinearPath.ManhattanDistance(target.Point, targetCenter) * interiorLengthAdjustment;

        var adjustedBestCost = bestCost;
        if (targetVertexEntries != null) {
          Array.Clear(tempTargetEntries, 0, tempTargetEntries.Length);
          adjustedBestCost = ssstCalculator.MultistageAdjustedCostBound(bestCost);
        }
        VertexEntry lastEntry = ssstCalculator.GetPathWithCost(sourceVertexEntries, source, sourceCostAdjustment,
                                                                tempTargetEntries, target, targetCostAdjustment,
                                                                adjustedBestCost);
        if (tempTargetEntries != null) {
          UpdateTargetEntriesForEachDirection(targetVertexEntries, tempTargetEntries, ref bestCost, ref bestEntry);
          continue;
        }

        // This is the final (or only) stage. Break ties by picking the lowest ratio of cost to ManhattanDistance between the endpoints.
        if (lastEntry == null) {
          continue;
        }
        var costRatio = lastEntry.Cost / SsstRectilinearPath.ManhattanDistance(source.Point, target.Point);
        if ((lastEntry.Cost < bestCost) || ApproximateComparer.Close(lastEntry.Cost, bestCost) && (costRatio < bestPathCostRatio)) {
          bestCost = lastEntry.Cost;
          bestEntry = lastEntry;
          bestPathCostRatio = lastEntry.Cost / SsstRectilinearPath.ManhattanDistance(source.Point, target.Point);
        }
      }
      return bestEntry;
    }

    private static void UpdateTargetEntriesForEachDirection(VertexEntry[] targetVertexEntries, VertexEntry[] tempTargetEntries,
                        ref double bestCost, ref VertexEntry bestEntry) {
      for (int ii = 0; ii < tempTargetEntries.Length; ++ii) {
        var tempEntry = tempTargetEntries[ii];
        if (tempEntry == null) {
          continue;
        }
        if ((targetVertexEntries[ii] == null) || (tempEntry.Cost < targetVertexEntries[ii].Cost)) {
          targetVertexEntries[ii] = tempEntry;
          if (tempEntry.Cost < bestCost) {
            // This does not have the ratio tiebreaker because the individual stage path is only used as a success indicator.
            bestCost = tempEntry.Cost;
            bestEntry = tempEntry;
          }
        }
      }
      return;
    }

    private static Point GetBarycenterOfUniquePortLocations(IEnumerable<VisibilityVertex> vertices) {
      var center = new Point();
      VisibilityVertex prevVertex = null;
      int count = 0;
      foreach (var vertex in vertices.OrderBy(s => s.Point)) {
        if ((prevVertex != null) && ApproximateComparer.CloseIntersections(vertex.Point, prevVertex.Point)) {
          continue;
        }
        prevVertex = vertex;
        ++count;
        center += vertex.Point;
      }
      return center / count;
    }
  }
}