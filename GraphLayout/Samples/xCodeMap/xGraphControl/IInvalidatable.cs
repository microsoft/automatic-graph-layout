using Microsoft.Msagl.Drawing;
using System.Windows;

namespace xCodeMap.xGraphControl
{
    internal interface IInvalidatable {
        void Invalidate(double scale = 1);
    }

    public interface IViewerObjectX : IViewerObject
    {
        FrameworkElement VisualObject { get; }
    }

    public interface IViewerNodeX : IViewerNode, IViewerObjectX { }
    public interface IViewerEdgeX : IViewerEdge, IViewerObjectX { }

}