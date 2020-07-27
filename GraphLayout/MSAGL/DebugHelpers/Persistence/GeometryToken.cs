#if TEST_MSAGL
namespace Microsoft.Msagl.DebugHelpers {
#pragma warning disable 1591
    /// <summary>
    /// tokens for the graph parser
    /// </summary>
    public enum GeometryToken
    {
        ///<summary>
        ///</summary>
        Clusters,
        ///<summary>
        ///</summary>
        Force,
        ///<summary>
        ///</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Barycenter")]
        Barycenter,
        ///<summary>
        ///</summary>
        Cluster,
        ///<summary>
        ///</summary>
        Width,
        /// <summary>
        /// 
        /// </summary>
        Height,
        /// <summary>
        /// 
        /// </summary>
        Label,
        /// <summary>
        /// 
        /// </summary>
        MsaglGeometryGraph,
        /// <summary>
        /// 
        /// </summary>
        Header,
        /// <summary>
        /// 
        /// </summary>
        AspectRatio,
        /// <summary>
        /// 
        /// </summary>
        Transform,
        /// <summary>
        /// 
        /// </summary>
        TransformElement,
        /// <summary>
        /// 
        /// </summary>
        NodeSeparation,
        /// <summary>
        /// 
        /// </summary>
        LayerSeparation,
        /// <summary>
        /// 
        /// </summary>
        Margins,
        /// <summary>
        /// 
        /// </summary>
        MinNodeHeight,
        /// <summary>
        /// 
        /// </summary>
        MinNodeWidth,
        /// <summary>
        /// 
        /// </summary>
        Nodes,
        /// <summary>
        /// 
        /// </summary>
        Edges,
        /// <summary>
        /// 
        /// </summary>
        Node,
        /// <summary>
        /// 
        /// </summary>
        Edge,
        /// <summary>
        /// 
        /// </summary>
        Id,
        /// <summary>
        /// 
        /// </summary>
        Padding,
        /// <summary>
        /// 
        /// </summary>
        ICurve,
        /// <summary>
        /// 
        /// </summary>
        Ellipse,
        /// <summary>
        /// 
        /// </summary>
        Curve,
        /// <summary>
        /// 
        /// </summary>
        LineSegment,
        /// <summary>
        /// 
        /// </summary>
        CubicBezierSegment,
        /// <summary>
        /// 
        /// </summary>
        AxisA,
        /// <summary>
        /// 
        /// </summary>
        AxisB,
        /// <summary>
        /// 
        /// </summary>
        Center,
        /// <summary>
        /// 
        /// </summary>
        Point,
        /// <summary>
        /// 
        /// </summary>
        XCoordinate,
        /// <summary>
        /// 
        /// </summary>
        YCoordinate,
        /// <summary>
        /// 
        /// </summary>
        SourceNodeId,
        /// <summary>
        /// 
        /// </summary>
        TargetNodeId,
        /// <summary>
        /// 
        /// </summary>
        LabelWidth,
        /// <summary>
        /// 
        /// </summary>
        LabelHeight,
        /// <summary>
        /// 
        /// </summary>
        LabelCenter,
        /// <summary>
        /// 
        /// </summary>
        LineWidth,
        /// <summary>
        /// 
        /// </summary>
        ArrowheadAtSource,
        /// <summary>
        /// 
        /// </summary>
        ArrowheadPosition,
        /// <summary>
        /// 
        /// </summary>
        ArrowheadAtTarget,
        /// <summary>
        /// 
        /// </summary>
        Weight,
        /// <summary>
        /// 
        /// </summary>
        Separation,
        /// <summary>
        /// 
        /// </summary>
        Start,
        /// <summary>
        /// 
        /// </summary>
        End,
        /// <summary>
        /// 
        /// </summary>
        B0,
        /// <summary>
        /// 
        /// </summary>
        B1,
        /// <summary>
        /// 
        /// </summary>
        B2,
        /// <summary>
        /// 
        /// </summary>
        B3,
        /// <summary>
        /// 
        /// </summary>
        UnderlyingPolyline,
        /// <summary>
        /// 
        /// </summary>
        UnderlyingPolylineIsNull,
        /// <summary>
        /// 
        /// </summary>
        PolylineSite,
        /// <summary>
        /// 
        /// </summary>    
        SiteK,
        /// <summary>
        /// 
        /// </summary>
        SiteV,
        /// <summary>
        /// 
        /// </summary>
        ParStart,
        /// <summary>
        /// 
        /// </summary>
        ParEnd,
        /// <summary>
        /// 
        /// </summary>
        Reporting,
        /// <summary>
        /// 
        /// </summary>
        RandomSeedForOrdering,
        /// <summary>
        /// 
        /// </summary>
        NoGainStepsBound,
        /// <summary>
        /// 
        /// </summary>
        MaxNumberOfPassesInOrdering,
        /// <summary>
        /// 
        /// </summary>
        Demotion,
        /// <summary>
        /// 
        /// </summary>
        GroupSplit,
        /// <summary>
        /// 
        /// </summary>
        LabelCornersPreserveCoefficient,
        /// <summary>
        /// 
        /// </summary>
        SplineCalculationDuration,
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Brandes")]
        BrandesThreshold,
        /// <summary>
        /// 
        /// </summary>
        LayoutAlgorithmSettings,
        /// <summary>
        /// 
        /// </summary>
        SugiyamaLayoutSettings,
        /// <summary>
        /// 
        /// </summary>
        MdsLayoutSettings,
        /// <summary>
        /// 
        /// </summary>
        RankingLayoutSetting,
        /// <summary>
        /// 
        /// </summary>
        LayoutAlgorithmType,
        /// <summary>
        /// 
        /// </summary>
        RepetitionCoefficientForOrdering,
        /// <summary>
        /// 
        /// </summary>
        Exponent,
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Majorization")]
        IterationsWithMajorization,
        /// <summary>
        /// 
        /// </summary>
        PivotNumber,
        /// <summary>
        /// 
        /// </summary>
        RotationAngle,
        /// <summary>
        /// 
        /// </summary>
        ScaleX,
        /// <summary>
        /// 
        /// </summary>
        ScaleY,
        /// <summary>
        /// 
        /// </summary>
        OmegaX,
        /// <summary>
        /// 
        /// </summary>
        OmegaY,
        /// <summary>
        /// defines the edge routing mode
        /// </summary>
        EdgeRoutingMode,
        /// <summary>
        /// 
        /// </summary>
        UseSparseVisibilityGraph,
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Kd")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Kd")]
        UseKdHull,
        /// <summary>
        /// 
        /// </summary>
        ClusterIndex,
        /// <summary>
        /// 
        /// </summary>
        ChildClusters,
        /// <summary>
        /// 
        /// </summary>
        ChildNodes,
        /// <summary>
        /// 
        /// </summary>
        RectangularBoundary,
        /// <summary>
        /// 
        /// </summary>
        Polyline,
        /// <summary>
        /// 
        /// </summary>
        Closed,
        /// <summary>
        /// 
        /// </summary>
        PolylinePoints,
        /// <summary>
        /// 
        /// </summary>
        CurveData,


        /// <summary>
        /// center x
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cx")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Cx")]
        Cx,
        /// <summary>
        /// center y
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Cy")]
        Cy,
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ry")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ry")]
        Ry,
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Rx")]
        Rx,
        /// <summary>
        /// 
        /// </summary>
        ArrowheadPositionAtSource,
        /// <summary>
        /// 
        /// </summary>
        ArrowheadPositionAtTarget,
        /// <summary>
        /// 
        /// </summary>
        Polygon,
        /// <summary>
        /// 
        /// </summary>
        MsaglGeometryFile,
        /// <summary>
        /// 
        /// </summary>
        Error,
        /// <summary>
        /// 
        /// </summary>
        Rect,
        /// <summary>
        /// 
        /// </summary>
        X,
        /// <summary>
        /// 
        /// </summary>
        Y,
        ///<summary>
        ///</summary>
        Points,
        ///<summary>
        ///</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "T")]
        T,
        ///<summary>
        ///</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "S")]
        S,
        /// <summary>
        /// arrowhead at source
        /// </summary>
        As,
        /// <summary>
        /// arrowhead at target
        /// </summary>
        At,
        /// <summary>
        /// 
        /// </summary>
        Graph,
        ///<summary>
        /// ArrowheadSourceLength
        ///</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Asl")]
        Asl,
        /// <summary>
        /// ArrowheadTargetLength
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Atl")]
        Atl,
        /// <summary>
        /// RectangleClusterBoundary
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Atl")]        
        RectangularClusterBoundary,
        RightBorderInfo,
        LeftBorderInfo,
        BottomBorderInfo,
        TopBorderInfo,
        LeftMargin,
        RightMargin,
        BottomMargin,
        TopMargin,
        GenerateFixedConstraints,
        GenerateFixedConstraintsDefault,
        DefaultBottomMargin,
        DefaultTopMargin,
        DefaultRightMargin,
        DefaultLeftMargin,
        InnerMargin,
        FixedPosition,
        LgData,
        SortedLgInfos,
        LgNodeInfo,
        Rail,
        LgLevels,
        LgNodeInfos,
        Rank,
        Level,
        NodeCountOnLevel,
        Rails,
        Arrowhead,
        CurveAttachmentPoint,
        RailsPerEdge,
        EdgeRails,
        RailId,
        EdgeId,
        LgEdgeInfos,
        LgEdgeInfo,
        Zoomlevel,
        SkeletonLevel,
        LgSkeletonLevels,
        MinPassingEdgeZoomLevel,
        LabelVisibleFromScale,
        LabelOffset,
        LabelWidthToHeightRatio,
        Unknown
    }
}
#endif  