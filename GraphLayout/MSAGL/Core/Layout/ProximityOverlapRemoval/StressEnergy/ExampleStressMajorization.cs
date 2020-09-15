using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy {
    /// <summary>
    /// Example on how to use Stress Majorization with a small graph and Localized method.
    /// </summary>
    public class ExampleStressMajorization {

        /// <summary>
        /// Example on how to use Stress Majorization with a small graph and Localized method.
        /// </summary>
        public static void RunStressMajorizationExample() {

            //create a star graph where three nodes are connected to the center
            GeometryGraph graph = new GeometryGraph();
            graph.Nodes.Add(new Node());
            graph.Nodes.Add(new Node());
            graph.Nodes.Add(new Node());
            graph.Nodes.Add(new Node());

            //set initial positions, e.g., random
            graph.Nodes[0].BoundaryCurve = CurveFactory.CreateRectangle(20, 10, new Point(5, 5));
            graph.Nodes[1].BoundaryCurve = CurveFactory.CreateRectangle(20, 10, new Point(7, 10));
            graph.Nodes[2].BoundaryCurve = CurveFactory.CreateRectangle(20, 10, new Point(7, 2));
            graph.Nodes[3].BoundaryCurve = CurveFactory.CreateRectangle(20, 10, new Point(35, 1));

            graph.Edges.Add(new Edge(graph.Nodes[0], graph.Nodes[1]));
            graph.Edges.Add(new Edge(graph.Nodes[0], graph.Nodes[2]));
            graph.Edges.Add(new Edge(graph.Nodes[0], graph.Nodes[3]));

            //array with desired distances between the nodes for every edge
            double[] idealEdgeLength=new double[graph.Edges.Count];
            for (int i = 0; i < graph.Edges.Count; i++) {
                idealEdgeLength[i] = 100; //all edges should have this euclidean length
            }


            //create stress majorization class and set the desired distances on the edges
            StressMajorization majorizer = new StressMajorization();


            majorizer.Positions = new List<Point>(graph.Nodes.Select(v=>v.Center));
            majorizer.NodeVotings = new List<NodeVoting>(graph.Nodes.Count);

            // initialize for every node an empty block
            for (int i = 0; i < graph.Nodes.Count; i++) {
                var nodeVote = new NodeVoting(i); //by default there is already a block with weighting 1
                //optional: add second block with different type of edges, e.g., with stronger weight
                //var secondBlock = new VoteBlock(new List<Vote>(), 100);
                //nodeVote.VotingBlocks.Add(secondBlock);
                //var block2=nodeVote.VotingBlocks[1]; //block could be accessed like this in a later stage
                
                majorizer.NodeVotings.Add(nodeVote);
            }


            // for every edge set the desired distances by setting votings among the two end nodes.
            Dictionary<Node,int> posDict=new Dictionary<Node, int>();
            for (int i = 0; i < graph.Nodes.Count; i++) {
                posDict[graph.Nodes[i]] = i;
            }

            var edges = graph.Edges.ToArray();
            for (int i=0; i<graph.Edges.Count;i++) {
                var edge = edges[i];
                int nodeId1 = posDict[edge.Source];
                int nodeId2 = posDict[edge.Target];

                double idealDistance = idealEdgeLength[i];
                double weight = 1 / (idealDistance * idealDistance);
                var voteFromNode1 = new Vote(nodeId1, idealDistance, weight); // vote from node1 for node2
                var voteFromNode2 = new Vote(nodeId2, idealDistance, weight); // vote from node2 for node1
                
                // add vote of node1 to list of node2 (in first voting block)
                majorizer.NodeVotings[nodeId2].VotingBlocks[0].Votings.Add(voteFromNode1);
                // add vote of node2 to list of node1 (in first voting block)
                majorizer.NodeVotings[nodeId1].VotingBlocks[0].Votings.Add(voteFromNode2);
               

            }

            //used localized method to reduce stress
            majorizer.Settings=new StressMajorizationSettings();
            majorizer.Settings.SolvingMethod=SolvingMethod.Localized;

            List<Point> result = majorizer.IterateAll();

            for (int i = 0; i < result.Count; i++) {
                graph.Nodes[i].Center = result[i];
            }
#if TEST_MSAGL && !SHARPKIT
            LayoutAlgorithmSettings.ShowGraph(graph);
#endif
        }
    }
}
