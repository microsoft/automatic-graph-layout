using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;

namespace Microsoft.Msagl.Routing.Spline.Bundling {
    /// <summary>
    /// Linear algorithm as described in our paper
    /// Edge Routing with Ordered Bunldles
    /// </summary>
    public class LinearMetroMapOrdering : IMetroMapOrderingAlgorithm {
        /// <summary>
        /// bundle lines
        /// </summary>
        readonly List<Metroline> MetrolinesGlobal;

        List<int[]> Metrolines;

        /// <summary>
        /// Station positions
        /// </summary>
        Point[] positions;

        /// <summary>
        /// Initialize bundle graph and build the ordering
        /// </summary>
        internal LinearMetroMapOrdering(List<Metroline> MetrolinesGlobal, Dictionary<Point, Station> pointToIndex) {
            this.MetrolinesGlobal = MetrolinesGlobal;

            ConvertParameters(pointToIndex);

            BuildOrder();
        }

        /// <summary>
        /// Get the ordering of lines on station u with respect to the edge (u->v)
        /// </summary>
        IEnumerable<Metroline> IMetroMapOrderingAlgorithm.GetOrder(Station u, Station v) {
            MetroEdge me = MetroEdge.CreateFromTwoNodes(u.SerialNumber, v.SerialNumber);
            List<int> orderedMetrolineListForUv = order[me];
            if (u.SerialNumber < v.SerialNumber) {
                foreach (int MetrolineIndex in orderedMetrolineListForUv)
                    yield return MetrolinesGlobal[MetrolineIndex];
            }
            else {
                for (int i = orderedMetrolineListForUv.Count - 1; i >= 0; i--)
                    yield return MetrolinesGlobal[orderedMetrolineListForUv[i]];
            }
        }

        /// <summary>
        /// Get the index of line on the edge (u->v) and node u
        /// </summary>
        int IMetroMapOrderingAlgorithm.GetLineIndexInOrder(Station u, Station v, Metroline Metroline) {
            MetroEdge me = MetroEdge.CreateFromTwoNodes(u.SerialNumber, v.SerialNumber);
            Dictionary<Metroline, int> d = lineIndexInOrder[me];
            if (u.SerialNumber < v.SerialNumber) {
                return d[Metroline];
            }
            else {
                return d.Count - 1 - d[Metroline];
            }
        }

        void ConvertParameters(Dictionary<Point, Station> pointToIndex) {
            Metrolines = new List<int[]>();
            positions = new Point[pointToIndex.Count];
            foreach (Metroline gline in MetrolinesGlobal) {
                List<int> line = new List<int>();
                foreach (Point p in gline.Polyline) {
                    line.Add(pointToIndex[p].SerialNumber);
                    positions[pointToIndex[p].SerialNumber] = p;
                }

                Metrolines.Add(line.ToArray());
            }
        }

        //order for node u of edge u->v
        Dictionary<MetroEdge, List<int>> order;
        Dictionary<MetroEdge, Dictionary<Metroline, int>> lineIndexInOrder;

        HashSet<int> nonTerminals;
        HashSet<MetroEdge> initialEdges;

        /// <summary>
        /// Edge in graph H
        /// label is used to distinguish multiple edges
        /// </summary>
        class MetroEdge {
            List<int> nodes;

            internal static MetroEdge CreateFromTwoNodes(int u, int v) {
                MetroEdge res = new MetroEdge();
                res.nodes = new List<int>();
                res.nodes.Add(Math.Min(u, v));
                res.nodes.Add(Math.Max(u, v));

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289
                res.UpdateHashKey();
#endif

                return res;
            }

            internal static MetroEdge CreateFromTwoEdges(int v, MetroEdge e1, MetroEdge e2) {
                int s = e1.Source() == v ? e1.Target() : e1.Source();
                int t = e2.Source() == v ? e2.Target() : e2.Source();

                if (s < t)
                    return CreateFromTwoEdges(v, e1.nodes, e2.nodes);
                else
                    return CreateFromTwoEdges(v, e2.nodes, e1.nodes);
            }

            internal static MetroEdge CreateFromTwoEdges(int v, List<int> e1, List<int> e2) {
                List<int> nodes = new List<int>(e1.Count + e2.Count - 1);
                if (e1[0] != v) {
                    for (int i = 0; i < e1.Count; i++)
                        nodes.Add(e1[i]);
                }
                else {
                    for (int i = e1.Count - 1; i >= 0; i--)
                        nodes.Add(e1[i]);
                }

                if (e2[0] == v) {
                    for (int i = 1; i < e2.Count; i++)
                        nodes.Add(e2[i]);
                }
                else {
                    for (int i = e2.Count - 2; i >= 0; i--)
                        nodes.Add(e2[i]);
                }

                MetroEdge res = new MetroEdge();
                res.nodes = nodes;
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289
                res.UpdateHashKey();
#endif
                return res;
            }

