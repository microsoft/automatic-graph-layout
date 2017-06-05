using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Msagl;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Routing;

namespace TestForGdi {
    partial class Form2 {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.selection = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.coneAngleNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.showVisGraphCheckBox = new System.Windows.Forms.CheckBox();
            this.gViewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            this.minimumRoutingOffset = new System.Windows.Forms.NumericUpDown();
            this.looseObstaclesOffset = new System.Windows.Forms.NumericUpDown();
            this.routingRelaxOffset = new System.Windows.Forms.NumericUpDown();
            this.demoButton = new System.Windows.Forms.Button();
            this.graphBorderSize = new System.Windows.Forms.NumericUpDown();
            this.aspectRatio = new System.Windows.Forms.NumericUpDown();
            this.nodeSeparMult = new System.Windows.Forms.NumericUpDown();
            this.layerSeparationMult = new System.Windows.Forms.NumericUpDown();
            this.searchButton = new System.Windows.Forms.Button();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.demoPauseValueNumericValue = new System.Windows.Forms.NumericUpDown();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDotFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.demoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.routingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.routingSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.overrideGraphRoutingSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.coneAngleNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minimumRoutingOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.looseObstaclesOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.routingRelaxOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.graphBorderSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.aspectRatio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeSeparMult)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layerSeparationMult)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.demoPauseValueNumericValue)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // selection
            // 
            this.selection.Location = new System.Drawing.Point(362, 5);
            this.selection.Name = "selection";
            this.selection.Size = new System.Drawing.Size(286, 22);
            this.selection.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.coneAngleNumericUpDown);
            this.panel1.Controls.Add(this.showVisGraphCheckBox);
            this.panel1.Controls.Add(this.gViewer);
            this.panel1.Controls.Add(this.minimumRoutingOffset);
            this.panel1.Controls.Add(this.looseObstaclesOffset);
            this.panel1.Controls.Add(this.routingRelaxOffset);
            this.panel1.Controls.Add(this.demoButton);
            this.panel1.Controls.Add(this.graphBorderSize);
            this.panel1.Controls.Add(this.aspectRatio);
            this.panel1.Controls.Add(this.nodeSeparMult);
            this.panel1.Controls.Add(this.layerSeparationMult);
            this.panel1.Controls.Add(this.searchButton);
            this.panel1.Controls.Add(this.searchTextBox);
            this.panel1.Controls.Add(this.demoPauseValueNumericValue);
            this.panel1.Controls.Add(this.menuStrip1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1276, 970);
            this.panel1.TabIndex = 2;
            // 
            // coneAngleNumericUpDown
            // 
            this.coneAngleNumericUpDown.Location = new System.Drawing.Point(528, 2);
            this.coneAngleNumericUpDown.Maximum = new decimal(new int[] {
            90,
            0,
            0,
            0});
            this.coneAngleNumericUpDown.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.coneAngleNumericUpDown.Name = "coneAngleNumericUpDown";
            this.coneAngleNumericUpDown.Size = new System.Drawing.Size(120, 22);
            this.coneAngleNumericUpDown.TabIndex = 25;
            this.coneAngleNumericUpDown.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // showVisGraphCheckBox
            // 
            this.showVisGraphCheckBox.AutoSize = true;
            this.showVisGraphCheckBox.Location = new System.Drawing.Point(390, 5);
            this.showVisGraphCheckBox.Name = "showVisGraphCheckBox";
            this.showVisGraphCheckBox.Size = new System.Drawing.Size(162, 21);
            this.showVisGraphCheckBox.TabIndex = 24;
            this.showVisGraphCheckBox.Text = "Show Visibility Graph";
            this.showVisGraphCheckBox.UseVisualStyleBackColor = true;
            // 
            // gViewer
            // 
            this.gViewer.ArrowheadLength = 10D;
            this.gViewer.AsyncLayout = false;
            this.gViewer.AutoScroll = true;
            this.gViewer.BackwardEnabled = false;
            this.gViewer.BuildHitTree = true;
            this.gViewer.CurrentLayoutMethod = Microsoft.Msagl.GraphViewerGdi.LayoutMethod.UseSettingsOfTheGraph;
            this.gViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gViewer.FileName = "";
            this.gViewer.ForwardEnabled = false;
            this.gViewer.Graph = null;
            this.gViewer.InsertingEdge = false;
            this.gViewer.LayoutAlgorithmSettingsButtonVisible = true;
            this.gViewer.Location = new System.Drawing.Point(0, 28);
            this.gViewer.LooseOffsetForRouting = 0.25D;
            this.gViewer.MouseHitDistance = 0.05D;
            this.gViewer.Name = "gViewer";
            this.gViewer.NavigationVisible = true;
            this.gViewer.NeedToCalculateLayout = true;
            this.gViewer.OffsetForRelaxingInRouting = 0.6D;
            this.gViewer.PaddingForEdgeRouting = 8D;
            this.gViewer.PanButtonPressed = false;
            this.gViewer.SaveAsImageEnabled = true;
            this.gViewer.SaveAsMsaglEnabled = true;
            this.gViewer.SaveButtonVisible = true;
            this.gViewer.SaveGraphButtonVisible = true;
            this.gViewer.SaveInVectorFormatEnabled = true;
            this.gViewer.Size = new System.Drawing.Size(1542, 921);
            this.gViewer.TabIndex = 19;
            this.gViewer.TightOffsetForRouting = 0.125D;
            this.gViewer.ToolBarIsVisible = true;
            this.gViewer.WindowZoomButtonPressed = false;
            this.gViewer.ZoomF = 1D;
            this.gViewer.ZoomWindowThreshold = 0.05D;
            // 
            // minimumRoutingOffset
            // 
            this.minimumRoutingOffset.DecimalPlaces = 3;
            this.minimumRoutingOffset.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.minimumRoutingOffset.Location = new System.Drawing.Point(688, 4);
            this.minimumRoutingOffset.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            65536});
            this.minimumRoutingOffset.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.minimumRoutingOffset.Name = "minimumRoutingOffset";
            this.minimumRoutingOffset.Size = new System.Drawing.Size(81, 22);
            this.minimumRoutingOffset.TabIndex = 14;
            this.minimumRoutingOffset.Value = new decimal(new int[] {
            4,
            0,
            0,
            65536});
            // 
            // looseObstaclesOffset
            // 
            this.looseObstaclesOffset.Location = new System.Drawing.Point(769, 3);
            this.looseObstaclesOffset.Name = "looseObstaclesOffset";
            this.looseObstaclesOffset.Size = new System.Drawing.Size(81, 22);
            this.looseObstaclesOffset.TabIndex = 13;
            this.looseObstaclesOffset.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // routingRelaxOffset
            // 
            this.routingRelaxOffset.Location = new System.Drawing.Point(853, 5);
            this.routingRelaxOffset.Name = "routingRelaxOffset";
            this.routingRelaxOffset.Size = new System.Drawing.Size(81, 22);
            this.routingRelaxOffset.TabIndex = 12;
            this.routingRelaxOffset.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // demoButton
            // 
            this.demoButton.Location = new System.Drawing.Point(1478, 5);
            this.demoButton.Name = "demoButton";
            this.demoButton.Size = new System.Drawing.Size(64, 23);
            this.demoButton.TabIndex = 3;
            this.demoButton.Text = "Demo";
            this.demoButton.UseVisualStyleBackColor = true;
            this.demoButton.Click += new System.EventHandler(this.DemoButtonClick);
            // 
            // graphBorderSize
            // 
            this.graphBorderSize.Location = new System.Drawing.Point(940, 5);
            this.graphBorderSize.Name = "graphBorderSize";
            this.graphBorderSize.Size = new System.Drawing.Size(81, 22);
            this.graphBorderSize.TabIndex = 11;
            this.graphBorderSize.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // aspectRatio
            // 
            this.aspectRatio.DecimalPlaces = 2;
            this.aspectRatio.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.aspectRatio.Location = new System.Drawing.Point(1388, 7);
            this.aspectRatio.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.aspectRatio.Name = "aspectRatio";
            this.aspectRatio.Size = new System.Drawing.Size(84, 22);
            this.aspectRatio.TabIndex = 10;
            // 
            // nodeSeparMult
            // 
            this.nodeSeparMult.DecimalPlaces = 2;
            this.nodeSeparMult.Location = new System.Drawing.Point(1152, 7);
            this.nodeSeparMult.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nodeSeparMult.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nodeSeparMult.Name = "nodeSeparMult";
            this.nodeSeparMult.Size = new System.Drawing.Size(84, 22);
            this.nodeSeparMult.TabIndex = 9;
            this.nodeSeparMult.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // layerSeparationMult
            // 
            this.layerSeparationMult.DecimalPlaces = 2;
            this.layerSeparationMult.Location = new System.Drawing.Point(1242, 8);
            this.layerSeparationMult.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.layerSeparationMult.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.layerSeparationMult.Name = "layerSeparationMult";
            this.layerSeparationMult.Size = new System.Drawing.Size(84, 22);
            this.layerSeparationMult.TabIndex = 8;
            this.layerSeparationMult.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // searchButton
            // 
            this.searchButton.Location = new System.Drawing.Point(1083, 5);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(63, 23);
            this.searchButton.TabIndex = 5;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.SearchButtonClick);
            // 
            // searchTextBox
            // 
            this.searchTextBox.Location = new System.Drawing.Point(1022, 5);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(58, 22);
            this.searchTextBox.TabIndex = 4;
            // 
            // demoPauseValueNumericValue
            // 
            this.demoPauseValueNumericValue.Location = new System.Drawing.Point(652, 2);
            this.demoPauseValueNumericValue.Name = "demoPauseValueNumericValue";
            this.demoPauseValueNumericValue.Size = new System.Drawing.Size(30, 22);
            this.demoPauseValueNumericValue.TabIndex = 1;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.routingToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1542, 28);
            this.menuStrip1.TabIndex = 18;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openDotFileToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.cancelLayoutToolStripMenuItem,
            this.demoToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openDotFileToolStripMenuItem
            // 
            this.openDotFileToolStripMenuItem.Name = "openDotFileToolStripMenuItem";
            this.openDotFileToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.openDotFileToolStripMenuItem.Text = "Open Dot file";
            this.openDotFileToolStripMenuItem.Click += new System.EventHandler(this.OpenDotFileToolStripMenuItemClick);
            // 
            // reloadToolStripMenuItem
            // 
            this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            this.reloadToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.reloadToolStripMenuItem.Text = "Reload";
            this.reloadToolStripMenuItem.Click += new System.EventHandler(this.ReloadToolStripMenuItemClick);
            // 
            // cancelLayoutToolStripMenuItem
            // 
            this.cancelLayoutToolStripMenuItem.Name = "cancelLayoutToolStripMenuItem";
            this.cancelLayoutToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.cancelLayoutToolStripMenuItem.Text = "Cancel layout";
            this.cancelLayoutToolStripMenuItem.Click += new System.EventHandler(this.CancelLayoutToolStripMenuItemClick);
            // 
            // demoToolStripMenuItem
            // 
            this.demoToolStripMenuItem.Name = "demoToolStripMenuItem";
            this.demoToolStripMenuItem.Size = new System.Drawing.Size(168, 24);
            this.demoToolStripMenuItem.Text = "Demo";
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(47, 24);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // routingToolStripMenuItem
            // 
            this.routingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.routingSettingsToolStripMenuItem,
            this.overrideGraphRoutingSettingsToolStripMenuItem});
            this.routingToolStripMenuItem.Name = "routingToolStripMenuItem";
            this.routingToolStripMenuItem.Size = new System.Drawing.Size(73, 24);
            this.routingToolStripMenuItem.Text = "Routing";
            // 
            // routingSettingsToolStripMenuItem
            // 
            this.routingSettingsToolStripMenuItem.Name = "routingSettingsToolStripMenuItem";
            this.routingSettingsToolStripMenuItem.Size = new System.Drawing.Size(292, 24);
            this.routingSettingsToolStripMenuItem.Text = "Routing Settings";
            this.routingSettingsToolStripMenuItem.Click += new System.EventHandler(this.RoutingSettingsToolStripMenuItemClick);
            // 
            // overrideGraphRoutingSettingsToolStripMenuItem
            // 
            this.overrideGraphRoutingSettingsToolStripMenuItem.CheckOnClick = true;
            this.overrideGraphRoutingSettingsToolStripMenuItem.Name = "overrideGraphRoutingSettingsToolStripMenuItem";
            this.overrideGraphRoutingSettingsToolStripMenuItem.Size = new System.Drawing.Size(292, 24);
            this.overrideGraphRoutingSettingsToolStripMenuItem.Text = "Override Graph Routing Settings";
            this.overrideGraphRoutingSettingsToolStripMenuItem.Click += new System.EventHandler(this.OverrideGaphRoutingSettingsToolStripMenuItemClick);
            // 
            // Form2
            // 
            this.ClientSize = new System.Drawing.Size(1276, 970);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.selection);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form2";
            this.Text = "Form2";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.coneAngleNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minimumRoutingOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.looseObstaclesOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.routingRelaxOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.graphBorderSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.aspectRatio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeSeparMult)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layerSeparationMult)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.demoPauseValueNumericValue)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox selection;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button demoButton;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.NumericUpDown demoPauseValueNumericValue;
        internal System.Windows.Forms.NumericUpDown layerSeparationMult;
        internal System.Windows.Forms.NumericUpDown nodeSeparMult;
        internal System.Windows.Forms.NumericUpDown aspectRatio;
        private System.Windows.Forms.NumericUpDown graphBorderSize;
        private System.Windows.Forms.NumericUpDown looseObstaclesOffset;
        private System.Windows.Forms.NumericUpDown routingRelaxOffset;
        protected System.Windows.Forms.NumericUpDown minimumRoutingOffset;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDotFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem demoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private Microsoft.Msagl.GraphViewerGdi.GViewer gViewer;
        private System.Windows.Forms.NumericUpDown coneAngleNumericUpDown;

        private CheckBox showVisGraphCheckBox;
        private ToolStripMenuItem routingToolStripMenuItem;
        private ToolStripMenuItem routingSettingsToolStripMenuItem;
        private ToolStripMenuItem overrideGraphRoutingSettingsToolStripMenuItem;
    }
}
