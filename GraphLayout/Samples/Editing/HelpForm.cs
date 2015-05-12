using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Editing {
    public partial class HelpForm : Form {
        public HelpForm() {
            InitializeComponent();
        }
        internal RichTextBox RichTectBox {
            get { return this.richTextBox1; }
        }
    }

  
}
