//-----------------------------------------------------------------------
// <copyright file="GraphGenerator.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Msagl;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
//using Microsoft.Test.KoKoMo;

namespace Microsoft.Msagl.UnitTests {
    /// <summary>
    /// Class for generating various kinds of graphs.
    /// </summary>
    public static class GraphGenerator {
        /// <summary>
        /// Generates a tree with the given number of nodes.
        /// </summary>
        /// <returns>A graph that is a tree</returns>
        public static GeometryGraph GenerateTree(int nodeCount) {
            return GenerateTree(nodeCount, new Random(1));
        }

        /// <summary>
        /// Generates a tree with the given number of nodes.
        /// </summary>
        /// <returns>A graph that is a tree</returns>
        public static GeometryGraph GenerateTree(int nodeCount, Random random) {
            GeometryGraph graph = new GeometryGraph();

            List<Node> nodes = new List<Node>();

            for (int i = 0; i < nodeCount; i++) {
                // Space them out horizontally to prevent them from being on top of each other
                Node newNode = CreateNode(i);
                // newNode.Center = new Point(random.Next(nodeCount) * 50, random.Next(nodeCount) * 50);
                nodes.Add(newNode);
                graph.Nodes.Add(newNode);

                if (i > 0) {
                    Node target = nodes[random.Next(nodes.Count)];
                    Edge edge = CreateEdge(newNode, target);
                    graph.Edges.Add(edge);
                }
            }

            return graph;
        }

