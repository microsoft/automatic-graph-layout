using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy;
using Microsoft.Msagl.Layout.MDS;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval {
    /// <summary>
    /// Settings for Overlap Removal process. Usage of the properties depends on the algorithm.
    /// </summary>
    public class OverlapRemovalSettings {
        
        double epsilon = 0.01;
        int iterationsMax=1000;
        bool stopOnMaxIterat = false;
        double nodeSeparation = 4;
        int randomizationSeed = 1;
        bool workInInches;

        /// <summary>
        /// Constructor.
        /// </summary>
        public OverlapRemovalSettings() {
            StressSettings=new StressMajorizationSettings();
        }

        /// <summary>
        /// Settings for the StressMajorization process.
        /// </summary>
        public StressMajorizationSettings StressSettings { get; set; }

         bool randomizeAllPointsOnStart = false;
        /// <summary>
        /// If true, the overlap iteration process stops after maxIterat iterations.
        /// </summary>
        public bool StopOnMaxIterat {
            get { return stopOnMaxIterat; }
            set { stopOnMaxIterat = value; }
        }

        /// <summary>
        /// Epsilon
        /// </summary>
        public double Epsilon {
            get { return epsilon; }
            set { epsilon = value; }
        }

        /// <summary>
        /// Number of maxIterat to be made. In each iteration overlap is partly removed.
        /// </summary>
        public int IterationsMax {
            get { return iterationsMax; }
            set { iterationsMax = value; }
        }

        /// <summary>
        /// Minimal distance between nodes.
        /// </summary>
        public double NodeSeparation {
            get { return nodeSeparation; }
            set { nodeSeparation = value; }
        }

     
        /// <summary>
        /// 
        /// </summary>
        public int RandomizationSeed {
            get { return randomizationSeed; }
            set { randomizationSeed = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RandomizeAllPointsOnStart {
            get { return randomizeAllPointsOnStart; }
            set { randomizeAllPointsOnStart = value; }
        }
        
        /// <summary>
        /// Divide the coordinates by 72(Pixels) to work in inches. At the end this transformation is reverted again.
        /// </summary>
        public bool WorkInInches {
            get { return workInInches; }
            set { workInInches = value; }
        }

        
        /// <summary>
        /// Clones the settings together with the stressmajorization settings
        /// </summary>
        /// <returns></returns>
        public OverlapRemovalSettings Clone() {
            OverlapRemovalSettings settings = new OverlapRemovalSettings();
            settings.Epsilon = this.Epsilon;
            settings.IterationsMax = this.IterationsMax;
            settings.StopOnMaxIterat = StopOnMaxIterat;
            settings.NodeSeparation = NodeSeparation;
            settings.RandomizationSeed = RandomizationSeed;
            settings.RandomizeAllPointsOnStart = randomizeAllPointsOnStart;
            settings.WorkInInches = this.WorkInInches;
       
            settings.StressSettings=new StressMajorizationSettings();
            settings.StressSettings.MaxStressIterations = StressSettings.MaxStressIterations;
            settings.StressSettings.SolvingMethod = StressSettings.SolvingMethod;
            settings.StressSettings.UpdateMethod = StressSettings.UpdateMethod;
            settings.StressSettings.StressChangeTolerance = StressSettings.StressChangeTolerance;
            settings.StressSettings.CancelOnStressConvergence = StressSettings.CancelOnStressConvergence;
            settings.StressSettings.CancelOnStressMaxIteration = StressSettings.CancelOnStressMaxIteration;
            //relevant for conjugate gradient methods only
            settings.StressSettings.ResidualTolerance =StressSettings.ResidualTolerance;
            settings.StressSettings.CancelAfterFirstConjugate = StressSettings.CancelAfterFirstConjugate;
            settings.StressSettings.MaxSolverIterations = StressSettings.MaxSolverIterations;
            settings.StressSettings.SolverMaxIteratMethod = StressSettings.SolverMaxIteratMethod;
            settings.StressSettings.Parallelize = StressSettings.Parallelize;
            settings.StressSettings.ParallelDegree = StressSettings.ParallelDegree;
            return settings;
        }
    }
}
