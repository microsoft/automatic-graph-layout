using System.Collections.Generic;
using Microsoft.Msagl.Core.Geometry.Curves;

namespace Microsoft.Msagl.Layout.Layered {
    
    internal class HierarchyCalculator {

        List<ParallelogramNode> initialNodes;
        int groupSplitThreshold = 2;
        
        internal static ParallelogramNode Calculate(List<ParallelogramNode> nodes, int groupSplitThresholdPar){
            HierarchyCalculator calc=new HierarchyCalculator(nodes,groupSplitThresholdPar);
            return calc.Calculate();
        }

        HierarchyCalculator(List<ParallelogramNode> nodes, int groupSplitThresholdPar){
            initialNodes=nodes;
            groupSplitThreshold=groupSplitThresholdPar;
        }

        ParallelogramNode Calculate(){
            return Calc(initialNodes);
        }

        ParallelogramNode Calc(List<ParallelogramNode> nodes) {

            if (nodes.Count == 0)
                return null;

            if (nodes.Count == 1)
                return nodes[0];

            //Finding the seeds
            Parallelogram b0 = nodes[0].Parallelogram;

            //the first seed
            int seed0 = 1;

            double area = new Parallelogram(b0, nodes[seed0].Parallelogram).Area;
            for (int i = 2; i < nodes.Count; i++) {
                double area0 = new Parallelogram(b0, nodes[i].Parallelogram).Area;
                if (area0 > area) {
                    seed0 = i;
                    area = area0;
                }
            }

            //Got the first seed seed0
            //Now looking for a seed for the second group
            int seed1 = 0; //the compiler forces me to init it

            //init seed1
            for (int i = 0; i < nodes.Count; i++) {
                if (i != seed0) {
                    seed1 = i;
                    break;
                }
            }

            area = new Parallelogram(nodes[seed0].Parallelogram, nodes[seed1].Parallelogram).Area;
            //Now try to improve the second seed

            for (int i = 0; i < nodes.Count; i++) {
                if (i == seed0)
                    continue;
                double area1 = new Parallelogram(nodes[seed0].Parallelogram, nodes[i].Parallelogram).Area;
                if (area1 > area) {
                    seed1 = i;
                    area = area1;
                }
            }

            //We have two seeds at hand. Build two groups.
            List<ParallelogramNode> gr0 = new List<ParallelogramNode>();
            List<ParallelogramNode> gr1 = new List<ParallelogramNode>();

            gr0.Add(nodes[seed0]);
            gr1.Add(nodes[seed1]);

            Parallelogram box0 = nodes[seed0].Parallelogram;
            Parallelogram box1 = nodes[seed1].Parallelogram;
            //divide nodes on two groups
            for (int i = 0; i < nodes.Count; i++) {

                if (i == seed0 || i == seed1)
                    continue;

                Parallelogram box0_ = new Parallelogram(box0, nodes[i].Parallelogram);
                double delta0 = box0_.Area - box0.Area;

                Parallelogram box1_ = new Parallelogram(box1, nodes[i].Parallelogram);
                double delta1 = box1_.Area - box1.Area;

                //keep the tree roughly balanced

                if (gr0.Count * groupSplitThreshold < gr1.Count) {
                    gr0.Add(nodes[i]);
                    box0 = box0_;
                } else if (gr1.Count * groupSplitThreshold < gr0.Count) {
                    gr1.Add(nodes[i]);
                    box1 = box1_;
                } else if (delta0 < delta1) {
                    gr0.Add(nodes[i]);
                    box0 = box0_;
                } else {
                    gr1.Add(nodes[i]);
                    box1 = box1_;
                }

            }

            ParallelogramBinaryTreeNode ret = new ParallelogramBinaryTreeNode();
            ret.Parallelogram = new Parallelogram(box0, box1);
        
            ret.LeftSon = Calc(gr0);
            ret.RightSon = Calc(gr1);
         
            return ret;

        }
    }
}