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

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// Mouse button enum
    /// </summary>
    [Flags]
    public enum MouseButtons {
       /// <summary>
       /// No button was pressed
       /// </summary>
        None = 0,
        /// <summary>
        /// The left mouse button was pressed.
        /// </summary>
        Left = 1048576,
        /// <summary>
        /// The right mouse button was pressed.
        /// </summary>
        Right = 2097152,
        
        //
        /// <summary>
        ///The middle mouse button was pressed. 
        /// </summary>
        Middle = 4194304,
        
        /// <summary>
        ///  The first XButton was pressed.
        /// </summary>
        XButton1 = 8388608,
        /// <summary>
        ///    The second XButton was pressed.
        /// </summary>
        XButton2 = 16777216,

    }
}
