//-----------------------------------------------------------------------
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
