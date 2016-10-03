namespace SettingGraphBoundsSample {
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
            this.viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            this.simpleStretchCheckBox = new System.Windows.Forms.CheckBox();
            this.aspectRatioUpDown = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.MinWidth = new System.Windows.Forms.NumericUpDown();
            this.MinHeight = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.aspectRatioUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MinWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MinHeight)).BeginInit();
            this.SuspendLayout();
            // 
            // viewer
            // 
            this.viewer.ArrowheadLength = 10;
            this.viewer.AsyncLayout = false;
            this.viewer.AutoScroll = true;
            this.viewer.BackwardEnabled = false;
            this.viewer.BuildHitTree = true;
            this.viewer.CurrentLayoutMethod = Microsoft.Msagl.GraphViewerGdi.LayoutMethod.UseSettingsOfTheGraph;
            this.viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewer.FileName = "";
            this.viewer.ForwardEnabled = false;
            this.viewer.Graph = null;
            this.viewer.InsertingEdge = false;
            this.viewer.LayoutAlgorithmSettingsButtonVisible = true;
            this.viewer.Location = new System.Drawing.Point(0, 0);
            this.viewer.LooseOffsetForRouting = 0.25;
            this.viewer.MouseHitDistance = 0.05;
            this.viewer.Name = "viewer";
            this.viewer.NavigationVisible = true;
            this.viewer.NeedToCalculateLayout = true;
            this.viewer.OffsetForRelaxingInRouting = 0.6;
            this.viewer.PaddingForEdgeRouting = 8;
            this.viewer.PanButtonPressed = false;
            this.viewer.SaveAsImageEnabled = true;
            this.viewer.SaveAsMsaglEnabled = true;
            this.viewer.SaveButtonVisible = true;
            this.viewer.SaveGraphButtonVisible = true;
            this.viewer.SaveInVectorFormatEnabled = true;
            this.viewer.Size = new System.Drawing.Size(1188, 629);
            this.viewer.TabIndex = 0;
            this.viewer.TightOffsetForRouting = 0.125;
            this.viewer.ToolBarIsVisible = true;
            this.viewer.WindowZoomButtonPressed = false;
            this.viewer.ZoomF = 1;
            this.viewer.ZoomWindowThreshold = 0.05;
            this.viewer.Load += new System.EventHandler(this.gViewer1_Load);
            // 
            // simpleStretchCheckBox
            // 
            this.simpleStretchCheckBox.AutoSize = true;
            this.simpleStretchCheckBox.Checked = true;
            this.simpleStretchCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.simpleStretchCheckBox.Location = new System.Drawing.Point(331, 7);
            this.simpleStretchCheckBox.Name = "simpleStretchCheckBox";
            this.simpleStretchCheckBox.Size = new System.Drawing.Size(94, 17);
            this.simpleStretchCheckBox.TabIndex = 1;
            this.simpleStretchCheckBox.Text = "Simple Stretch";
            this.simpleStretchCheckBox.UseVisualStyleBackColor = true;
            // 
            // aspectRatioUpDown
            // 
            this.aspectRatioUpDown.DecimalPlaces = 1;
            this.aspectRatioUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.aspectRatioUpDown.Location = new System.Drawing.Point(431, 7);
            this.aspectRatioUpDown.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.aspectRatioUpDown.Name = "aspectRatioUpDown";
            this.aspectRatioUpDown.Size = new System.Drawing.Size(75, 20);
            this.aspectRatioUpDown.TabIndex = 2;
            this.aspectRatioUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(575, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "MinWidth";
            // 
            // MinWidth
            // 
            this.MinWidth.DecimalPlaces = 1;
            this.MinWidth.Location = new System.Drawing.Point(645, 11);
            this.MinWidth.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.MinWidth.Name = "MinWidth";
            this.MinWidth.Size = new System.Drawing.Size(75, 20);
            this.MinWidth.TabIndex = 4;
            // 
            // MinHeight
            // 
            this.MinHeight.DecimalPlaces = 1;
            this.MinHeight.Location = new System.Drawing.Point(825, 12);
            this.MinHeight.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.MinHeight.Name = "MinHeight";
            this.MinHeight.Size = new System.Drawing.Size(75, 20);
            this.MinHeight.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(755, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "MinHeight";
            // 
            // button1
            // 
            this.button1.AutoSize = true;
            this.button1.ForeColor = System.Drawing.Color.Blue;
            this.button1.Location = new System.Drawing.Point(932, 8);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(109, 23);
            this.button1.TabIndex = 7;
            this.button1.Text = "Recalculate Layout";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1188, 629);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.MinHeight);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.MinWidth);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.aspectRatioUpDown);
            this.Controls.Add(this.simpleStretchCheckBox);
            this.Controls.Add(this.viewer);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.aspectRatioUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MinWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MinHeight)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.Msagl.GraphViewerGdi.GViewer viewer;
        private System.Windows.Forms.CheckBox simpleStretchCheckBox;
        private System.Windows.Forms.NumericUpDown aspectRatioUpDown;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown MinWidth;
        private System.Windows.Forms.NumericUpDown MinHeight;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
    }
}

