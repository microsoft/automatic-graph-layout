using System;
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