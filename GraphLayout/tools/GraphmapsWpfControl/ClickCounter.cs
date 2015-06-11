using System;
using System.Windows;
using Microsoft.Msagl.Drawing;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    internal class ClickCounter {
        readonly Func<Point> mousePosition;
        internal object ClickedObject;
        internal bool IsRunning { get;  set; }

        internal ClickCounter(Func<Point> mousePosition) {
            this.mousePosition = mousePosition;
            _clickTimer.Tick += TimeTick;
            _clickTimer.Interval = TimeSpan.FromMilliseconds(500);

        }

        internal int DownCount { get;  set; }
        internal int UpCount { get;  set; }
        readonly System.Windows.Threading.DispatcherTimer _clickTimer = new System.Windows.Threading.DispatcherTimer();
        internal Point LastDownClickPosition;

        internal void AddMouseDown() {
            if (!IsRunning) {
                DownCount = 0;
                UpCount = 0;
                _clickTimer.Start();
                IsRunning = true;

            }
            LastDownClickPosition = this.mousePosition();
            DownCount++;
        }

        internal void AddMouseUp() {
            const double minDistanceForClickDownAndUp = 0.1;
            if (IsRunning) {
                if ((mousePosition() - LastDownClickPosition).Length > minDistanceForClickDownAndUp) {
                    //it is not a click
                    UpCount = 0;
                    DownCount = 0;
                    _clickTimer.Stop();
                    IsRunning = false;
                }
                else
                    UpCount++;
            }
        }


        void TimeTick(object sender, EventArgs e) {
            _clickTimer.Stop();
            IsRunning = false;
            OnElapsed();
        }

        public event EventHandler<EventArgs> Elapsed;

        protected virtual void OnElapsed() {
            EventHandler<EventArgs> handler = Elapsed;
            if (handler != null) 
                handler(this, EventArgs.Empty);
        }
    }
}