using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy {
    /// <summary>
    ///     Main class, which iteratively computes a layout according to the given votings and positions:
    ///     (paper: Graph Drawing by Stress Majorization by Emden R. Gansner, Yehuda Koren, Stephen North)
    ///     This class allows to deal with the sparse Stress also.
    /// </summary>
    public class StressMajorization {

        internal StressMajorizationSettings Settings { get; set; }

        /// <summary>
        ///     An entry in the list corresponds to a voting from another node to that node.
        ///     For each node a set of votings from other nodes or reference points(virtual nodes).
        /// </summary>
        internal List<NodeVoting> NodeVotings { get; set; }

        /// <summary>
        ///     Positions to which the votings refer to. Positions may belong to nodes or just reference points (virtual nodes).
        ///     used to influence the layout.
        /// </summary>
        internal List<Point> Positions { get; set; }

        /// <summary>
        ///     Iterates only once.
        /// </summary>
        /// <returns></returns>
        public List<Point> IterateSingleLocalizedMethod() {
            List<Point> newPositions;
            if (Settings.UpdateMethod == UpdateMethod.Serial)
                newPositions = Positions;
            else
                newPositions = new List<Point>(Positions.Count);

            // For each node compute the new position by averaging over all votes.
            for (int i = 0; i < NodeVotings.Count; i++) {
                NodeVoting nodeVoting = NodeVotings[i];
                int votedIndex = nodeVoting.VotedNodeIndex;
                Point newPos = LocalizedOptimization(nodeVoting);
                if (Settings.UpdateMethod == UpdateMethod.Serial) {
                    newPositions[votedIndex] = newPos;
                }
                else newPositions.Add(newPos);
            }

            if (Settings.UpdateMethod == UpdateMethod.Parallel) {
                for (int i = 0; i < NodeVotings.Count; i++) {
                    NodeVoting nodeVoting = NodeVotings[i];
                    int index = nodeVoting.VotedNodeIndex;
                    Positions[index] = newPositions[i];
                }
            }

            return newPositions;
        }

        /// <summary>
        ///     Applies the Stress Majorization layout process with the defined StressSettings.
        /// </summary>
        /// <returns></returns>
        public List<Point> IterateAll() {
            initMaxIterationsSolver();
            int i = 0;
            List<Point> res = null;
            double stressOld = StressValue(Positions);
            while ( (!Settings.CancelOnStressMaxIteration || (i++)<Settings.MaxStressIterations)){
                if (Settings.SolvingMethod == SolvingMethod.Localized)
                    res = IterateSingleLocalizedMethod();
                else {
                    res = IterateSingleConjugateGradient();
                    if (Settings.CancelAfterFirstConjugate) break;
                }

                double stressNew = StressValue(res);//stressNew <= stressOld (this should always hold, otherwise there is something wrong)
                double stressChange = (stressOld > 0) ? (stressOld - stressNew)/stressOld : 0;
                stressChange = Math.Sqrt(stressChange)/Positions.Count;
                if (stressChange < Settings.StressChangeTolerance) break;
            }
            return res;
        }

        /// <summary>
        /// Sets the maximal number of iterations for the solver. Only relevant if conjugate gradient method is used.
        /// </summary>
         void initMaxIterationsSolver() {
            if (Positions == null) return;
            int problemSize = Positions.Count;
            int maxIterat = Settings.MaxSolverIterations;

            switch (Settings.SolverMaxIteratMethod) {
                case MaxIterationMethod.FixedMax:
                    return;
                case MaxIterationMethod.SqrtProblemSize:
                    maxIterat = (int) (Math.Ceiling(Math.Sqrt(problemSize)));
                    break;
                case MaxIterationMethod.LinearProblemSize:
                    maxIterat = problemSize;
                    break;
                default:
                    return;
            }
            Settings.MaxSolverIterations = maxIterat;
        }

        /// <summary>
        ///  To get the next node coordinates the following system has to be solved:
        ///  Lw*x=Lx*z(i), where x is the (unknown) node position vector, Lw and Lx are two known matrices (see Graph Drawing by Stress Majorization by Gansner et. al.) 
        ///  and z(i) is the vector of current node positions in dimension i. 
        ///  Each dimension has to be solved seperately.
        /// </summary>
        /// <returns></returns>
         List<Point> IterateSingleConjugateGradient() {
            SparseMatrix Lw;
            SparseMatrix Lx;
            ConstructLinearSystemFromMajorization(out Lw, out Lx);
           
            double[] zX = Positions.Select(p => p.X).ToArray();
            double[] zY = Positions.Select(p => p.Y).ToArray();

            double[] bX = Lx*zX;
            double[] bY = Lx*zY;
            //solve each dimension seperately
            double[] resX=null, resY=null;


            // solve for x/y dimension and do it in parallel if settings allow.

            var invokers = new Dictionary<bool, Action<Action[]>> {
                {true, Parallel.Invoke},
                {false, actions => Array.ForEach(actions, a => a())}
            };
            var solvers = new Dictionary<SolvingMethod, Func<SparseMatrix, double[], double[], int, double, double[]>> {
                { SolvingMethod.ConjugateGradient, LinearSystemSolver.SolveConjugateGradient },
                { SolvingMethod.PrecondConjugateGradient, LinearSystemSolver.SolvePrecondConjugateGradient }
            };

            invokers[Settings.Parallelize](new Action[] {
                () => resX = solvers[Settings.SolvingMethod](Lw, bX, zX, Settings.MaxSolverIterations,
                                                                                       Settings.ResidualTolerance),
                                                                       () =>
                                                                       resY =
                                                                       solvers[Settings.SolvingMethod](Lw, bY,
                                                                                                                 zY,
                                                                                                                 Settings.MaxSolverIterations,
                                                                       
                                                                                       Settings.ResidualTolerance)
});

            for (int i = 0; i < Positions.Count; i++)
                Positions[i] = new Point(resX[i], resY[i]);

            return Positions;

        }


        /// <summary>
        ///     Local optimization for a given node, as described in Graph Drawing by Stress Majorization by Gansner et. al. (Sect. 2.3).
        /// </summary>
        /// <param name="nodeVoting"></param>
        /// <returns></returns>
         Point LocalizedOptimization(NodeVoting nodeVoting) {
            Point currentPosition = Positions[nodeVoting.VotedNodeIndex];
            double nextX = 0;
            double nextY = 0;
            double sumWeights = 0;

            // if there is not voting vlock, the position of this node will not change
            if (nodeVoting.VotingBlocks == null || nodeVoting.VotingBlocks.Count == 0) {
                return currentPosition;
            }

            foreach (VoteBlock votingBlock in nodeVoting.VotingBlocks) {
                double blockWeight = votingBlock.BlockWeight;
                foreach (Vote vote in votingBlock.Votings) {
                    Point voterPos = Positions[vote.VoterIndex];

                    double votingDistance = vote.Distance;
                    double diffX = currentPosition.X - voterPos.X;
                    double diffY = currentPosition.Y - voterPos.Y;
                    double euclidDistance = Math.Sqrt(diffX*diffX + diffY*diffY);
                    double weight = blockWeight*vote.Weight;

                    double voteX = voterPos.X + votingDistance*diffX/euclidDistance;
                    nextX += voteX*weight;
                    double voteY = voterPos.Y + votingDistance*diffY/euclidDistance;
                    nextY += voteY*weight;

                    sumWeights += weight;
                }
            }

            if (sumWeights == 0) {
                // there was no single voting, just return the current position
                return currentPosition;
            }
            return new Point(nextX/sumWeights, nextY/sumWeights);
        }


        /// <summary>
        ///     Clears the votings for a given node representative, but leaves the index references.
        /// </summary>
        /// <param name="nodeVoting"></param>
         void ClearVoting(NodeVoting nodeVoting) {
            foreach (VoteBlock block in nodeVoting.VotingBlocks) {
                block.Votings.Clear();
            }
        }

        /// <summary>
        ///     Clears the Votings of the nodes.
        /// </summary>
        public void ClearVotings() {
            foreach (NodeVoting nodeVoting in NodeVotings) {
                ClearVoting(nodeVoting);
            }
        }

#if TEST_MSAGL
    /// <summary>
    ///     Only for internal testing purposes. Three nodes a-b-c, after the process the distances should be 10 between them.
    /// </summary>
        public static void TestMajorizationSmall() {
            // small 4-star to test the majorization
            var positions = new List<Point>(4);

            positions.Add(new Point(0, 0));
            positions.Add(new Point(-2, -1));
            positions.Add(new Point(1, 2));


            //for first node on 0/0 create forces to push the other away
            var nodeVotings = new List<NodeVoting>(8);


            var votesNode1 = new List<Vote>();
            var votesNodeLeft = new List<Vote>();
            var votesNodeRight = new List<Vote>();

            votesNodeLeft.Add(new Vote(0, 10, 1/Math.Pow(10, 2))); // force from middle node to left node
            votesNodeRight.Add(new Vote(0, 10, 1/Math.Pow(10, 2))); // force from middle node to right node

            votesNode1.Add(new Vote(1, 10, 1/Math.Pow(10, 2)));
            votesNode1.Add(new Vote(2, 10, 1/Math.Pow(10, 2)));

            var blockNode1 = new List<VoteBlock>();
            blockNode1.Add(new VoteBlock(votesNode1, 1));

            var blockNodeLeft = new List<VoteBlock>();
            blockNodeLeft.Add(new VoteBlock(votesNodeLeft, 1));

            var blockNodeRight = new List<VoteBlock>();
            blockNodeRight.Add(new VoteBlock(votesNodeRight, 1));


            nodeVotings.Add(new NodeVoting(0, blockNode1));
            nodeVotings.Add(new NodeVoting(1, blockNodeLeft));
            nodeVotings.Add(new NodeVoting(2, blockNodeRight));


            var majorization = new StressMajorization();
            majorization.Positions = positions;
            majorization.NodeVotings = nodeVotings;

            for (int i = 0; i < 20; i++) {
                List<Point> result = majorization.IterateSingleLocalizedMethod();

                foreach (Point point in result) {
                    System.Diagnostics.Debug.WriteLine("ResultPoint: {0}", point.ToString());
                }

                System.Diagnostics.Debug.WriteLine("Distance To Left: {0}", (result[0] - result[1]).Length);
                System.Diagnostics.Debug.WriteLine("Distance To Right: {0}", (result[0] - result[2]).Length);

                System.Diagnostics.Debug.WriteLine("----------------------------------------");
            }
        }
#endif

         void ConstructLinearSystemFromMajorization(out SparseMatrix Lw, out SparseMatrix Lx) {
            int numEdges = GetNumberOfEdges(NodeVotings);

#if SHARPKIT //SharpKit/Colin: multidimensional arrays not supported in JavaScript - https://code.google.com/p/sharpkit/issues/detail?id=340
            var edgesDistance = new double[numEdges];
            var edgesWeight = new double[numEdges];
#else            
            var edges = new double[numEdges,2];
#endif

            // list of undirected (symmetric) edges: [,0]=distance, [,1]=weight , every edge must be symmetric (thus exist once in each direction).

            //each element in the array corresponds to a node, the list corresponds to the adjacency list of that node.
            //int[0]: node id of adjacent node, int[1]: edge id of this edge
            var adjLists = new List<int[]>[NodeVotings.Count];

            int row = 0;
            int edgeId = 0;
            foreach (NodeVoting nodeVoting in NodeVotings) {
                int targetId = nodeVoting.VotedNodeIndex;
                int numAdj = 0;
                nodeVoting.VotingBlocks.ForEach(block => numAdj += block.Votings.Count);
                var currentAdj = new List<int[]>(numAdj);
                if (row != targetId)
                    throw new ArgumentOutOfRangeException("VotedNodeIndex must be consecutive starting from 0");
                adjLists[row] = currentAdj;
                foreach (VoteBlock block in nodeVoting.VotingBlocks) {
                    foreach (Vote voting in block.Votings) {
                        //corresponds to an edge or an entry in the corresponding matrix
                        int sourceId = voting.VoterIndex;

#if SHARPKIT
                        edgesDistance[edgeId] = voting.Distance;
                        edgesWeight[edgeId] = voting.Weight * block.BlockWeight;
#else
                        edges[edgeId, 0] = voting.Distance;
                        edges[edgeId, 1] = voting.Weight*block.BlockWeight;
#endif
                        currentAdj.Add(new[] { sourceId, edgeId });
                        edgeId++;
                    }
                }
                row++;
            }

            //TODO check if sorting can be removed to make it faster.

            //sort the adjacency lists, so that we can create a sparse matrix where the ordering is important
            for (int rowC = 0; rowC < adjLists.Length; rowC++) {
                List<int[]> adjList = adjLists[rowC];
                //add self loop, since there will be an entry in the matrix too.
                adjList.Add(new[] {rowC, -1});

                adjList.Sort((a, b) => a[0].CompareTo(b[0]));
            }
            numEdges += adjLists.Length; //self loop added to every node, so number of edges has increased by n.

            var diagonalPos = new int[adjLists.Length];
            // points to the position of the i-th diagonal entry in the flatened value array
            Lw = new SparseMatrix(numEdges, adjLists.Length, adjLists.Length);
            Lx = new SparseMatrix(numEdges, adjLists.Length, adjLists.Length);

            int valPos = 0;
            for (int rowId = 0; rowId < adjLists.Length; rowId++) {
                List<int[]> adjList = adjLists[rowId];
                double sumLw = 0;
                double sumLx = 0;
                foreach (var node in adjList) {
                    int colId = node[0];
                    Lw.ColInd()[valPos] = colId;
                    Lx.ColInd()[valPos] = colId;
                    if (rowId == colId) {
                        //on diagonal of matrix
                        diagonalPos[rowId] = valPos; //we will fill in the value later with the sum of all row entries
                    }
                    else {
#if SHARPKIT
                        double distance = edgesDistance[node[1]]; // distance(rowId,colId)
                        double weight = edgesWeight[node[1]]; // weight(rowId,colId)
#else
                        double distance = edges[node[1], 0]; // distance(rowId,colId)
                        double weight = edges[node[1], 1]; // weight(rowId,colId)
#endif

                        // weighted laplacian fill
                        Lw.Values()[valPos] = -weight;
                        sumLw += weight;

                        // Lx, which is similar to weighted laplacian but takes the distance into account
                        //TODO extract the distance computation outside of this step to make the procedure faster
                        double euclid = (Positions[rowId] - Positions[colId]).Length;
                        double entry = -weight*distance/euclid;
                        Lx.Values()[valPos] = entry;
                        sumLx += entry;
                    }
                    valPos++;
                }
                //set the diagonal to the sum of all other entries in row
                Lw.Values()[diagonalPos[rowId]] = sumLw;
                Lx.Values()[diagonalPos[rowId]] = -sumLx;

                //mark row end in flatened list
                Lw.RowPtr()[rowId + 1] = valPos;
                Lx.RowPtr()[rowId + 1] = valPos;
            }
        }

         int GetNumberOfEdges(List<NodeVoting> nodeVotings) {
            int i = 0;
            foreach (NodeVoting nodeVoting in nodeVotings) {
                int targetId = nodeVoting.VotedNodeIndex;
                foreach (VoteBlock block in nodeVoting.VotingBlocks) {
                    i += block.Votings.Count;
                }
            }
            return i;
        }

        /// <summary>
        /// The value of the stress function with the current positions.
        /// </summary>
        /// <param name="nodePositions"></param>
        /// <returns></returns>
        public double StressValue(List<Point> nodePositions) {
            double stress = 0;
            foreach (NodeVoting nodeVoting in NodeVotings) {
                int targetId = nodeVoting.VotedNodeIndex;
                foreach (VoteBlock block in nodeVoting.VotingBlocks) {
                    foreach (Vote voting in block.Votings) {
                        //corresponds to an edge or an entry in the corresponding matrix
                        int sourceId = voting.VoterIndex;
                        double euclid = (nodePositions[targetId] - nodePositions[sourceId]).Length;
                        double diff = euclid - voting.Distance;
                        stress += block.BlockWeight*voting.Weight*(diff*diff);
                    }
                }
            }

            return stress;
        }
    }
}