            internal int Source() {
                return nodes[0];
            }

            internal int Target() {
                return nodes[nodes.Count - 1];
            }

            public override string ToString() {
                string s = "(";
                foreach (int i in nodes)
                    s += i + " ";
                s += ")";
                return s;
            }

            int label;
            bool labelCached = false;

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=289 Support Dictionary directly based on object's GetHashCode
            private SharpKit.JavaScript.JsString _hashKey;
            private void UpdateHashKey()
            {
                _hashKey = GetHashCode().ToString();
            }
#endif

            public override int GetHashCode()
            {
                if (!labelCached) {
                    ulong hc = (ulong)nodes.Count;
                    for (int i = 0; i < nodes.Count; i++) {
                        hc = unchecked(hc * 314159 + (ulong)nodes[i]);
                    }

                    label = (int)hc;
                    labelCached = true;
                }

                return label;
            }
            /// <summary>
            /// overrides the equality
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj) {
                if (!(obj is MetroEdge))
                    return false;
                return (MetroEdge)obj == this;

            }

            public static bool operator ==(MetroEdge pair0, MetroEdge pair1) {
                if (pair0.GetHashCode() != pair1.GetHashCode()) return false;
                return true;
                //TODO: are conflicts possible?
                //return pair0.nodes.SequenceEqual(pair1.nodes);
            }

            public static bool operator !=(MetroEdge pair0, MetroEdge pair1) {
                return !(pair0 == pair1);
            }

        }

        /// <summary>
        /// unordered list of paths on a specified edge
        /// </summary>
        class PathList {
            internal MetroEdge edge;
            internal HashSet<PathOnEdge> paths;
            internal List<PathList> subLists;

            public override string ToString() {
                return edge.ToString() + " (" + paths.Count + ")";
            }
        }

        class PathOnEdge {
            internal int index;
            internal LinkedListNode<MetroEdge> node;

            public override string ToString() {
                string s = "(index = " + index + ")";
                return s;
            }
        }

        Dictionary<int, LinkedList<MetroEdge>> orderedAdjacent;
        Dictionary<Tuple<int, MetroEdge>, LinkedListNode<MetroEdge>> adjacencyIndex;

        Dictionary<MetroEdge, PathList> e2p;
        Dictionary<int, LinkedList<MetroEdge>> paths;

        /// <summary>
        /// Do the main job
        /// </summary>
        void BuildOrder() {
            //init local structures
            Initialize();

            //ordering itself
            foreach (int v in nonTerminals) {
                ProcessNonTerminal(v);
            }

            //get result
            RestoreResult();
        }

        void Initialize() {
            //non terminals and adjacent
            nonTerminals = new HashSet<int>();
            initialEdges = new HashSet<MetroEdge>();
            //non-sorted adjacent edges. will be sorted later
            Dictionary<int, HashSet<MetroEdge>> adjacent = new Dictionary<int, HashSet<MetroEdge>>();
            for (int mi = 0; mi < Metrolines.Count; mi++) {
                int[] Metroline = Metrolines[mi];
                for (int i = 0; i + 1 < Metroline.Length; i++) {
                    MetroEdge me = MetroEdge.CreateFromTwoNodes(Metroline[i], Metroline[i + 1]);

                    if (!initialEdges.Contains(me))
                        initialEdges.Add(me);

                    if (i + 2 < Metroline.Length)
                        nonTerminals.Add(Metroline[i + 1]);

                    CollectionUtilities.AddToMap(adjacent, Metroline[i], me);
                    CollectionUtilities.AddToMap(adjacent, Metroline[i + 1], me);
                }
            }

            //order neighbors around each vertex
            InitAdjacencyData(adjacent);

            //create e2p and paths...
            InitPathData();
        }

        void InitPathData() {
            paths = new Dictionary<int, LinkedList<MetroEdge>>();
            e2p = new Dictionary<MetroEdge, PathList>();
            for (int mi = 0; mi < Metrolines.Count; mi++) {
                int[] Metroline = Metrolines[mi];
                paths.Add(mi, new LinkedList<MetroEdge>());

                for (int i = 0; i + 1 < Metroline.Length; i++) {
                    MetroEdge me = MetroEdge.CreateFromTwoNodes(Metroline[i], Metroline[i + 1]);

                    if (!e2p.ContainsKey(me)) {
                        PathList pl = new PathList();
                        pl.edge = me;
                        pl.paths = new HashSet<PathOnEdge>();
                        e2p.Add(me, pl);
                    }

                    PathOnEdge pathOnEdge = new PathOnEdge();
                    pathOnEdge.index = mi;
                    pathOnEdge.node = paths[mi].AddLast(me);
                    e2p[me].paths.Add(pathOnEdge);
                }
            }
        }

