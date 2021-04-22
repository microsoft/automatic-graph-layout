using System;
using Microsoft.Msagl.Core.Geometry;

namespace Microsoft.Msagl.Layout.LargeGraphLayout {
    /// <summary>
    /// represents a range of doubles
    /// </summary>
    public class Interval:IRectangle<double> {
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
        public double Area { get { return End - Start; } }
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

        public bool Contains(IRectangle<double> rect) {
            var r = (Interval)rect;
            return this.Contains(r);
        }

        public IRectangle<double> Intersection(IRectangle<double> rectangle) {
            var r = (Interval)rectangle;
            return new Interval(Math.Max(Start, r.Start), Math.Min(End, r.End));
        }

        public bool Intersects(IRectangle<double> rectangle) {
            var r = (Interval)rectangle;
            return this.Intersects(r);
        }

        public void Add(IRectangle<double> rectangle) {
            var r = (Interval)rectangle;
            Add(r.Start);
            Add(r.End);
        }

        public bool Contains(double p, double radius) {
            return Contains(p - radius) && Contains(p + radius);
        }

        public IRectangle<double> Unite(IRectangle<double> rectangle) {
            return new Interval(this, (Interval)rectangle);
        }
    }
}