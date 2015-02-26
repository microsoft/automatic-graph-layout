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
﻿namespace Editing {
    partial class Form1 {
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
        private void InitializeComponent() {
            this.helpButton = new System.Windows.Forms.Button();
            this.graphEditor.SuspendLayout();
            this.SuspendLayout();
            // 
            // graphEditor1
            // 
            this.graphEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphEditor.Location = new System.Drawing.Point(0, 0);
            this.graphEditor.Name = "graphEditor1";
            this.graphEditor.Size = new System.Drawing.Size(900, 549);
            this.graphEditor.TabIndex = 0;
            // 
            // button1
            // 
            this.helpButton.Location = new System.Drawing.Point(370, 26);
            this.helpButton.Name = "button1";
            this.helpButton.Size = new System.Drawing.Size(150, 23);
            this.helpButton.TabIndex = 2;
            this.helpButton.Text = "How it works";
            this.helpButton.UseVisualStyleBackColor = true;            
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 549);
            this.Controls.Add(this.graphEditor);
            this.Controls.Add(helpButton);
            this.Name = "Form1";
            this.Text = "Form1";
            this.graphEditor.ResumeLayout(false);
            this.graphEditor.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Editing.GraphEditor graphEditor=new GraphEditor();
        private System.Windows.Forms.Button helpButton;
    }
}

