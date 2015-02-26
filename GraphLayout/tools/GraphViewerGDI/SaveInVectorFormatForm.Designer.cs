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
namespace Microsoft.Msagl.GraphViewerGdi {
    partial class SaveInVectorFormatForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.Windows.Forms.Control.set_Text(System.String)")]
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.saveInTextBox = new System.Windows.Forms.TextBox();
            this.saveInLabel = new System.Windows.Forms.Label();
            this.browseButton = new System.Windows.Forms.Button();
            this.saveCurrentView = new System.Windows.Forms.RadioButton();
            this.saveTotalView = new System.Windows.Forms.RadioButton();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // saveInTextBox
            // 
            this.saveInTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.saveInTextBox.Location = new System.Drawing.Point(82, 23);
            this.saveInTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.saveInTextBox.Name = "saveInTextBox";
            this.saveInTextBox.Size = new System.Drawing.Size(132, 22);
            this.saveInTextBox.TabIndex = 9;
            // 
            // saveInLabel
            // 
            this.saveInLabel.AutoSize = true;
            this.saveInLabel.Location = new System.Drawing.Point(15, 30);
            this.saveInLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.saveInLabel.Name = "saveInLabel";
            this.saveInLabel.Size = new System.Drawing.Size(59, 17);
            this.saveInLabel.TabIndex = 10;
            this.saveInLabel.Text = "Save in:";
            // 
            // browseButton
            // 
            this.browseButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.browseButton.Location = new System.Drawing.Point(235, 23);
            this.browseButton.Margin = new System.Windows.Forms.Padding(4);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(100, 28);
            this.browseButton.TabIndex = 11;
            this.browseButton.Text = "Browse";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButtonClick);
            // 
            // saveCurrentView
            // 
            this.saveCurrentView.AutoSize = true;
            this.saveCurrentView.Location = new System.Drawing.Point(31, 87);
            this.saveCurrentView.Margin = new System.Windows.Forms.Padding(4);
            this.saveCurrentView.Name = "saveCurrentView";
            this.saveCurrentView.Size = new System.Drawing.Size(141, 21);
            this.saveCurrentView.TabIndex = 12;
            this.saveCurrentView.TabStop = true;
            this.saveCurrentView.Text = "Save current view";
            this.saveCurrentView.UseVisualStyleBackColor = true;
            // 
            // saveTotalView
            // 
            this.saveTotalView.AutoSize = true;
            this.saveTotalView.Location = new System.Drawing.Point(31, 117);
            this.saveTotalView.Margin = new System.Windows.Forms.Padding(4);
            this.saveTotalView.Name = "saveTotalView";
            this.saveTotalView.Size = new System.Drawing.Size(134, 21);
            this.saveTotalView.TabIndex = 13;
            this.saveTotalView.TabStop = true;
            this.saveTotalView.Text = "Save global view";
            this.saveTotalView.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cancelButton.Location = new System.Drawing.Point(217, 214);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 28);
            this.cancelButton.TabIndex = 17;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.okButton.Location = new System.Drawing.Point(217, 179);
            this.okButton.Margin = new System.Windows.Forms.Padding(4);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(100, 28);
            this.okButton.TabIndex = 16;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // SaveInVectorFormatForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(365, 269);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.saveTotalView);
            this.Controls.Add(this.saveCurrentView);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.saveInLabel);
            this.Controls.Add(this.saveInTextBox);
            this.Name = "SaveInVectorFormatForm";
            this.Text = "SaveInVectorFormatForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox saveInTextBox;
        private System.Windows.Forms.Label saveInLabel;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.RadioButton saveCurrentView;
        private System.Windows.Forms.RadioButton saveTotalView;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.ToolTip toolTip;

    }
}
