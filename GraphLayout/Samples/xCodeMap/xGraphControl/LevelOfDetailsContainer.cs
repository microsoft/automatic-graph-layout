using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Rectangle = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace xCodeMap.xGraphControl
{
    internal class LevelOfDetailsContainer : FrameworkElement
    {
        private VisualCollection _children;
        private List<Visual> _visuals;
        private List<Rectangle> _boundingBoxes;
        public Rectangle BoundingBox;

        public int MaxLevelOfDetail
        {
            get { return _visuals.Count; }
        }

        public LevelOfDetailsContainer()
        {
            _children = new VisualCollection(this);
            //CacheMode = new BitmapCache { RenderAtScale = 1, EnableClearType = true, SnapsToDevicePixels = true };

            _visuals = new List<Visual>();
            _boundingBoxes = new List<Rectangle>();
        }

        public void AddDetail(Visual v, Rectangle bounding_box)
        {
            if (v != null)
            {
                _visuals.Add(v);

                Rectangle last_box;
                if (_boundingBoxes.Count > 0)
                {
                    last_box = _boundingBoxes[_boundingBoxes.Count - 1];
                    bounding_box.Add(last_box);
                }

                _boundingBoxes.Add(bounding_box);
            }
        }

        public int MeasureLevelOfDetail(Size container)
        {
            int LOD;
            for (LOD = 0; LOD < _boundingBoxes.Count; LOD++)
            {
                Rectangle box = _boundingBoxes[LOD];
                if (container.Width < box.Width || container.Height < box.Height || container.Width < box.Right || container.Height < box.Bottom) break;
            }
            return LOD;
        }

        private int _levelOfDetail = 0;
        public int LevelOfDetail
        {
            get { return _levelOfDetail; }
            set
            {
                if (_levelOfDetail != value && value <= MaxLevelOfDetail)
                {
                    for (int i = _levelOfDetail; i < value; i++)
                    {
                        _children.Add(_visuals[i]);
                    }
                    for (int i = value; i < _levelOfDetail; i++)
                    {
                        _children.Remove(_visuals[i]);
                    }

                    if (value == 0)
                    {
                        this.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        this.Visibility = Visibility.Visible;
                        BoundingBox = _boundingBoxes[value - 1];
                    }

                    _levelOfDetail = value;
                }
            }
        }

        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return _children[index];
        }
    }
}
