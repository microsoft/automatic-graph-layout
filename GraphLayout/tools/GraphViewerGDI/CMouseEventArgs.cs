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
using System.Windows.Forms;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// Implements the IMouseEventArgs
    /// </summary>
    public class ViewerMouseEventArgs : Microsoft.Msagl.Drawing.MsaglMouseEventArgs {

        MouseEventArgs args;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="argsP"></param>
        public ViewerMouseEventArgs(MouseEventArgs argsP) { args = argsP; }


        /// <summary>
        /// true is the left mouse button is pressed
        /// </summary>
        override public bool LeftButtonIsPressed {
            get { return (args.Button & MouseButtons.Left) == MouseButtons.Left; }
        }
        /// <summary>
        /// true is the middle mouse button is pressed
        /// </summary>
        override public bool MiddleButtonIsPressed {
            get { return (args.Button & MouseButtons.Middle) == MouseButtons.Middle; }
        }
        /// <summary>
        /// true is the right button is pressed
        /// </summary>
        override public bool RightButtonIsPressed {
            get { return (args.Button & MouseButtons.Right) == MouseButtons.Right; }
        }

        bool handled;
        /// <summary>
        /// the controls should ignore the event if handled is set to true
        /// </summary>
        override public bool Handled {
            get { return handled; }
            set { handled = value; }
        }

        /// <summary>
        /// return x position
        /// </summary>
        override public int X {
            get { return args.X; }
        }
        /// <summary>
        /// return y position
        /// </summary>
        override public int Y {
            get { return args.Y; }
        }
        
        /// <summary>
        /// gets the number of clicks of the button
        /// </summary>
        public override int Clicks {
            get { return this.args.Clicks; }
        }
    }
}
