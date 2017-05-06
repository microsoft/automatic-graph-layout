# Microsoft Automatic Graph Layout
**MSAGL** is a .NET library and tool for graph layout and viewing. 

MSAFL was developed in Microsoft by Lev Nachmanson, Sergey Pupyrev, Tim Dwyer, Ted Hart, and Roman Prutkin.

## Getting Started

The simplest way to start with MSAGL in C# is to open GraphLayout.sln in Visual Studio, and and look at the Samples folder.

## MSAGL Modules

### The Core Layout engine (Microsoft.MSAGL.dll) - [NuGet package](https://www.nuget.org/packages/Microsoft.Automatic.Graph.Layout/)
This .NET asssembly contains the core layout functionality. Use this library if you just want MSAGL to perform the layout only and afterwards you will use a separate tool to perform the rendering and visalization.

### The **Drawing module** (Microsoft.MSAGL.Drawing.dll) - [NuGet package](https://www.nuget.org/packages/Microsoft.Automatic.Graph.Drawing/)
The Definitions of different drawing attributes like colors, line styles, etc. It also contains definitions of a node class, an edge class, and a graph class. By using these classes a user can create a graph object and use it later for layout, and rendering.

### A **Viewer control** (Microsoft.MSAGL.GraphViewerGDIGraph.dll)
The viewer control lets you visualize graphs and has and some other rendering functionality.

Key features:
* Pan and Zoom.
* Navigate Forward and Backward
* Configure tooltips and highlighting of graph entities.
* Search for and focus on graph entities.

# Code Samples
The code snippet demonstrates the basic usage of the viewer. It uses the C# language.

## The Viewer sample
![Drawing of the graph from the sampleDrawing of the graph from the sample](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-abc.jpg)

*Drawing of the graph from the sampleDrawing of the graph from the sample*

```csharp
using System;
using System.Collections.Generic; 
using System.Windows.Forms; 
class ViewerSample { 
    public static void Main() { 
    //create a form 
        System.Windows.Forms.Form form = new System.Windows.Forms.Form();
    //create a viewer object 
        Microsoft.Msagl.GraphViewerGdi.GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
    //create a graph object 
        Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("graph");
    //create the graph content 
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("A", "C").Attr.Color = Microsoft.Msagl.Drawing.Color.Green;
        graph.FindNode("A").Attr.FillColor = Microsoft.Msagl.Drawing.Color.Magenta;
        graph.FindNode("B").Attr.FillColor = Microsoft.Msagl.Drawing.Color.MistyRose;
        Microsoft.Msagl.Drawing.Node c = graph.FindNode("C");
        c.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PaleGreen;
        c.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Diamond;
    //bind the graph to the viewer 
        viewer.Graph = graph;
    //associate the viewer with the form 
        form.SuspendLayout();
        viewer.Dock = System.Windows.Forms.DockStyle.Fill;
        form.Controls.Add(viewer);
        form.ResumeLayout();
    //show the form 
        form.ShowDialog();
    } 
}
```

[More code
samples can be found here…](https://www.microsoft.com/en-us/research/project/microsoft-automatic-graph-layout/#code-samples)

# GraphMaps
This functionality allows viewing a large graph in the
online map fashion. Here is a [video](https://1drv.ms/v/s!AhsA76T-agdHgUBKXzpdOHeVNmq9) demoing
GraphMaps. To see the system in action please open Lg.sln, build it,
and run TestGraphMaps. The configuration Release/x64 needs to be used
to load a large graph.  The graph from the video can be found in
GraphLayout/graphs/composers.zip. Please load composers.msagl to avoid
the preprocessing step.  If composers.dot is loaded then
composers.msagl and the tiles directory composers.msagl_tiles will be
regenerated. GraphMaps ideas, design, and the mathematics are described in a
[paper](http://arxiv.org/pdf/1506.06745v1.pdf).

# Layouts Created by MSAGL
![](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-195f1b23116b4f049b6e5dc815d96c89.png)
![](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-195f1b23116b4f049b6e5dc815d96c89.png)
![](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-c34826a5e3af4cecbd8165fabc947b36.jpg)
![](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-44a7b11774a54cab92a3f75a9501601b.png)


# Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
