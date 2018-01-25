using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Layout.Layered {
  /// <summary>
  /// Following "Improving Layered Graph Layouts with Edge Bundling" and
  /// "Two polynomial time algorithms for the bundle-Line crossing minimization problem"
  /// Postprocessing minimizing crossings step that works on the layered graph
  /// </summary>
  internal class MetroMapOrdering {
    LayerArrays layerArrays;
    Dictionary<int, Point> nodePositions;
    ProperLayeredGraph properLayeredGraph;

    MetroMapOrdering(ProperLayeredGraph properLayeredGraph, LayerArrays layerArrays,
                     Dictionary<int, Point> nodePositions) {
      this.properLayeredGraph = properLayeredGraph;
      this.layerArrays = layerArrays;
      this.nodePositions = nodePositions;
    }

    /// <summary>
    /// Reorder only points having identical nodePositions
    /// </summary>
    internal static void UpdateLayerArrays(ProperLayeredGraph properLayeredGraph, LayerArrays layerArrays,
                                           Dictionary<int, Point> nodePositions) {
      new MetroMapOrdering(properLayeredGraph, layerArrays, nodePositions).UpdateLayerArrays();
    }

    /// <summary>
    /// Reorder virtual nodes between the same pair of real nodes
    /// </summary>
    internal static void UpdateLayerArrays(ProperLayeredGraph properLayeredGraph, LayerArrays layerArrays) {
      Dictionary<int, Point> nodePositions = BuildInitialNodePositions(properLayeredGraph, layerArrays);
      UpdateLayerArrays(properLayeredGraph, layerArrays, nodePositions);
    }

    static Dictionary<int, Point> BuildInitialNodePositions(ProperLayeredGraph properLayeredGraph,
                                                            LayerArrays layerArrays) {
      var result = new Dictionary<int, Point>();
      for (int i = 0; i < layerArrays.Layers.Length; i++) {
        int prev = 0, curr = 0;
        while (curr < layerArrays.Layers[i].Length) {
          while (curr < layerArrays.Layers[i].Length &&
                 properLayeredGraph.IsVirtualNode(layerArrays.Layers[i][curr])) curr++;
          for (int j = prev; j < curr; j++)
            result[layerArrays.Layers[i][j]] = new Point(i, prev);

          if (curr < layerArrays.Layers[i].Length)
            result[layerArrays.Layers[i][curr]] = new Point(i, curr);
          curr++;
          prev = curr;
        }
      }

      return result;
    }

    void UpdateLayerArrays() {
#if TEST_MSAGL
            //    int initialCrossingNumber = Ordering.GetCrossingsTotal(properLayeredGraph, layerArrays);
#endif

      //algo stuff here
      Dictionary<Point, List<int>> ordering = CreateInitialOrdering();
      ordering = BuildOrdering(ordering);
      RestoreLayerArrays(ordering);

#if TEST_MSAGL
//            int finalCrossingNumber = Ordering.GetCrossingsTotal(properLayeredGraph, layerArrays);
//            double gain = (initialCrossingNumber > 0 ? (double)(initialCrossingNumber - finalCrossingNumber) / initialCrossingNumber * 100.0 : 0);
//            Console.WriteLine("Crossing number reduced on {0:0.00}% (initial:{1}, final:{2})", gain, initialCrossingNumber, finalCrossingNumber);
#endif
    }

    Dictionary<Point, List<int>> CreateInitialOrdering() {
      var initialOrdering = new Dictionary<Point, List<int>>();
      for (int i = 0; i < layerArrays.Layers.Length; i++)
        for (int j = 0; j < layerArrays.Layers[i].Length; j++) {
          int node = layerArrays.Layers[i][j];
          if (!initialOrdering.ContainsKey(nodePositions[node]))
            initialOrdering[nodePositions[node]] = new List<int>();
          initialOrdering[nodePositions[node]].Add(node);
        }

      return initialOrdering;
    }


    Dictionary<Point, List<int>> BuildOrdering(Dictionary<Point, List<int>> initialOrdering) {
      //run through nodes points and build order
      var result = new Dictionary<Point, List<int>>();
      var reverseOrder = new Dictionary<int, int>();
      for (int i = 0; i < layerArrays.Layers.Length; i++)
        for (int j = 0; j < layerArrays.Layers[i].Length; j++) {
          int node = layerArrays.Layers[i][j];

          //already processed
          if (result.ContainsKey(nodePositions[node])) continue;

          result[nodePositions[node]] = BuildNodeOrdering(initialOrdering[nodePositions[node]], reverseOrder);
        }

      return result;
    }

    List<int> BuildNodeOrdering(List<int> nodeOrdering, Dictionary<int, int> inverseToOrder) {
      List<int> result = nodeOrdering;

      result.Sort(Comparison(inverseToOrder));

      for (int i = 0; i < result.Count; i++)
        inverseToOrder[result[i]] = i;
      return result;
    }

    Comparison<int> Comparison(Dictionary<int, int> inverseToOrder) {
      return delegate (int node1, int node2) {
        Debug.Assert(properLayeredGraph.IsVirtualNode(node1) &&
                     properLayeredGraph.IsVirtualNode(node2));

        int succ1 = properLayeredGraph.Succ(node1).ElementAt(0);
        int succ2 = properLayeredGraph.Succ(node2).ElementAt(0);
        int pred1 = properLayeredGraph.Pred(node1).ElementAt(0);
        int pred2 = properLayeredGraph.Pred(node2).ElementAt(0);

        Point succPoint1 = nodePositions[succ1];
        Point succPoint2 = nodePositions[succ2];
        Point predPoint1 = nodePositions[pred1];
        Point predPoint2 = nodePositions[pred2];

        if (succPoint1 != succPoint2) {
          if (predPoint1 != predPoint2)
            return predPoint1.CompareTo(predPoint2);
          return succPoint1.CompareTo(succPoint2);
        }
        if (properLayeredGraph.IsVirtualNode(succ1)) {
          if (predPoint1 != predPoint2)
            return predPoint1.CompareTo(predPoint2);

          int o1 = inverseToOrder[succ1];
          int o2 = inverseToOrder[succ2];
          Debug.Assert(o1 != -1 && o2 != -1);
          return (o1.CompareTo(o2));
        }
        while (nodePositions[pred1] == nodePositions[pred2] &&
               properLayeredGraph.IsVirtualNode(pred1)) {
          pred1 = properLayeredGraph.Pred(pred1).ElementAt(0);
          pred2 = properLayeredGraph.Pred(pred2).ElementAt(0);
        }

        if (nodePositions[pred1] == nodePositions[pred2])
          return node1.CompareTo(node2);
        return nodePositions[pred1].CompareTo(nodePositions[pred2]);
      };
    }

    void RestoreLayerArrays(Dictionary<Point, List<int>> ordering) {
      for (int i = 0; i < layerArrays.Layers.Length; i++) {
        int pred = 0, tec = 0;
        while (tec < layerArrays.Layers[i].Length) {
          while (tec < layerArrays.Layers[i].Length &&
                 nodePositions[layerArrays.Layers[i][pred]] == nodePositions[layerArrays.Layers[i][tec]])
            tec++;
          for (int j = pred; j < tec; j++)
            layerArrays.Layers[i][j] = ordering[nodePositions[layerArrays.Layers[i][j]]][j - pred];
          pred = tec;
        }
      }

      layerArrays.UpdateXFromLayers();
    }
  }
}