        /// <summary>
        /// Generate lattice graph with given number of nodes
        /// </summary>
        /// <returns>A graph with lattice pattern</returns>
        public static GeometryGraph GenerateSquareLattice(int nodeCount) {
            GeometryGraph graph = new GeometryGraph();

            int nodesOnOneEdge = (int)Math.Ceiling(Math.Sqrt(nodeCount));

            for (int i = 0; i < nodesOnOneEdge; i++) {
                for (int j = 0; j < nodesOnOneEdge; j++) {
                    Node node = CreateNode(graph.Nodes.Count.ToString(CultureInfo.InvariantCulture));

                    graph.Nodes.Add(node);

                    if (i > 0) {
                        List<Node> allNodes = graph.Nodes.ToList();
                        Node sourceNode = allNodes[(graph.Nodes.Count - 1) - nodesOnOneEdge];
                        Node targetNode = allNodes[graph.Nodes.Count - 1];
                        Edge edge = CreateEdge(sourceNode, targetNode);
                        graph.Edges.Add(edge);
                    }

                    if (j > 0) {
                        List<Node> allNodes = graph.Nodes.ToList();
                        Node sourceNode = allNodes[graph.Nodes.Count - 2];
                        Node targetNode = allNodes[graph.Nodes.Count - 1];
                        Edge edge = CreateEdge(sourceNode, targetNode);
                        graph.Edges.Add(edge);
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// Generate a circle graph with given number of nodes
        /// </summary>        
        /// <returns>A circle graph</returns>
        public static GeometryGraph GenerateCircle(int nodeCount) {
            GeometryGraph graph = new GeometryGraph();

            graph = GenerateSimpleChain(nodeCount);
            List<Node> allNodes = graph.Nodes.ToList();

            //create one additional edge from the last node to the first node to make a real circle
            //since the chain is made from edges between lower indexed node and higher indexed node
            Edge edgeToCircle = CreateEdge(allNodes[graph.Nodes.Count - 1], allNodes[0]);
            graph.Edges.Add(edgeToCircle);

            return graph;
        }

        /// <summary>
        /// Generate a simple chain graph with given number of nodes
        /// </summary>
        /// <returns>A chain graph</returns>
        public static GeometryGraph GenerateSimpleChain(int nodeCount) {
            GeometryGraph graph = new GeometryGraph();

            for (int i = 0; i < nodeCount; i++) {
                Node node = CreateNode(graph.Nodes.Count.ToString(CultureInfo.InvariantCulture));

                graph.Nodes.Add(node);

                if (i > 0) {
                    List<Node> allNodes = graph.Nodes.ToList();
                    Node sourceNode = allNodes[graph.Nodes.Count - 2];
                    Node targetNode = allNodes[graph.Nodes.Count - 1];
                    Edge edge = CreateEdge(sourceNode, targetNode);
                    graph.Edges.Add(edge);
                }
            }

            return graph;
        }

        /// <summary>
        /// Generate a full tree graph
        /// </summary>
        /// <returns>A tree graph</returns>
        public static GeometryGraph GenerateFullTree(int nodeCount, int numberOfChildren) {
            GeometryGraph graph = new GeometryGraph();

            int actualNumberOfNodes = numberOfChildren == 1 ? nodeCount : (int)(Math.Pow(numberOfChildren, Math.Ceiling(Math.Log(nodeCount, numberOfChildren))) - 1);

            if (nodeCount > 0) {
                Node node = CreateNode(graph.Nodes.Count.ToString(CultureInfo.InvariantCulture));

                graph.Nodes.Add(node);
            }

            int currentParent = 0;

            while (graph.Nodes.Count < actualNumberOfNodes) {
                for (int i = 0; i < numberOfChildren; i++) {
                    Node node = CreateNode(graph.Nodes.Count.ToString(CultureInfo.InvariantCulture));

                    graph.Nodes.Add(node);

                    Node sourceNode = node;
                    List<Node> allNodes = graph.Nodes.ToList();
                    Node targetNode = allNodes[currentParent];
                    Edge edge = CreateEdge(sourceNode, targetNode);
                    graph.Edges.Add(edge);
                }

                currentParent++;
            }

            return graph;
        }

        /// <summary>
        /// Generate a simple graph
        /// </summary>
        /// <returns>A Simple Graph</returns>
        public static GeometryGraph GenerateOneSimpleGraph() {
            GeometryGraph graph = new GeometryGraph();

            Node[] allNodes = new Node[5];

            for (int i = 0; i < 5; i++) {
                Node node = CreateNode(i);
                allNodes[i] = node;
                graph.Nodes.Add(node);
            }

            graph.Edges.Add(CreateEdge(allNodes[0], allNodes[1]));
            graph.Edges.Add(CreateEdge(allNodes[2], allNodes[1]));
            graph.Edges.Add(CreateEdge(allNodes[3], allNodes[1]));
            graph.Edges.Add(CreateEdge(allNodes[4], allNodes[1]));
            graph.Edges.Add(CreateEdge(allNodes[0], allNodes[2]));

            return graph;
        }

        /// <summary>
        /// Generate a graph with n nodes and m random edges
        /// </summary>
        /// <returns>A Graph</returns>
        public static GeometryGraph GenerateRandomGraph(int nodeCount, int edgeCount, Random random) {
            GeometryGraph graph = new GeometryGraph();

            Node[] allNodes = new Node[nodeCount];

            for (int i = 0; i < nodeCount; i++) {
                Node node = CreateNode(i);
                allNodes[i] = node;
                graph.Nodes.Add(node);
            }

            for (int i = 0; i < edgeCount; i++) {
                int s = random.Next(nodeCount);
                int t = random.Next(nodeCount);

                graph.Edges.Add(CreateEdge(allNodes[s], allNodes[t]));
            }

            return graph;

        }

        /// <summary>
        /// Generate a graph containing multiple sub groups
        /// </summary>
        /// <returns>A multi sub group graph</returns>
        public static GeometryGraph GenerateGraphWithSameSubgraphs(int numberOfSubgraphs, int numberOfNodesInSubgraphs) {
            GeometryGraph graph = new GeometryGraph();

            for (int i = 0; i < numberOfSubgraphs; i++) {
                Node[] nodes = new Node[numberOfNodesInSubgraphs];
                for (int j = 0; j < numberOfNodesInSubgraphs; j++) {
                    nodes[j] = CreateNode(i.ToString() + "-" + j.ToString());
                    graph.Nodes.Add(nodes[j]);
                }

                for (int j = 0; j < numberOfNodesInSubgraphs; j++) {
                    Edge edge = CreateEdge(nodes[j], nodes[(j + 1) % numberOfNodesInSubgraphs]);
                    graph.Edges.Add(edge);
                }
            }

            return graph;
        }

        /// <summary>
        /// Generate a fully connected graph
        /// </summary>
        /// <returns>A fully connected graph</returns>
        public static GeometryGraph GenerateFullyConnectedGraph(int numberOfNodes) {
            GeometryGraph graph = new GeometryGraph();

            for (int i = 0; i < numberOfNodes; i++) {
                Node node = CreateNode("N" + i.ToString());
                graph.Nodes.Add(node);
            }

            List<Node> allNodes = graph.Nodes.ToList();
            for (int i = 0; i < numberOfNodes; i++) {
                for (int j = 0; j < numberOfNodes; j++) {
                    if (i != j) {
                        Edge edge = CreateEdge(allNodes[i], allNodes[j]);
                        graph.Edges.Add(edge);
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// Update node shape with randomly selected boundary curve
        /// </summary>        
        public static void SetRandomNodeShapes(GeometryGraph graph, Random random) {
            if (graph == null) {
                return;
            }

            foreach (Node node in graph.Nodes) {
                node.BoundaryCurve = GetRandomShape(random);
            }
        }
        /*
        /// <summary>
        /// Generate a model based graph
        /// </summary>
        /// <returns>A controlled random graph</returns>
        public static GeometryGraph GenerateOneGraph(GeometryGraph initialGraph, int expectedNodeNumber, int expectedEdgeNumber, bool isConnected)
        {
            GraphGenerationModel model = new GraphGenerationModel(initialGraph);
            model.IsConnectedGraph = isConnected;
            model.CheckAction = () => { return model.NumberOfNodes >= expectedNodeNumber && model.NumberOfEdges >= expectedEdgeNumber; };

            using (ModelEngine modelEngine = new ModelEngine(model))
            {                
                modelEngine.Options.Timeout = System.Threading.Timeout.Infinite;
                Random random = new Random();
                modelEngine.Options.Seed = random.Next(0, 1000);
                
                //To generate some trouble graph, use seed numbers like 275, 710, 770, or etc.
                //modelEngine.Options.Seed = 275;
                modelEngine.RunUntil(() => { return model.IsDone; });
            }

            return model.Graph;
        }
        */
        #region Creation Helpers

        /// <summary>
        /// Creates a new node with a rectangular geometry
        /// </summary>
        /// <returns>The newly created node.</returns>
        public static Node CreateNode(int id) {
            return new Node(CurveFactory.CreateRectangle(50, 50, new Point(0, 0)), id);
        }

        public static Node CreateNode(string id) {
            return new Node(CurveFactory.CreateRectangle(50, 50, new Point(0, 0)), id);
        }

        /// <summary>
        /// Creates a Edge
        /// </summary>
        /// <returns>The newly created edge.</returns>
        public static Edge CreateEdge(Node source, Node target) {
            return new Edge(source, target) {
                SourcePort = new RelativeFloatingPort(() => source.BoundaryCurve, () => source.Center),
                TargetPort = new RelativeFloatingPort(() => target.BoundaryCurve, () => target.Center),
            };
        }

        private static ICurve GetRandomShape(Random random) {
            //we support rectangle, roundedRectangle, circle, ellipse, diamond, Octagon, triangle, star            
            int index = random.Next(8);
            switch (index) {
                case 0:
                    return CurveFactory.CreateRectangle(25, 15, new Microsoft.Msagl.Core.Geometry.Point());
                case 1:
                    return CurveFactory.CreateRectangleWithRoundedCorners(35, 25, 3, 3, new Microsoft.Msagl.Core.Geometry.Point());
                case 2:
                    return CurveFactory.CreateCircle(19, new Microsoft.Msagl.Core.Geometry.Point());
                case 3:
                    return CurveFactory.CreateEllipse(26, 18, new Microsoft.Msagl.Core.Geometry.Point());
                case 4:
                    return CurveFactory.CreateDiamond(25, 15, new Microsoft.Msagl.Core.Geometry.Point());
                case 5:
                    return CurveFactory.CreateOctagon(25, 15, new Microsoft.Msagl.Core.Geometry.Point());
                case 6:
                    return CurveFactory.CreateInteriorTriangle(30, 20, new Microsoft.Msagl.Core.Geometry.Point());
                case 7:
                    return CurveFactory.CreateStar(33, new Microsoft.Msagl.Core.Geometry.Point());
            }

            return null;
        }
        #endregion Creation Helpers
        /*
        #region Kokomo
        [Model]        
        internal class GraphGenerationModel : Model
        {
            [ModelVariable]
            private int numberOfNodes = 0;

            [ModelVariable]
            private int numberOfEdges = 0;

            [ModelVariable]
            private bool done;

            [ModelVariable]
            private GeometryGraph initialGraph; // initial graph to start with

            [ModelVariable]
            private GeometryGraph graph = new GeometryGraph();

            /// <summary>
            /// node number
            /// </summary>
            public int NumberOfNodes
            {
                get { return numberOfNodes; }
                set { numberOfNodes = value; }
            }

            /// <summary>
            /// edge number
            /// </summary>
            public int NumberOfEdges
            {
                get { return numberOfEdges; }
                set { numberOfEdges = value; }
            }

            /// <summary>
            /// completion or not
            /// </summary>
            public bool IsDone
            {
                get { return done; }
                set { done = value; }
            }

            /// <summary>
            /// layout graph
            /// </summary>
            public GeometryGraph Graph
            {
                get { return graph; }
                set { graph = value; }
            }

            /// <summary>
            /// initial graph
            /// </summary>
            public GeometryGraph InitialGraph
            {
                get { return initialGraph; }
                set { initialGraph = value; }
            }

            private bool isConnectedGraph;

            public bool IsConnectedGraph
            {
                get { return isConnectedGraph; }
                set { isConnectedGraph = value; }
            }

            /// <summary>
            /// constructor of GraphGenerationModel
            /// </summary>
            /// <param name="initialGraph">initial graph</param>
            public GraphGenerationModel(GeometryGraph initialGraph)
            {
                InitialGraph = initialGraph;

                if (InitialGraph != null)
                {
                    Graph = InitialGraph;
                    NumberOfNodes = Graph.Nodes.Count;
                }
            }

            /// <summary>
            /// add a node to the graph
            /// </summary>
            [ModelAction(Weight = 10)]
            public virtual void AddNode()
            {
                Node node = CreateNode(Graph.Nodes.Count.ToString(CultureInfo.InvariantCulture));

                Graph.Nodes.Add(node);
                NumberOfNodes = Graph.Nodes.Count;

                if (IsConnectedGraph)
                {
                    List<Node> allNodes = Graph.Nodes.ToList();
                    Node sourceNode = allNodes[this.Random.Next(Graph.Nodes.Count)];
                    Node targetNode = node; 

                    Edge edge = CreateEdge(sourceNode, targetNode);
                    Graph.Edges.Add(edge);
                    NumberOfEdges = Graph.Edges.Count;
                }
            }

            /// <summary>
            /// add an edge to the graph
            /// </summary>
            [ModelAction(Weight = 10)]
            [ModelRequirement(Variable = "NumberOfNodes", GreaterThanOrEqual = 2)]
            public virtual void AddEdge()
            {
                List<Node> allNodes = Graph.Nodes.ToList();
                Node sourceNode = allNodes[this.Random.Next(Graph.Nodes.Count)];
                Node targetNode = allNodes[this.Random.Next(Graph.Nodes.Count)];

                Edge edge = CreateEdge(sourceNode, targetNode);
                Graph.Edges.Add(edge);
                NumberOfEdges = Graph.Edges.Count;
            }

            /// <summary>
            /// Allow users to inject stop condition
            /// </summary>
            public Func<bool> CheckAction
            {
                get;
                set;
            }

            /// <summary>
            /// Stop the graph generation process
            /// </summary>
            [ModelAction(Weight = 1)]
            public virtual void StopGeneration()
            {
                if (CheckAction != null && CheckAction())
                {
                    IsDone = true;
                }
            }
        }
        #endregion
         */
    }
}
