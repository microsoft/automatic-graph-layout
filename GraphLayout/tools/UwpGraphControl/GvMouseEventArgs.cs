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
using System.Windows;
using Windows.UI.Input;
using Microsoft.Msagl.Drawing;
using PointerRoutedEventArgs = Windows.UI.Xaml.Input.PointerRoutedEventArgs;

namespace Microsoft.Msagl.Viewers.Uwp {
    internal class GvMouseEventArgs : MsaglMouseEventArgs {
        PointerPoint _point;
        PointerRoutedEventArgs _eventArgs;
        private readonly int _clicks;
        internal GvMouseEventArgs(PointerRoutedEventArgs eventArgs, GraphViewer graphScrollerP, int clicks) {
            _eventArgs = eventArgs;
            _clicks = clicks;
            _point = _eventArgs.GetCurrentPoint(graphScrollerP.GraphCanvas);
        }

        public override bool LeftButtonIsPressed => _point.Properties.IsLeftButtonPressed;
        public override bool MiddleButtonIsPressed => _point.Properties.IsMiddleButtonPressed;
        public override bool RightButtonIsPressed => _point.Properties.IsRightButtonPressed;

        public override bool Handled {
            get => _eventArgs.Handled;
            set => _eventArgs.Handled = value;
        }

        public override int X => (int)_point.Position.X;
        public override int Y => (int)_point.Position.Y;

        /// <summary>
        ///     number of clicks
        /// </summary>
        public override int Clicks => _clicks;
    }
}