        void InitAdjacencyData(Dictionary<int, HashSet<MetroEdge>> adjacent) {
            orderedAdjacent = new Dictionary<int, LinkedList<MetroEdge>>();
            adjacencyIndex = new Dictionary<Tuple<int, MetroEdge>, LinkedListNode<MetroEdge>>();
            foreach (int v in adjacent.Keys) {
                List<MetroEdge> adj = new List<MetroEdge>(adjacent[v]);
                orderedAdjacent.Add(v, SortAdjacentEdges(v, adj));
            }
        }

        LinkedList<MetroEdge> SortAdjacentEdges(int v, List<MetroEdge> adjacent) {
            MetroEdge mn = adjacent.First();
            int mnv = OppositeNode(mn, v);
            adjacent.Sort(delegate(MetroEdge edge1, MetroEdge edge2) {
                int a = OppositeNode(edge1, v);
                int b = OppositeNode(edge2, v);

                //TODO: remove angles!
                double angA = Point.Angle(positions[a] - positions[v], positions[mnv] - positions[v]);
                double angB = Point.Angle(positions[b] - positions[v], positions[mnv] - positions[v]);

                return angA.CompareTo(angB);
            });

            LinkedList<MetroEdge> res = new LinkedList<MetroEdge>();
            foreach (MetroEdge edge in adjacent) {
                LinkedListNode<MetroEdge> node = res.AddLast(edge);
                adjacencyIndex.Add(new Tuple<int, MetroEdge>(v, edge), node);
            }
            return res;
        }

        /// <summary>
        /// update adjacencies of node 'a': put new edges instead of oldEdge
        /// </summary>
        void UpdateAdjacencyData(int a, MetroEdge oldEdge, List<PathList> newSubList) {
            //find a (cached) position of oldEdge in order
            LinkedListNode<MetroEdge> node = adjacencyIndex[new Tuple<int, MetroEdge>(a, oldEdge)];
            Debug.Assert(node.Value == oldEdge);

            LinkedListNode<MetroEdge> inode = node;
            foreach (PathList pl in newSubList) {
                MetroEdge newEdge = pl.edge;

                if (oldEdge.Source() == a)
                    node = node.List.AddAfter(node, newEdge);
                else
                    node = node.List.AddBefore(node, newEdge);

                adjacencyIndex.Add(new Tuple<int, MetroEdge>(a, newEdge), node);
            }

            adjacencyIndex.Remove(new Tuple<int, MetroEdge>(a, oldEdge));
            inode.List.Remove(inode);
        }

        /// <summary>
        /// recursively build an order on the edge
        /// </summary>
        List<int> RestoreResult(MetroEdge edge) {
            List<int> res = new List<int>();

            PathList pl = e2p[edge];
            if (pl.subLists == null) {
                foreach (PathOnEdge path in pl.paths)
                    res.Add(path.index);
            }
            else {
                foreach (PathList subList in pl.subLists) {
                    List<int> subResult = RestoreResult(subList.edge);
                    if (!(edge.Source() == subList.edge.Source() || edge.Target() == subList.edge.Target()))
                        subResult.Reverse();
                    res.AddRange(subResult);
                }
            }
            return res;
        }

        void RestoreResult() {
            order = new Dictionary<MetroEdge, List<int>>();
            lineIndexInOrder = new Dictionary<MetroEdge, Dictionary<Metroline, int>>();
            foreach (MetroEdge me in initialEdges) {
                order.Add(me, RestoreResult(me));
                Dictionary<Metroline, int> d = new Dictionary<Metroline, int>();
                int index = 0;
                foreach (int v in order[me]) {
                    d[MetrolinesGlobal[v]] = index++;
                }
                lineIndexInOrder.Add(me, d);
            }
        }

        /// <summary>
        /// Remove vertex v from the graph. Update graph and paths correspondingly
        /// </summary>
        void ProcessNonTerminal(int v) {
            //oldEdge => sorted PathLists
            Dictionary<MetroEdge, List<PathList>> newSubLists = RadixSort(v);

            //update current data
            foreach (MetroEdge oldEdge in orderedAdjacent[v]) {
                Debug.Assert(e2p.ContainsKey(oldEdge));
                List<PathList> newSubList = newSubLists[oldEdge];

                //update e2p[oldEdge]
                e2p[oldEdge].paths = null;
                e2p[oldEdge].subLists = newSubList;

                //update ordered adjacency data
                UpdateAdjacencyData(OppositeNode(oldEdge, v), oldEdge, newSubList);

                //update paths and add new edges
                foreach (PathList pl in newSubList) {
                    MetroEdge newEdge = pl.edge;

                    //we could check the reverse edge before
                    if (e2p.ContainsKey(newEdge)) continue;

                    //add e2p for new edge
                    e2p.Add(newEdge, pl);

                    //update paths
                    foreach (PathOnEdge path in pl.paths) {
                        UpdatePath(path, v, newEdge);
                    }
                }
            }
        }

