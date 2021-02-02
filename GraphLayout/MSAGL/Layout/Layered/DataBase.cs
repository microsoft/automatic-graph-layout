using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Layout.Layered {
    /// <summary>
    /// This class holds assorted data associated with the graph under layout: list of anchors, 
    /// edges sorted by their sources,targets etc
    /// </summary>
#if TEST_MSAGL
    public 
#else
    internal
#endif
        class Database {

        /// <summary>
        /// maps middles of multiple strings to their buckets
        /// </summary>
        Set<int> multipleMiddles = new Set<int>();
#if TEST_MSAGL
        /// <summary>
        /// The layer to visualize. Is set to zero after each display
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int[] nodesToShow; //set it to the layer index to display. The 
     /// <summary>
     /// 
     /// </summary>
        public
#else
        internal
#endif
 Set<int> MultipleMiddles {
            get { return multipleMiddles; }
        }

        /// <summary>
        /// This table keeps multi edges
        /// </summary>
        Dictionary<IntPair, List<PolyIntEdge>> multiedges = new Dictionary<IntPair, List<PolyIntEdge>>();

        internal IEnumerable<PolyIntEdge> AllIntEdges {
            get {
                foreach (List<PolyIntEdge> l in Multiedges.Values)
                    foreach (PolyIntEdge e in l)
                        yield return e;
            }
        }



        internal Anchor[] anchors;

        /// <summary>
        /// Anchors of the nodes
        /// </summary>

#if TEST_MSAGL
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public
#else
        internal
#endif

 Anchor[] Anchors {
            get { return anchors; }
            set { anchors = value; }
        }

        internal void AddFeedbackSet(IEnumerable<IEdge> edges) {
            foreach (IEdge e in edges) {
                IntPair ip = new IntPair(e.Source, e.Target);
                IntPair ipr = new IntPair(e.Target, e.Source);

                //we shuffle reversed edges into the other multiedge
                List<PolyIntEdge> listToShuffle = multiedges[ip];
                foreach (PolyIntEdge er in listToShuffle)
                    er.Revert();

                if (multiedges.ContainsKey(ipr))
                    multiedges[ipr].AddRange(listToShuffle);
                else
                    multiedges[ipr] = listToShuffle;

                multiedges.Remove(ip);
            }
        }

        internal IEnumerable<List<PolyIntEdge>> RegularMultiedges {
            get {
                foreach (KeyValuePair<IntPair, List<PolyIntEdge>>
                        kv in Multiedges)
                    if (kv.Key.x != kv.Key.y)
                        yield return kv.Value;
            }
        }
#if TEST_MSAGL
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multiedges")]
        public
#else
        internal
#endif
 Dictionary<IntPair, List<PolyIntEdge>> Multiedges {
            get {
                return this.multiedges;
            }
        }


        internal List<PolyIntEdge> GetMultiedge(int source, int target) {
            return GetMultiedge(new IntPair(source, target));
        }


        internal List<PolyIntEdge> GetMultiedge(IntPair ip) {
            if (multiedges.ContainsKey(ip))
                return multiedges[ip];

            return new List<PolyIntEdge>();
        }


        internal void RegisterOriginalEdgeInMultiedges(PolyIntEdge edge) {
            IntPair ip = new IntPair(edge.Source, edge.Target);
            List<PolyIntEdge> o;
            if (multiedges.ContainsKey(ip) == false)
                multiedges[ip] = o = new List<PolyIntEdge>();
            else
                o = multiedges[ip];

            o.Add(edge);
        }

        internal IEnumerable<PolyIntEdge> SkeletonEdges() {
            return from kv in Multiedges where kv.Key.x != kv.Key.y select kv.Value[0];
        }
        }
}
