using System;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    /// <summary>
    /// looking for a root of a function on a given segment
    /// </summary>
    static public class RootFinder {


        /// <summary>
        /// implements the Newton method
        /// </summary>
        /// <param name="f"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="guess"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public static bool TryToFindRoot(IFunction f, double start, double end, double guess, out double x) {
            ValidateArg.IsNotNull(f, "f");
            System.Diagnostics.Debug.Assert(start >= f.ParStart && end <= f.ParEnd);
            System.Diagnostics.Debug.Assert(start <= guess && end >= guess);
            int numberOfBoundaryCrossings = 0;
            const int maxNumberOfBoundaryCrossings = 10;
            int numberOfTotalReps = 0;
            const int maxNumberOfTotalReps = 100;
            x = guess;

            double dx;
            bool abort = false;
            do {

                var fp = f.Derivative(x);
                if (Math.Abs(fp) < ApproximateComparer.Tolerance) {
                    abort = true;
                    break;
                }

                dx = -f[x] / fp;
                x += dx;
                if (x < start - ApproximateComparer.DistanceEpsilon) {
                    x = start;
                    numberOfBoundaryCrossings++;
                } else if (x > end + ApproximateComparer.DistanceEpsilon) {
                    x = end;
                    numberOfBoundaryCrossings++;
                }

                numberOfTotalReps++;

                abort = numberOfBoundaryCrossings >= maxNumberOfBoundaryCrossings ||
                  numberOfTotalReps >= maxNumberOfTotalReps || dx == 0;

            } while (Math.Abs(dx) >= ApproximateComparer.Tolerance && !abort);

            if (abort) {
                //may be the initial guess was just OK
                if (Math.Abs(f[guess]) < ApproximateComparer.DistanceEpsilon) {
                    x = guess;
                    return true;
                }
                return false;
            } 
            if (x < start) 
                x = start;
            else if (x > end )
                x = end;
            
            return true;

        }
    }
}
