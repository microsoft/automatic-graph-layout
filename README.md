# Microsoft Automatic Graph Layout
**MSAGL** is a .NET library and tool for graph layout and viewing. 

MSAGL was developed in Microsoft by Lev Nachmanson, Sergey Pupyrev, Tim Dwyer, Ted Hart, and Roman Prutkin.

## Getting Started

The simplest way to start with MSAGL in C# is to open GraphLayout.sln in Visual Studio, and and look at the Samples folder.

## MSAGL Modules

**The Core Layout engine (AutomaticGraphLayout.dll)** - [NuGet package](https://www.nuget.org/packages/AutomaticGraphLayout/)
This .NET asssembly contains the core layout functionality. Use this library if you just want MSAGL to perform the layout only and afterwards you will use a separate tool to perform the rendering and visalization.

**The Drawing module (AutomaticGraphLayout.Drawing.dll)** - [NuGet package](https://www.nuget.org/packages/AutomaticGraphLayout.Drawing/)
The Definitions of different drawing attributes like colors, line styles, etc. It also contains definitions of a node class, an edge class, and a graph class. By using these classes a user can create a graph object and use it later for layout, and rendering.


**A WPF control (Microsoft.Msagl.WpfGraphControl.dll)** - [NuGet package](https://www.nuget.org/packages/AutomaticGraphLayout.WpfGraphControl/)
The viewer control lets you visualize graphs and has and some other rendering functionality. Key features: (1) Pan and Zoom (2) Navigate Forward and Backward (3) tooltips and highlighting on graph entities (4) Search for and focus on graph entities.

**A Windows Forms  Viewer control (Microsoft.Msagl.GraphViewerGdi.dll)** - [NuGet package](https://www.nuget.org/packages/AutomaticGraphLayout.GraphViewerGDI/)
The viewer control lets you visualize graphs and has and some other rendering functionality. Key features: (1) Pan and Zoom (2) Navigate Forward and Backward (3) tooltips and highlighting on graph entities (4) Search for and focus on graph entities.

# Code Samples
The code snippets demonstrate the basic usage of the viewer. It uses the C# language.

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

GraphMaps lets you view very large graphs like oneline maps - as you zoom in more detail is revealed. Watch a [video](https://youtu.be/qCUP20dQqBo) that shows how GraphMaps works, and here is the [video](http://i11www.iti.kit.edu/~rprutkin/composers.wmv) of the previous version.

## Using GraphMaps
* open Lg.sln and build the solution,
* run TestGraphMaps. 

NOTES:
* The configuration Release/x64 needs to be used to load a large graph.  
* The graph from the video can be found in GraphLayout/graphs/composers.zip. Please load composers.msagl to avoid the preprocessing step.
* If composers.dot is loaded then composers.msagl and the tiles directory composers.msagl_tiles will be
regenerated. 

## Learn More
The ideas, design, and the mathematics of GraphMaps are described in [this paper](http://arxiv.org/pdf/1506.06745v1.pdf).

# Layouts Created by MSAGL
![](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-195f1b23116b4f049b6e5dc815d96c89.png)
![](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-195f1b23116b4f049b6e5dc815d96c89.png)
![](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-c34826a5e3af4cecbd8165fabc947b36.jpg)
![](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/msagl-44a7b11774a54cab92a3f75a9501601b.png)

# MSAGL in JavaScript

WebMSAGL is a version of MSAGL that was transcompiled to JavaScript with [SharpKit](https://github.com/SharpKit/SharpKit/), plus a [TypeScript](https://www.typescriptlang.org/) wrapper and rendering/interaction layer that provides a friendly TypeScript API. You can create a graph either programmatically or from a JSON object, have MSAGL create a layout for it, and then render it to an HTML Canvas or to an SVG block. All layout operations are run in a web worker, ensuring that your application remains responsive while computation is taking place. Limited interactivity is also supported.

## Using WebMSAGL
* open WebMsagl.sln and build the solution,
* set index.html from any of the sample folders as the starting page,
* run WebMsagl.


# Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# Build

[![Build Status](https://dev.azure.com/MSAGL/MSAGLBuild/_apis/build/status/microsoft.automatic-graph-layout?branchName=master)](https://dev.azure.com/MSAGL/MSAGLBuild/_build/latest?definitionId=1&branchName=master)

## Producing a release
The release containing the binaries of agl.exe
can be created automatically by a github action of
".github\workflows\dotnet.yaml'.
To invoke the action do the following. 
Create a new tag in the form "release*". For example,  "git tag -a
release_11 -m "some comment here"". Then execute git push with this
tag: "git push origin release_11". These should trigger the release creation.
 

