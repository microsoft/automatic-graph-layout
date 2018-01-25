namespace Microsoft.Msagl.Core.Geometry.Curves {
    public partial class Curve {

        double parStart;

        /// <summary>
        /// the start of the parameter domain
        /// </summary>
        public double ParStart {
            get { return parStart; }
            set { parStart = value; }
        }

        double parEnd;

        /// <summary>
        /// the end of the parameter domain
        /// </summary>
        public double ParEnd {
            get { return parEnd; }
            set { parEnd = value; }
        }

        /// <summary>
        /// Returns the curved moved by delta
        /// </summary>
        public void Translate(Point delta)
        {
            lock (this)
            {
                foreach (ICurve s in segs)
                    s.Translate(delta);

                pBNode = null;
            }
        }

        /// <summary>
        /// Returns the curved scaled by x and y
        /// </summary>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        /// <returns></returns>     
        public ICurve ScaleFromOrigin(double xScale, double yScale)
        {
            Curve c = new Curve(segs.Count);
            foreach (ICurve s in segs)
                c.AddSegment(s.ScaleFromOrigin(xScale, yScale));

            return c;
        }

        /// <summary>
        /// Return the transformed curve
        /// </summary>
        /// <param name="transformation"></param>
        /// <returns>the transformed curve</returns>
        public ICurve Transform(PlaneTransformation transformation) {
            Curve c = new Curve(segs.Count);
            foreach (ICurve s in segs)
                c.AddSegment(s.Transform(transformation));

            return c;
        }

        /// <summary>
        /// return length of the curve segment [start,end] 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public double LengthPartial(double start, double end) {
            double s, e;
            int i, j;
            
            AdjustStartEndEndParametersToDomain(ref start, ref end);
            this.GetSegmentAndParameter(start, out s, out i);
            this.GetSegmentAndParameter(end, out e, out j);

            ICurve seg = this.segs[i];
            double ret = seg.LengthPartial(s, seg.ParEnd);
            for (int k = i + 1; k < j; k++)
                ret += segs[k].Length;

            seg = segs[j];
            return ret + seg.LengthPartial(seg.ParStart, e);

        }

        /// <summary>
        /// An approximate length of the curve calculated using the interpolation
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        static public double LengthWithInterpolation(ICurve curve) {
            double ret = 0;
            foreach (LineSegment ls in Interpolate(curve, lineSegThreshold))
                ret += ls.Length;
            return ret;
        }

        /// <summary>
        /// An approximate length of the curve calculated using the interpolation
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        static public double LengthWithInterpolationAndThreshold(ICurve curve, double threshold)
        {
            double ret = 0;
            foreach (LineSegment ls in Interpolate(curve, threshold))
                ret += ls.Length;
            return ret;
        }

        /// <summary>
        /// Get the length of the curve
        /// </summary>
        public double Length {
            get {
                double ret = 0;
                foreach (ICurve ic in segs)
                    ret += ic.Length;
                return ret;
            }
        }
    }
}
