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
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// Implements the IMouseEventArgs
    /// </summary>
    public class ViewerMouseEventArgs : Microsoft.Msagl.Drawing.MsaglMouseEventArgs
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="argsP"></param>
        public ViewerMouseEventArgs(int x, int y, bool leftb, bool middleb, bool rightb, int clicks)
            : base()
        {
            _X = x;
            _Y = y;
            _LeftButtonIsPressed = leftb;
            _MiddleButtonIsPressed = middleb;
            _RightButtonIsPressed = rightb;
            _Clicks = clicks;
        }

        private bool _LeftButtonIsPressed;
        /// <summary>
        /// true is the left mouse button is pressed
        /// </summary>
        override public bool LeftButtonIsPressed { get { return _LeftButtonIsPressed; } }

        private bool _MiddleButtonIsPressed;
        /// <summary>
        /// true is the middle mouse button is pressed
        /// </summary>
        override public bool MiddleButtonIsPressed { get { return _MiddleButtonIsPressed; } }

        private bool _RightButtonIsPressed;
        /// <summary>
        /// true is the right button is pressed
        /// </summary>
        override public bool RightButtonIsPressed { get { return _RightButtonIsPressed; } }

        /// <summary>
        /// the controls should ignore the event if handled is set to true
        /// </summary>
        override public bool Handled { get; set; }

        private int _X;
        /// <summary>
        /// return x position
        /// </summary>
        override public int X { get { return _X; } }

        private int _Y;
        /// <summary>
        /// return y position
        /// </summary>
        override public int Y { get { return _Y; } }

       
        private int _Clicks;
        /// <summary>
        /// gets the number of clicks of the button
        /// </summary>
        public override int Clicks { get { return _Clicks; } }
    }
}