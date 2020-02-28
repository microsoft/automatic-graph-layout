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
using System;
using WPoint = Windows.Foundation.Point;
using WDispatcherTimer = Windows.UI.Xaml.DispatcherTimer;
namespace Microsoft.Msagl.Viewers.Uwp {
    internal class ClickCounter {
        readonly Func<WPoint> mousePosition;
        internal object ClickedObject;
        internal bool IsRunning { get; private set; }

        internal ClickCounter(Func<WPoint> mousePosition) {
            this.mousePosition = mousePosition;
            clickTimer.Tick += TimeTick;
            clickTimer.Interval = TimeSpan.FromMilliseconds(500);

        }

        internal int DownCount { get; private set; }
        internal int UpCount { get; private set; }
        readonly WDispatcherTimer clickTimer = new WDispatcherTimer();
        internal WPoint LastDownClickPosition;

        internal void AddMouseDown(object objectUnderMouseCursor) {
            if (!IsRunning) {
                DownCount = 0;
                UpCount = 0;
                clickTimer.Start();
                IsRunning = true;

            }
            LastDownClickPosition = this.mousePosition();
            ClickedObject = objectUnderMouseCursor;
            DownCount++;
        }

        internal void AddMouseUp() {
            const double minDistanceForClickDownAndUp = 0.1;
            if (IsRunning) {
                if (GetLength(mousePosition(), LastDownClickPosition) > minDistanceForClickDownAndUp) {
                    //it is not a click
                    UpCount = 0;
                    DownCount = 0;
                    clickTimer.Stop();
                    IsRunning = false;
                }
                else
                    UpCount++;
            }
        }

        private double GetLength(WPoint a, WPoint b) {
            (double x, double y) = (a.X - b.X, a.Y - b.Y);
            return Math.Sqrt(x * x + y * y);
        }


        void TimeTick(object sender, object e) {
            clickTimer.Stop();
            IsRunning = false;
            OnElapsed();
        }

        public event EventHandler<EventArgs> Elapsed;

        protected virtual void OnElapsed() {
            Elapsed?.Invoke(this, EventArgs.Empty);
        }
    }
}