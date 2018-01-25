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
using System;
using System.Collections.Generic;
using Microsoft.Msagl.Drawing;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace Microsoft.Msagl.GraphViewerGdi {
    /// <summary>
    /// Build a spatial hierarchy
    /// </summary>
    internal class SpatialAlgorithm {
        // we will sort the geometries array
     
        List<ObjectWithBox> geometries;
        internal SpatialAlgorithm(List<ObjectWithBox> geometries) {
            this.geometries = geometries;
        }


        static internal BBNode CreateBBNodeOnGeometries(List<ObjectWithBox> geoms){
            SpatialAlgorithm sa=new SpatialAlgorithm(geoms);
            return sa.CalcHierachy();
        }
        
        /// <summary>
        /// first and count denine the segment  needed to be split into two groups
        /// </summary>
        /// <param name="first"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        BBNode ProcessGroup(int first, int count) {
            if (count > 1) {
                int seed0;
                BBox b0;
                int seed1;
                FindSeeds(first, count, out seed0, out b0, out seed1);
                BBox b1;
                int count0;
                int count1;
                SplitOnGroups(first, count, seed0, ref b0, seed1, out b1, out count0, out count1);
                BBNode node=new BBNode();
                node.bBox=new BBox(b0,b1);
                node.left =  ProcessGroup(first, count0);
                node.left.parent=node;
                node.right = ProcessGroup(first + count0, count1);
                node.right.parent = node;
                return node;
            }

            if (count == 1){
                Geometry geom=geometries[first] as Geometry;
                if (geom != null){
                    BBNode node=new BBNode();
                    node.geometry = geom;
                    return node;
                }
                
                DObject dObject = geometries[first] as DObject;
                
                if (dObject != null) 
                    return dObject.BbNode;
                
            }

            return null;
        }

        private void SplitOnGroups(int first, int count, int seed0, ref BBox b0, int seed1, out BBox b1, out int count0, out int count1) {
            b0 = geometries[seed0].Box;
            b1 = geometries[seed1].Box;

            //reshuffling in place: 
            //put the next element of the first group on the first not occupied place on from the left
            //put the next element of the second group to the last not occupied place to the right
            Swap(first, seed0);//this puts the seed0 the most left position
            Swap(first + count - 1, seed1);//this puts seed1 to the right most position

            double ratio = 2;
            //lp points to the first not assigned element to the right of group 0
            int lp = first + 1;
            //rp 
            int rp = first + count - 2; //seed1 stands at first +count-1
            count0 = 1;
            count1 = 1;

            while (count0 + count1 < count) {
                //First check the ratio of numbers of elements of the groups.
                //We need to keep the tree balanced. Let's watch that the ratio of the numbers of elements of the 
                // two groups is between ratio and 1/ratio.
                ObjectWithBox g = geometries[lp];
                if (count0 * ratio < count1)
                    AddToTheLeftGroup(ref b0, ref count0, ref lp, g);
                else if (count1 * ratio < count0)
                    AddToTheRightGroup(ref b1, ref count1, lp, ref rp, g);
                else { //make decision based on the growing of the group boxes
                    BBox b=g.Box;
                    double squareGrouth0 = CommonArea(ref b0, ref b) - b0.Area;
                    double squareGrouth1 = CommonArea(ref b1, ref b) - b1.Area;
                    if (squareGrouth0 < squareGrouth1)
                        AddToTheLeftGroup(ref b0, ref count0, ref lp, g);     
                    else
                        AddToTheRightGroup(ref b1, ref count1, lp, ref rp, g);
                }
            }
    
        }

        private void AddToTheRightGroup(ref BBox b1, ref int count1, int lp, ref int rp, ObjectWithBox g) {
            Swap(lp, rp);
            rp--;
            b1.Add(g.Box);
            count1++;
        }

        private static void AddToTheLeftGroup(ref BBox b0, ref int count0, ref int lp, ObjectWithBox g) {
            lp++;
            count0++;
            b0.Add(g.Box);
        }

        private void FindSeeds(int first, int count, out int seed0, out BBox b0, out int seed1) {
            seed0 = first;
            b0 = geometries[seed0].Box;

            double area = -1.0f;
            //looking for seed0       
            for (int i = first + 1; i < first + count; i++) {
                ObjectWithBox g = geometries[i];
                BBox b=g.Box;
                double ar = CommonArea(ref b0, ref b);
                if (ar > area) {
                    seed0 = i;
                    area = ar;
                }
            }

            //looking for seed1
            seed1 = first;//I'm getting a compiler error: there is no need actually to init seed1
            area = -1.0f;

            b0 = geometries[seed0].Box;

            for (int i = first; i < first + count; i++) {

                if (i == seed0)
                    continue;

                ObjectWithBox g = geometries[i];

                BBox b=g.Box;
                
                double ar = CommonArea(ref b0, ref b);
                if (ar > area) {
                    seed1 = i;
                    area = ar;
                }
            }

            if (seed0 > seed1)
            {
                int t = seed1;
                seed1 = seed0;
                seed0 = t;
            }
         
        }

        void Swap(int i, int j) {
            if (i != j) {
                ObjectWithBox t = geometries[i];
                geometries[i] = geometries[j];
                geometries[j] = t;
            } 
        }


        static double CommonArea(ref BBox a, ref BBox b) {
            double l = Math.Min(a.Left, b.Left);
            double r = Math.Max(a.Right, b.Right);
            double t = Math.Max(a.Top, b.Top);
            double bt = Math.Min(a.Bottom, b.Bottom);
            return (r - l) * (t - bt);

        }

        internal BBNode CalcHierachy() {
            return ProcessGroup(0, geometries.Count);
        }
    }
}
