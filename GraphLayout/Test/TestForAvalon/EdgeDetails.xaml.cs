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