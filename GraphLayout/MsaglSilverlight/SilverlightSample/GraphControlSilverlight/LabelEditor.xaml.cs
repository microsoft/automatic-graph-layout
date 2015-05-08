using System;
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

