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
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public partial class LabelEditor : UserControl
    {
        public LabelEditor()
        {
            InitializeComponent();
        }

        public DObject EditTarget { get; set; }

        private FrameworkElement _EditControl;
        public FrameworkElement EditControl
        {
            get
            {
                return _EditControl;
            }
            set
            {
                if (_EditControl != null)
                    LayoutRoot.Children.Remove(_EditControl);
                _EditControl = value;
                LayoutRoot.Children.Add(_EditControl);
            }
        }

        public event EventHandler Closed;

        public bool OK { get; set; }

        public void Close(bool ok)
        {
            OK = ok;
            if (Closed != null)
                Closed(this, EventArgs.Empty);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            OK = true;
            if (Closed != null)
                Closed(this, EventArgs.Empty);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OK = false;
            if (Closed != null)
                Closed(this, EventArgs.Empty);
        }
    }
}

