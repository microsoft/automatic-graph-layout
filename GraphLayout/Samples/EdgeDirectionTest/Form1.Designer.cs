using Microsoft.Msagl.GraphViewerGdi;
namespace EdgeDirectionTest
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private GViewer gv = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.gv = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // gv
            // 
            this.gv.ArrowheadLength = 10D;
            this.gv.AsyncLayout = false;
            this.gv.AutoScroll = true;
            this.gv.BackColor = System.Drawing.Color.Blue;
            this.gv.BackwardEnabled = false;
            this.gv.BuildHitTree = true;
            this.gv.CurrentLayoutMethod = Microsoft.Msagl.GraphViewerGdi.LayoutMethod.UseSettingsOfTheGraph;
            this.gv.EdgeInsertButtonVisible = true;
            this.gv.FileName = "";
            this.gv.ForwardEnabled = false;
            this.gv.Graph = null;
            this.gv.IncrementalDraggingModeAlways = false;
            this.gv.InsertingEdge = false;
            this.gv.LayoutAlgorithmSettingsButtonVisible = true;
            this.gv.LayoutEditingEnabled = true;
            this.gv.Location = new System.Drawing.Point(0, 0);
            this.gv.LooseOffsetForRouting = 0.25D;
            this.gv.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.gv.MouseHitDistance = 0.05D;
            this.gv.Name = "gv";
            this.gv.NavigationVisible = true;
            this.gv.NeedToCalculateLayout = true;
            this.gv.OffsetForRelaxingInRouting = 0.6D;
            this.gv.PaddingForEdgeRouting = 8D;
            this.gv.PanButtonPressed = false;
            this.gv.SaveAsImageEnabled = true;
            this.gv.SaveAsMsaglEnabled = true;
            this.gv.SaveButtonVisible = true;
            this.gv.SaveGraphButtonVisible = true;
            this.gv.SaveInVectorFormatEnabled = true;
            this.gv.Size = new System.Drawing.Size(1248, 842);
            this.gv.TabIndex = 0;
            this.gv.TightOffsetForRouting = 0.125D;
            this.gv.ToolBarIsVisible = true;
            this.gv.Transform = ((Microsoft.Msagl.Core.Geometry.Curves.PlaneTransformation)(resources.GetObject("gv.Transform")));
            this.gv.UndoRedoButtonsVisible = true;
            this.gv.WindowZoomButtonPressed = false;
            this.gv.ZoomF = 1D;
            this.gv.ZoomWindowThreshold = 0.05D;
            this.gv.EdgeAdded += new System.EventHandler(this.gv_EdgeAdded);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(685, 0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(228, 40);
            this.button1.TabIndex = 1;
            this.button1.Text = "Remove  (1 -> 2)";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 865);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.gv);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
    }
}

