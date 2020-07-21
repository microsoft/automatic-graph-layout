using System;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    internal class ClosestPointOnCurve {
        ClosestPointOnCurve() { }
        ///// <summary>
        ///// Gets the closest point on the curve to the point
        ///// </summary>
        ///// <param name="curve"></param>
        ///// <param name="coeff"></param>
        ///// <param name="hint">will return Double.MaxVal if Newton iterations fail</param>
        ///// <param name="closestPointParam"></param>
        internal static double ClosestPoint(ICurve curve, Point a, double hint, double low, double high) {
            const int numberOfIterationsMax = 5;
            const int numberOfOverShootsMax = 5;
            double t = hint;
      
            int numberOfIteration = 0;
            int numberOfOvershoots = 0;
            double dt;
            bool abort = false;
            do {
                Point c = curve[t];
                Point ct = curve.Derivative(t);
                Point ctt = curve.SecondDerivative(t);

                double secondDerivative = ct * ct + (c - a) * ctt;

                if (Math.Abs(secondDerivative) < ApproximateComparer.Tolerance) 
                    return t;

                dt = (c - a) * ct / secondDerivative;
               
                t -= dt;
                
                if (t > high + ApproximateComparer.Tolerance) {
                    t = high;
                    numberOfOvershoots++;
                } else if (t < low - ApproximateComparer.Tolerance) {
                    t = low;
                    numberOfOvershoots++;
                }
                numberOfIteration++;
            } while (Math.Abs(dt) > ApproximateComparer.Tolerance &&!
                (abort = (numberOfIteration >= numberOfIterationsMax || numberOfOvershoots >= numberOfOverShootsMax)));

            //may be the initial value was just fine
            if (abort && (curve[hint] - a).Length < ApproximateComparer.DistanceEpsilon) 
                t = hint;
            
            return t;

           }

    }
}
