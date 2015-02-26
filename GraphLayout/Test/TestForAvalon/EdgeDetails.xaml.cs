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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TestForAvalon
{
    /// <summary>
    /// Interaction logic for EdgeDetails.xaml
    /// </summary>

    public partial class EdgeDetails : System.Windows.Window
    {

        public EdgeDetails()
        {
            InitializeComponent();

            this.Closing += new System.ComponentModel.CancelEventHandler(EdgeDetails_Closing);
        }

        private delegate void DoNothing();

        void EdgeDetails_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,  
                (DoNothing)delegate {
                    try
                    {
                        this.Hide();
                    }
                    catch (Exception exception) { System.Console.WriteLine(exception); }
                });
            e.Cancel = true;            
        }

        internal void TakeEdge(Microsoft.Msagl.Drawing.Edge edge)
        {
            if (edge != null)
            {
                EdgeDetailsPanel.DataContext = edge;
            }
        }
    }
}