        /// <summary>
        /// Linear sorting of paths passing through vertex v
        /// </summary>
        Dictionary<MetroEdge, List<PathList>> RadixSort(int v) {
            //build a map [old_edge => list_of_paths_on_it]; the relative order of paths is important
            Dictionary<MetroEdge, List<PathOnEdge>> r = new Dictionary<MetroEdge, List<PathOnEdge>>();
            //first index in circular order
            Dictionary<MetroEdge, int> firstIndex = new Dictionary<MetroEdge, int>();

            foreach (MetroEdge oldEdge in orderedAdjacent[v]) {
                PathList pathList = e2p[oldEdge];
                foreach (PathOnEdge path in pathList.paths) {
                    MetroEdge ej = FindNextEdgeOnPath(v, path);
                    CollectionUtilities.AddToMap(r, ej, path);
                }

                firstIndex.Add(oldEdge, (r.ContainsKey(oldEdge) ? r[oldEdge].Count : 0));
            }

            //oldEdge => SortedPathLists
            Dictionary<MetroEdge, List<PathList>> res = new Dictionary<MetroEdge, List<PathList>>();
            //build the desired order for each edge
            foreach (MetroEdge oldEdge in orderedAdjacent[v]) {
                //r[oldEdge] is the right order! (up to the circleness)
                List<PathOnEdge> paths = r[oldEdge];
                Debug.Assert(paths.Count > 0);

                List<PathList> subLists = new List<PathList>();
                HashSet<PathOnEdge> curPathSet = new HashSet<PathOnEdge>();

                for (int j = 0; j < paths.Count; j++) {

                    int i = (j + firstIndex[oldEdge]) % paths.Count;
                    MetroEdge nowEdge = paths[i].node.Value;
                    MetroEdge nextEdge = paths[(i + 1) % paths.Count].node.Value;

                    curPathSet.Add(paths[i]);

                    if (j == paths.Count - 1 || nowEdge != nextEdge) {
                        //process
                        MetroEdge newEdge = MetroEdge.CreateFromTwoEdges(v, oldEdge, nowEdge);
                        PathList pl = new PathList();
                        pl.edge = newEdge;
                        pl.paths = curPathSet;
                        subLists.Add(pl);

                        //clear
                        curPathSet = new HashSet<PathOnEdge>();
                    }
                }

                if (oldEdge.Source() == v) subLists.Reverse();
                res.Add(oldEdge, subLists);
            }

            return res;
        }

        /// <summary>
        /// extract the next edge on a given path after node v
        /// </summary>
        MetroEdge FindNextEdgeOnPath(int v, PathOnEdge pathOnEdge) {
            if (pathOnEdge.node.Next != null) {
                int o = OppositeNode(pathOnEdge.node.Next.Value, v);
                if (o != -1) return pathOnEdge.node.Next.Value;
            }

            if (pathOnEdge.node.Previous != null) {
                int o = OppositeNode(pathOnEdge.node.Previous.Value, v);
                if (o != -1) return pathOnEdge.node.Previous.Value;
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// return an opposite vertex of a given edge
        /// </summary>
        int OppositeNode(MetroEdge edge, int v) {
            if (edge.Source() == v) return edge.Target();
            if (edge.Target() == v) return edge.Source();

            return -1;
        }

        /// <summary>
        /// replace edges (av) and (vb) with edge (ab) on a given path
        /// </summary>
        void UpdatePath(PathOnEdge pathOnEdge, int v, MetroEdge newEdge) {
            LinkedListNode<MetroEdge> f = pathOnEdge.node;
            Debug.Assert(f.Value.Source() == v || f.Value.Target() == v);

            int a, b;

            a = OppositeNode(f.Value, v);

            if (f.Next != null && (b = OppositeNode(f.Next.Value, v)) != -1) {
                Debug.Assert((a == newEdge.Source() || a == newEdge.Target()));
                Debug.Assert((b == newEdge.Source() || b == newEdge.Target()));

                f.Value = newEdge;
                f.List.Remove(f.Next);
            }
            else if (f.Previous != null && (b = OppositeNode(f.Previous.Value, v)) != -1) {
                Debug.Assert((a == newEdge.Source() || a == newEdge.Target()));
                Debug.Assert((b == newEdge.Source() || b == newEdge.Target()));

                f.Value = newEdge;
                f.List.Remove(f.Previous);
            }
            else
                throw new NotSupportedException();
        }
    }


}
