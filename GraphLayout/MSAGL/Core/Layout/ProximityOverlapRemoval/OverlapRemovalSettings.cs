/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MST;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy;
using Microsoft.Msagl.Layout.MDS;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval {
    /// <summary>
    /// Settings for Overlap Removal process. Usage of the properties depends on the algorithm.
    /// </summary>
    public class OverlapRemovalSettings {
        OverlapRemovalMethod method=OverlapRemovalMethod.Pmst;

        double epsilon = 0.01;
        int iterationsMax=1000;
        bool stopOnMaxIterat = false;
        double nodeSeparation = 4;
        int randomizationSeed = 1;
        InitialScaling initialScaling = InitialScaling.None;
        private bool workInInches;

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

        private bool randomizeAllPointsOnStart = false;
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
        /// Initial Scaling method is used if there is at least one edge.
        /// </summary>
        public InitialScaling InitialScaling {
            get { return initialScaling; }
            set { initialScaling = value; }
        }

        /// <summary>
        /// Divide the coordinates by 72(Pixels) to work in inches. At the end this transformation is reverted again.
        /// </summary>
        public bool WorkInInches {
            get { return workInInches; }
            set { workInInches = value; }
        }

        /// <summary>
        /// Method to be used for overlap removal.
        /// </summary>
        public OverlapRemovalMethod Method {
            get { return method; }
            set { method = value; }
        }

        /// <summary>
        /// Clones the settings together with the stressmajorization settings
        /// </summary>
        /// <returns></returns>
        public OverlapRemovalSettings Clone() {
            OverlapRemovalSettings settings = new OverlapRemovalSettings();
            settings.Method = Method;
            settings.Epsilon = this.Epsilon;
            settings.IterationsMax = this.IterationsMax;
            settings.StopOnMaxIterat = StopOnMaxIterat;
            settings.NodeSeparation = NodeSeparation;
            settings.RandomizationSeed = RandomizationSeed;
            settings.RandomizeAllPointsOnStart = randomizeAllPointsOnStart;
            settings.InitialScaling = this.InitialScaling;
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
