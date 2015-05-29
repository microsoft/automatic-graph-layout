using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dot2Graph;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval;
using Microsoft.Msagl.Drawing;

namespace FindOverlapSample {
    class DotLoader {

        public static GeometryGraph LoadFile(String fileName) {
            int line, column;
            string msg;
            Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);
//            TestGraph(gwgraph);
            if (gwgraph != null) {
                return gwgraph.GeometryGraph;
            } else
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
            return null;
        }
        public static Graph LoadGraphFile(String fileName) {
            int line, column;
            string msg;
            Graph gwgraph = Parser.Parse(fileName, out line, out column, out msg);
            //            TestGraph(gwgraph);
            if (gwgraph != null) {
                return gwgraph;
            } else
                MessageBox.Show(msg + String.Format(" line {0} column {1}", line, column));
            return null;
        }
    }
}
