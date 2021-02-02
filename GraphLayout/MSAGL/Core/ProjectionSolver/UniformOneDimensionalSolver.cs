using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.GraphAlgorithms;

namespace Microsoft.Msagl.Core.ProjectionSolver{
    internal class UniformOneDimensionalSolver {
        readonly Dictionary<int, double> idealPositions = new Dictionary<int, double>();
        readonly double varSepartion;
        /// <summary>
        /// desired variable separation
        /// </summary>
        /// <param name="variableSeparation"></param>
        public UniformOneDimensionalSolver(double variableSeparation){
            varSepartion = variableSeparation;
        }

        readonly List<UniformSolverVar> varList = new List<UniformSolverVar>();

        readonly Set<IntPair> constraints = new Set<IntPair>();
        BasicGraphOnEdges<IntPair> graph;

//        delegate IEnumerable<NudgerConstraint> Edges(int i);
//
//        delegate int End(NudgerConstraint constraint);

//        Edges outEdgesDel;
//        Edges inEdgesDel;
//        End sourceDelegate;
//        End targetDelegate;
//        Supremum minDel;
//        Supremum maxDel;

        internal void SetLowBound(double bound, int id){
            var v = Var(id);
            v.LowBound = Math.Max(bound, v.LowBound);
        }

        UniformSolverVar Var(int id){
            return varList[id];
        }

        internal void SetUpperBound(int id, double bound){
            var v = Var(id);
            v.UpperBound = Math.Min(bound, v.UpperBound);
        }

 

        internal void Solve(){
            SolveByRegularSolver();
        }

        readonly SolverShell solverShell = new SolverShell();

        void SolveByRegularSolver() {
            CreateVariablesForBounds();
            for (int i = 0; i < varList.Count; i++) {
                var v = varList[i];
                if (v.IsFixed)
                    solverShell.AddFixedVariable(i, v.Position);
                else {
                    solverShell.AddVariableWithIdealPosition(i, idealPositions[i]);
                    if (v.LowBound != double.NegativeInfinity)
                    //    solverShell.AddLeftRightSeparationConstraint(GetBoundId(v.LowBound), i, varSepartion);
                        constraints.Insert(new IntPair(GetBoundId(v.LowBound), i));
                    if (v.UpperBound != double.PositiveInfinity)
                        constraints.Insert(new IntPair(i, GetBoundId(v.UpperBound)));
                    
                }
            }
           
            CreateGraphAndRemoveCycles();

            foreach (var edge in graph.Edges) {
                var w = 0.0;
                if(edge.First<varList.Count)
                    w+=varList[edge.First].Width;
                if (edge.Second < varList.Count)
                    w += varList[edge.Second].Width;
                w /= 2;
                solverShell.AddLeftRightSeparationConstraint(edge.First, edge.Second, varSepartion + w);
            }
            solverShell.Solve();

            for (int i = 0; i < varList.Count; i++)
                varList[i].Position = solverShell.GetVariableResolvedPosition(i);
        }

        int GetBoundId(double bound) {
            return boundsToInt[bound];
        }

        void CreateVariablesForBounds(){
            foreach (var v in varList){
                if(v.IsFixed) continue;
                if (v.LowBound != double.NegativeInfinity)
                    RegisterBoundVar(v.LowBound);
                if (v.UpperBound != double.PositiveInfinity)
                    RegisterBoundVar(v.UpperBound);
            }
        }

        readonly Dictionary<double, int> boundsToInt = new Dictionary<double, int>();
        void RegisterBoundVar(double bound){
            if (!boundsToInt.ContainsKey(bound)){
                int varIndex=varList.Count + boundsToInt.Count;
                boundsToInt[bound] = varIndex;
                solverShell.AddFixedVariable(varIndex, bound);
            }
        }

        
        void CreateGraphAndRemoveCycles(){
            //edges in the graph go from a smaller value to a bigger value
            graph = new BasicGraphOnEdges<IntPair>(constraints, varList.Count+boundsToInt.Count);
            //removing cycles
            var feedbackSet = CycleRemoval<IntPair>.GetFeedbackSet(graph);
            if(feedbackSet!=null)
                foreach(var edge in feedbackSet)
                    graph.RemoveEdge(edge as IntPair);
        }

        internal double GetVariablePosition(int id){
            return varList[id].Position;
        }

        internal void AddConstraint(int i, int j){
            constraints.Insert(new IntPair(i, j));
        }

        internal void AddVariable(int id, double currentPosition, double idealPosition, double width){
            idealPositions[id] = idealPosition;
            AddVariable(id, currentPosition, false, width);

        }

        internal void AddFixedVariable(int id, double position){
            AddVariable(id, position,true, 0); //0 for width
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "id")]
        void AddVariable(int id, double position, bool isFixed, double width) {
            Debug.Assert(id==varList.Count);
            varList.Add(new UniformSolverVar { IsFixed = isFixed, Position = position, Width=width });
        }
    }
}