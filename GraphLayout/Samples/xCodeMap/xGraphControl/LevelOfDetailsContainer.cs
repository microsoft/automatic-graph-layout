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
