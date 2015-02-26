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
namespace Microsoft.Msagl.Drawing {
    internal enum Tokens {
        MsaglGraph,
        Text,
        Edges, Edge, Nodes, Node,
        SourceNodeID, TargetNodeID,
        ID, LayerSeparation,
        NodeAttribute, NodeSeparation, AspectRatio, OptimizeLabelPositions, Padding,
        TransparencyOfSelectedEntityColor, SelectedEntityColor, Color, A, R, G, B,
        SelectedNodeBoundaryColor, MinNodeWidth,
        MinNodeHeight, Border, Fillcolor, FontColor, FontName, FontSize,
        Label, LabelMargin, LineWidth, Shape, XRad, YRad, Styles, Style,
        BaseAttr, LabelSize, Width, Height, EdgeAttribute, EdgeSeparation, Weight, ArrowStyle, ArrowheadLength,
        GraphAttribute, BackgroundColor, Margin, MinNodeSeparation, MinLayerSeparation, LayerDirection,
        GeometryGraphIsPresent, UserData, UserDataType, SerializedUserData,
        Subgraphs,Subgraph,
        listOfSubgraphs,
        listOfNodes,
        graph,
        End
    }
}
