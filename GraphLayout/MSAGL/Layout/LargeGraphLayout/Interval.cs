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
﻿using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// represents a range of doubles
    /// </summary>
    public class Interval {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public Interval(double start, double end) {
            Start = start;
            End = end;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Interval(Interval a, Interval b)
        {
            Start = a.Start;
            End = a.End;
            Add(b.Start);
            Add(b.End);
        }



        /// <summary>
        /// expanding the range to hold v
        /// </summary>
        /// <param name="v"></param>
        public void Add(double v) {
            if (Start > v)
                Start = v;
            if (End < v)
                End = v;
        }

        /// <summary>
        /// 
        /// </summary>
        public double Start { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double End { get; set; }

        /// <summary>
        /// the length
        /// </summary>
        public double Length { get { return End - Start; } }

        /// <summary>
        /// return true if the value is inside the range
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool Contains(double v) {
            return Start <= v && v <= End;
        }

        /// <summary>
        /// bringe v into the range
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double GetInRange(double v) {
            return v < Start ? Start : (v > End ? End : v);
        }

        /// <summary>
        /// returns true if and only if two intervals are intersecting
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Intersects(Interval other) {
            if (other.Start > End + ApproximateComparer.DistanceEpsilon)
                return false;

            return !(other.End < Start - ApproximateComparer.DistanceEpsilon);
        }
    }
}