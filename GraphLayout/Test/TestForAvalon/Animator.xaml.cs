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
using System.Linq;
using System.Windows.Threading;
using Microsoft.Msagl.ControlForWpfObsolete;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Drawing;

namespace TestForAvalon {
    internal class Animator {
        readonly GraphScroller _viewer;
        DispatcherTimer _timer;
        const int Interval = 5; //seconds
        readonly IViewerObject[] _nodes;
        readonly Random _random=new Random(1);

        public Animator(GraphScroller viewer) {
            _viewer = viewer;
            _nodes = _viewer.Entities.Where(e => e is IViewerNode).ToArray();
        }


        public void Stop() {
            _timer.Stop();
        }

       
       
        public void Start() {

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0,0,0,0,Interval*1000);            
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        void TimerTick(object sender, EventArgs e) {
            int i = _random.Next(_nodes.Length);
            var nodeShape = _nodes[i];
            _viewer.ScrollIntoView((NodeShape)nodeShape);
        }
    }
}