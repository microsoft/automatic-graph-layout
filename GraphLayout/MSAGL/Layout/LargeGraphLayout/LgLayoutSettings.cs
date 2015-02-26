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
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// layout settings to handle a large graph
    /// </summary>
    public class LgLayoutSettings : LayoutAlgorithmSettings {
        internal readonly Func<PlaneTransformation> TransformFromGraphToScreen;
        internal readonly double DpiX;
        internal readonly double DpiY;

        /// <summary>
        /// Func giving the maximal arrowhead length for the current viewport
        /// </summary>
        public readonly Func<double> MaximalArrowheadLength;



        /// <summary>
        /// delegate to be called by LgInteractor on a graph change
        /// </summary>
        public delegate void LgLayoutEvent();

        /// <summary>
        /// event handler to be raised when the graph and its transform change
        /// </summary>
        public event LgLayoutEvent ViewerChangeTransformAndInvalidateGraph;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transformation"></param>
        public void OnViewerChangeTransformAndInvalidateGraph(PlaneTransformation transformation) {
            if (ViewerChangeTransformAndInvalidateGraph != null)
                ViewerChangeTransformAndInvalidateGraph();
        }

        /// <summary>
        /// this graph is shown in the viewer
        /// </summary>
        public RailGraph RailGraph {
            get { return Algorithm != null ? Algorithm.RailGraph : null; }
        }

        /// <summary>
        /// clones the object
        /// </summary>
        /// <returns></returns>
        public override LayoutAlgorithmSettings Clone() {
            return (LayoutAlgorithmSettings) MemberwiseClone();
        }

        /// <summary>
        /// the algorithm object
        /// </summary>
        public LgInteractor Algorithm { get; set; }


        /// <summary>
        /// a delegate returning the current viewport of the viewer
        /// </summary>
        internal Func<Rectangle> ClientViewportFunc { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="clientViewportFunc"></param>
        /// <param name="transformFromGraphToScreen"> </param>
        /// <param name="dpiX"> </param>
        /// <param name="dpiY"> </param>
        /// <param name="maximalArrowheadLength">the dynamic length of the arrowhead</param>
        public LgLayoutSettings(Func<Rectangle> clientViewportFunc,
                                Func<PlaneTransformation> transformFromGraphToScreen,
                                double dpiX, double dpiY,
                                Func<double> maximalArrowheadLength
            ) {
            ClientViewportFunc = clientViewportFunc;
            TransformFromGraphToScreen = transformFromGraphToScreen;
            DpiX = dpiX;
            DpiY = dpiY;
            MaximalArrowheadLength = maximalArrowheadLength;
            EdgeRoutingSettings.Padding = NodeSeparation/4;
            EdgeRoutingSettings.PolylinePadding = NodeSeparation/6;
        }

        internal Rectangle ClientViewportMappedToGraph {
            get {
                var t = TransformFromGraphToScreen().Inverse;
                var p0 = new Point(0, 0);
                var vp = ClientViewportFunc();
                var p1 = new Point(vp.Width, vp.Height);
                return new Rectangle(t*p0, t*p1);
            }
        }



        Dictionary<Node, LgNodeInfo> geometryNodesToLgNodeInfos = new Dictionary<Node, LgNodeInfo>();

        /// <summary>
        /// the mapping from Geometry nodes to LgNodes
        /// </summary>
        public Dictionary<Node, LgNodeInfo> GeometryNodesToLgNodeInfos {
            get { return geometryNodesToLgNodeInfos; }
            set { geometryNodesToLgNodeInfos = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IDictionary<Edge, LgEdgeInfo> GeometryEdgesToLgEdgeInfos {
            get { return Algorithm.GeometryEdgesToLgEdgeInfos; }
        }


        /// <summary>
        /// defines when a satellite becomes visible by muliplying current zoom by SattelliteZoomFactorForNode and comparing it with this.ZoomFactor
        /// </summary>
        public double SattelliteZoomFactor = 10;


        /// <summary>
        /// return the current zoom factor
        /// </summary>
        public double ZoomFactor {
            get { return Algorithm.GetZoomFactorToTheGraph(); }
        }


        /// <summary>
        /// 
        /// </summary>
        public Rectangle ScreenRectangle { get; set; }


        /// <summary>
        /// returns maximal edge zoom level range
        /// </summary>
        public Interval MaximalEdgeZoomLevelInterval {
            get { return Algorithm.MaximalEdgeZoomLevelInterval; }
        }

        double clusterPadding = 5;
        double reroutingDelayInSeconds = 1; //in seconds

        /// <summary>
        /// 
        /// </summary>
        public double ClusterPadding {
            get { return clusterPadding; }
            set { clusterPadding = value; }
        }

        /// <summary>
        /// the delay before firing the nice spline routing
        /// </summary>
        public double ReroutingDelayInSeconds {
            get { return reroutingDelayInSeconds; }
            set { reroutingDelayInSeconds = value; }
        }




        Interval _scaleInterval = new Interval(0.00001, 100000000.0);
        EdgeRoutingMode initialRouting = EdgeRoutingMode.StraightLine;
        bool needToLayout = true;
        int maxNumberNodesPerTile = 20;
        bool usePrecalculatedEdges = true;
        
        /// <summary>
        /// used to draw edges
        /// </summary>
        public byte EdgeTransparency = 100;

        /// <summary>
        /// used for debugging mostly
        /// </summary>
        public bool ExitAfterInit;

        /// <summary>
        /// the range of scale 
        /// </summary>
        public Interval ScaleInterval {
            get { return _scaleInterval; }
            set { _scaleInterval = value; }
        }

       
        /// <summary>
        /// controls the edge routing when loading the graph
        /// </summary>
        public EdgeRoutingMode InitialRouting {
            get { return initialRouting; }
            set { initialRouting = value; }
        }

        /// <summary>
        /// set this property to true if the graph comes with a given layout
        /// </summary>
        public bool NeedToLayout {
            get { return needToLayout; }
            set { needToLayout = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int MaxNumberNodesPerTile {
            get { return maxNumberNodesPerTile; }
            set { maxNumberNodesPerTile = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UsePrecalculatedEdges {
            get { return usePrecalculatedEdges; }
            set { usePrecalculatedEdges = value; }
        }

        public string BackgroundImage { get; set; }

        internal const double PathNodesScale = 0.1;
        /// <summary>
        /// used for debugging purposes
        /// </summary>
        public bool DrawBackgroundImage;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rail"></param>
        public void HighlightEdgesPassingThroughTheRail(Rail rail) {
            Algorithm.HighlightEdgesPassingThroughTheRail(rail);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edges"></param>
        public void HighlightEdges(IEnumerable<Edge> edges) {
            Algorithm.HighlightEdges(edges);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rail"></param>
        /// <returns></returns>
        public bool TheRailLevelIsTooHigh(Rail rail) {
            return Algorithm.TheRailLevelIsTooHigh(rail);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double GetMaximalZoomLevel() {
            return Algorithm.GetMaximalZoomLevel();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rail"></param>
        public void PutOffEdgesPassingThroughTheRail(Rail rail) {
            Algorithm.PutOffEdgesPassingThroughTheRail(rail);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edges"></param>
        public void PutOffEdges(List<Edge> edges) {
            Algorithm.PutOffEdges(edges);
        }

        public bool BackgroundImageIsHidden { get; set; }
    }
}