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
﻿//-----------------------------------------------------------------------
// <copyright file="SugiyamaSettingsTests.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.DebugHelpers.Persistence;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests
{
    [TestClass]
    public class SugiyamaSettingsTests : MsaglTestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            EnableDebugViewer();
            base.Initialize();
        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup();
        }

        [TestMethod]
        [Ignore] // Serialization/Deserialization doesn't currently work
        [Description("Verifies that SugiyamaLayoutSettings serialize and deserialize correctly.")]
        public void SerializeDeserialize()
        {
            // Create settings that are different from the defaults
            SugiyamaLayoutSettings oldSettings = new SugiyamaLayoutSettings();
            oldSettings.AspectRatio++;
            oldSettings.LayerSeparation++;
            oldSettings.RepetitionCoefficientForOrdering++;
            oldSettings.RandomSeedForOrdering++;
            oldSettings.NoGainAdjacentSwapStepsBound++;
            oldSettings.MaxNumberOfPassesInOrdering++;
            oldSettings.GroupSplit++;
            oldSettings.LabelCornersPreserveCoefficient++;
            oldSettings.BrandesThreshold++;
            oldSettings.MinimalWidth++;
            oldSettings.MinimalHeight++;
            oldSettings.NodeSeparation++;
            oldSettings.MinNodeHeight++;
            oldSettings.MinNodeWidth++;
            oldSettings.LayeringOnly = !oldSettings.LayeringOnly;
            
            // Serialize
            GeometryGraph oldGraph = new GeometryGraph();
            oldGraph.Nodes.Add(new Node());
            GeometryGraphWriter.Write(oldGraph, oldSettings, "settings.msagl.geom");

            // Deserialize
            LayoutAlgorithmSettings baseSettings;
            GeometryGraphReader.CreateFromFile("settings.msagl.geom", out baseSettings);
            SugiyamaLayoutSettings newSettings = (SugiyamaLayoutSettings)baseSettings;

            // Verify
            Assert.AreEqual(oldSettings.AspectRatio, newSettings.AspectRatio, "AspectRatio was not serialized correctly.");
            Assert.AreEqual(oldSettings.LayerSeparation, newSettings.LayerSeparation, "LayerSeparation was not serialized correctly.");
            Assert.AreEqual(oldSettings.LayeringOnly, newSettings.LayeringOnly, "LayeringOnly was not serialized correctly.");
            Assert.AreEqual(oldSettings.RepetitionCoefficientForOrdering, newSettings.RepetitionCoefficientForOrdering, "RepetitionCoefficientForOrdering was not serialized correctly.");
            Assert.AreEqual(oldSettings.RandomSeedForOrdering, newSettings.RandomSeedForOrdering, "RandomSeedForOrdering was not serialized correctly.");
            Assert.AreEqual(oldSettings.NoGainAdjacentSwapStepsBound, newSettings.NoGainAdjacentSwapStepsBound, "NoGainAdjacentSwapStepsBound was not serialized correctly.");
            Assert.AreEqual(oldSettings.MaxNumberOfPassesInOrdering, newSettings.MaxNumberOfPassesInOrdering, "MaxNumberOfPassesInOrdering was not serialized correctly.");
            Assert.AreEqual(oldSettings.GroupSplit, newSettings.GroupSplit, "GroupSplit was not serialized correctly.");
            Assert.AreEqual(oldSettings.LabelCornersPreserveCoefficient, newSettings.LabelCornersPreserveCoefficient, "LabelCornersPreserveCoefficient was not serialized correctly.");
            Assert.AreEqual(oldSettings.BrandesThreshold, newSettings.BrandesThreshold, "BrandesThreshold was not serialized correctly.");
            Assert.AreEqual(oldSettings.MinimalWidth, newSettings.MinimalWidth, "MinimalWidth was not serialized correctly.");
            Assert.AreEqual(oldSettings.MinimalHeight, newSettings.MinimalHeight, "MinimalHeight was not serialized correctly.");
            Assert.AreEqual(oldSettings.NodeSeparation, newSettings.NodeSeparation, "NodeSeparation was not serialized correctly.");
            Assert.AreEqual(oldSettings.MinNodeHeight, newSettings.MinNodeHeight, "MinNodeHeight was not serialized correctly.");
            Assert.AreEqual(oldSettings.MinNodeWidth, newSettings.MinNodeWidth, "MinNodeWidth was not serialized correctly.");
        }
    }
}
