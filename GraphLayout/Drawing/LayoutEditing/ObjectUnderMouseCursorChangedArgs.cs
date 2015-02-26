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

namespace Microsoft.Msagl.Drawing {
    /// <summary>
    /// arguments for the event of changing the object under the mouse cursor
    /// </summary>
    public class ObjectUnderMouseCursorChangedEventArgs:EventArgs {
        IViewerObject oldObject;
        /// <summary>
        /// The old object under the mouse
        /// </summary>
        public IViewerObject OldObject {
            get { return oldObject; }
            set { oldObject = value; }
        }
        IViewerObject newObject;

        /// <summary>
        /// the new object under the mouse
        /// </summary>
        public IViewerObject NewObject {
            get { return newObject; }
            set { newObject = value; }
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="oldObject"></param>
        /// <param name="newObject"></param>
        public ObjectUnderMouseCursorChangedEventArgs(IViewerObject oldObject, IViewerObject newObject) {
            OldObject = oldObject;
            NewObject = newObject;
        }
        /// <summary>
        /// an empty constructor
        /// </summary>
        public ObjectUnderMouseCursorChangedEventArgs() {}
    }
}
