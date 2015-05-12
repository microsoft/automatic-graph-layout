using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Msagl.Layout.LargeGraphLayout;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    internal class HitTestHandler {
        
        public GraphmapsNode _nodeUnderMouse;
        public FrameworkElement _infoShape;

        Rail _railUnderMouse;

        // GraphViewer _viewer;
        Canvas _graphCanvas;

        public HitTestHandler(Canvas _graphCanvas) {
            this._graphCanvas = _graphCanvas;
        }

        HitTestResultBehavior NodeOrRailHitTestSelOnlyOneNodeCallback(HitTestResult result) {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (!(frameworkElement is Path))
                return HitTestResultBehavior.Continue;
            object tag = frameworkElement.Tag;
            if (tag != null) {
                GraphmapsNode graphmapsNode = tag as GraphmapsNode;
                if (graphmapsNode != null) {
                    _nodeUnderMouse = graphmapsNode;
                    return HitTestResultBehavior.Stop;
                }
                else {
                    var rail = frameworkElement.Tag as Rail;
                    if (rail != null)
                        _railUnderMouse = rail;
                }
            }

            return HitTestResultBehavior.Continue;
        }

        HitTestResultBehavior InfoHitTestSelOneResultCallback(HitTestResult result) {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null)
                return HitTestResultBehavior.Continue;

            _infoShape = frameworkElement;
            return HitTestResultBehavior.Stop;
        }

        HitTestResultBehavior RailHitTestSelOneResultCallback(HitTestResult result) {
            var frameworkElement = result.VisualHit as FrameworkElement;

            if (frameworkElement == null || !(frameworkElement.Tag is Rail))
                return HitTestResultBehavior.Continue;

            _railUnderMouse = frameworkElement.Tag as Rail;
            return HitTestResultBehavior.Stop;
        }

        public object GetNodeOrRailUnderMouse(RectangleGeometry rect) {
            _nodeUnderMouse = null;
            _railUnderMouse = null;
            VisualTreeHelper.HitTest(_graphCanvas, null,
                NodeOrRailHitTestSelOnlyOneNodeCallback,
                new GeometryHitTestParameters(rect));
            return (object)_nodeUnderMouse ?? _railUnderMouse;
        }

        public FrameworkElement GetOneInfoInsideRect(RectangleGeometry rect) {
            _infoShape = null;
            VisualTreeHelper.HitTest(_graphCanvas, null,
                InfoHitTestSelOneResultCallback,
                new GeometryHitTestParameters(rect));
            return _infoShape;
        }

        public Rail GetOneRailInsideRect(RectangleGeometry rect) {
            _railUnderMouse = null;
            VisualTreeHelper.HitTest(_graphCanvas, null,
                RailHitTestSelOneResultCallback,
                new GeometryHitTestParameters(rect));
            return _railUnderMouse;
        }

    }
}
