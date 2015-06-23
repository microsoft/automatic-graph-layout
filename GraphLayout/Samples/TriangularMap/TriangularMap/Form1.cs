using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WindowsFormsApplication3
{
    public partial class Form1 : Form
    {
        Graph _graph;
        
        public int num_nodes = 40;
        public int grid_size = 40;
        public int tilingfactor = 20;
        public int pointsPerLevel = 10;
        public Network G;
        Tiling  grid;
        PointSet points;
        
        public Form1()
        {
            InitializeComponent();
            this.SuspendLayout();
            var loadFileButton = new Button();
            loadFileButton.Location = new System.Drawing.Point(1, 1);
            loadFileButton.Name = "button1";
            loadFileButton.Size = new System.Drawing.Size(20, 20);
            loadFileButton.TabIndex = 2;
            loadFileButton.Text = "Load graph";
            loadFileButton.UseVisualStyleBackColor = true;
            loadFileButton.Click += new System.EventHandler(loadFileButtonClick);
            
            Controls.Add(loadFileButton);
            this.ResumeLayout();
            this.Width = 1000;
            this.Height = 1000;
            this.DoubleBuffered = true;
        }

        private void loadFileButtonClick(object sender, EventArgs e)
        {
 
            var openFileDialog = new OpenFileDialog { RestoreDirectory = true, Filter = "MSAGL Files(*.msagl)|*.msagl" };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var FileName = openFileDialog.FileName;
                _graph = Graph.Read(openFileDialog.FileName);
                foreach (var node in _graph.Nodes)
                {
                    //Console.WriteLine(node.GeometryNode.Center);

                }
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
             /*
            Console.WriteLine(" 1 1 1 2 " + Math.Abs(Math.Atan2(1 , 1) - Math.Atan2(1 , 2)));
            Console.WriteLine(" 1 1 1 -2 " + Math.Abs(Math.Atan2(1, 1) - Math.Atan2(1, -2)));
            Console.WriteLine(" 1 1 -1 2 " + Math.Abs(Math.Atan2(1, 1) - Math.Atan2(-1, 2)));
            Console.WriteLine(" 1 1 -1 -2 " + Math.Abs(Math.Atan2(1, 1) - Math.Atan2(-1, -2)));
            */
            //create the triangular tiling of the plane
            Console.WriteLine("Create the triangular tiling of the plane.");
            grid = new Tiling(grid_size);
            //select subset of points for placing nodes
            Console.WriteLine("Create point set.");
            points = new PointSet(num_nodes, grid, pointsPerLevel); 
            //compute the weight of the grid edges
            Console.WriteLine("Compute the density based point weights.");
            grid.ComputeGridEdgeWeights();
            Console.WriteLine("Compute a random graph.");
            //CREATE a graph with x vertices and y edges
            G = new Network(num_nodes,   (int)(num_nodes * Math.Sqrt(num_nodes)));
            //plot some edges
            Console.WriteLine("Draw the recursive steiner routes.");
            grid.PlotAllEdges(points.Pt, G, points.NumPoints, points.NumOfLevels);
            this.Invalidate();
        }

 
    }


 

    //drawing things
    public partial class Form1 : Form
    {
        System.Drawing.Color[] col = new System.Drawing.Color[20];
        float[] thickness = new float[20];
        protected override void OnPaint(PaintEventArgs paintEvnt)
        {
            col[1] = System.Drawing.Color.Black;
            col[2] = System.Drawing.Color.Blue;
            col[3] = System.Drawing.Color.DarkGoldenrod;
            col[4] = System.Drawing.Color.DarkMagenta;
            col[5] = System.Drawing.Color.Orange;
            col[6] = System.Drawing.Color.Violet;
            col[7] = System.Drawing.Color.Chocolate;
            col[8] = System.Drawing.Color.DarkViolet ;
            thickness[1] = 4F;
            thickness[2] = 3.5F;
            thickness[3] = 3F;
            thickness[4] = 2.5F;
            thickness[5] = 2F;
            thickness[6] = 1.5F;
            thickness[7] = 4F;
            thickness[8] = 1F;

            // Get the graphics object 
            Graphics gfx = paintEvnt.Graphics;
            // Create a new pen that we shall use for drawing the line 
            Pen myPen = new Pen(System.Drawing.Color.Black);
            //myPen.Width = 18.0F;


            
            // Create solid brush.
            SolidBrush pointBrush = new SolidBrush(System.Drawing.Color.Red);
            //draw the points on the grid
            for (int index = 1; index <= points.NumPoints; index++)
            {
                //pointBrush = new SolidBrush(System.Drawing.Color.FromArgb(points.Pt[index].Weight,System.Drawing.Color.Red));
                gfx.FillEllipse(pointBrush, tilingfactor * points.Pt[index].X - 5, tilingfactor * points.Pt[index].Y - 5, 8, 8);
            }
             // draw the edges of the grid
            int neighbor;
            for (int index = 1; index <= grid.NumOfnodes; index++) 
            {
                if (grid.VList[index].CId>0 && grid.VList[index].invalid == false) gfx.DrawString("" + grid.VList[index].Id, new Font(FontFamily.GenericSansSerif, 7, FontStyle.Regular),
                    new SolidBrush(System.Drawing.Color.Black), grid.VList[index].XLoc * tilingfactor, grid.VList[index].YLoc * tilingfactor);
 
                for (int neighb = 1; neighb <= grid.DegList[index]; neighb++)
                {
                    neighbor = grid.eList[index,neighb].NodeId;
                    //if((grid.eList[index, neighb].weight / grid.maxweight) * 255 <60) continue;

                    myPen = new Pen(System.Drawing.Color.FromArgb((int)(grid.eList[index, neighb].Weight * 255 / grid.maxweight),                        System.Drawing.Color.Green));
                    //myPen.Width =0.2F;
                    //gfx.DrawLine(myPen, tilingfactor * grid.vList[index].x_loc, tilingfactor * grid.vList[index].y_loc,
                            //tilingfactor * grid.vList[neighbor].x_loc, tilingfactor * grid.vList[neighbor].y_loc);

                    if (grid.eList[index, neighb].Used >= 1) {
                        myPen = new Pen(col[grid.eList[index, neighb].Selected]);
                        myPen.Width = thickness[grid.eList[index, neighb].Selected];// -2 * (grid.eList[index, neighb].selected / points.numOfLevels);
                        gfx.DrawLine(myPen, tilingfactor * grid.VList[index].XLoc, tilingfactor * grid.VList[index].YLoc,
                                tilingfactor * grid.VList[neighbor].XLoc, tilingfactor * grid.VList[neighbor].YLoc);
                    }
                }
            }




            //draw the shortest paths on the grid
        } 

    }

}
