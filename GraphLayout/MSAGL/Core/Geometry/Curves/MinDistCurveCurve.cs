using System;

namespace Microsoft.Msagl.Core.Geometry.Curves {
    //For curves A(s) and B(t) with some guess for the parameters (s0, t0) we 
    //are trying to bring to (0,0) the vector(Fs,Ft).
    //where F(s,t) = (A(s) - B(t))^2.  To minimize F^2,
    //you get the system of equations to solve for ds and dt: 
    //Fs + Fss*ds + Fst*dt = 0
    //Ft + Fst*ds + Ftt*dt = 0
    // 
    //Where F = F(si,ti), Fs and Ft are the first partials at si, ti, Fxx are the second partials, 
    //    and s(i+1) = si+ds, t(i+1) = ti+dt. 
    //Of course you have to make sure that ds and dt do not take you out of your domain. 
    //This will converge if the curves have 2nd order continuity 
    //and we are starting parameters are reasonable.  
    //It is not a good method for situations that are not well behaved, but it is really simple.
/// <summary>
/// Implements the minimal distance between curves functionality
/// </summary>
#if TEST_MSAGL
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dist")]
    public 
#else
    internal
#endif
        class MinDistCurveCurve {

        ICurve curveA;
        ICurve curveB;
        double aMin;
        double aMax;
        double bMin; double bMax;
        double aGuess; double bGuess;
        double aSolution;
        internal double ASolution {
            get { return aSolution; }
        }
        double bSolution;
        internal double BSolution {
            get { return bSolution; }
        }

        Point aPoint;
        internal Point APoint { get { return aPoint; } }

        Point bPoint;
        internal Point BPoint { get { return bPoint; } }
        bool status;
        internal bool Status { get { return status; } }
        double si;
        double ti;

        Point a, b, a_b, ad, bd, add, bdd;

        void InitValues() {
            a = curveA[si];
            b = curveB[ti];
            a_b = a - b;
            ad = curveA.Derivative(si);
            add = curveA.SecondDerivative(si);
            bd = curveB.Derivative(ti);
            bdd = curveB.SecondDerivative(ti);
        }
/// <summary>
/// constructor
/// </summary>
/// <param name="curveAPar">first curve</param>
/// <param name="curveBPar">second curve</param>
/// <param name="lowBound0">the first curve minimal parameter</param>
        /// <param name="upperBound0">the first curve maximal parameter</param>
        /// <param name="lowBound1">the second curve minimal parameter</param>
        /// <param name="upperBound1">the first curve maximal parameter</param>
/// <param name="guess0"></param>
/// <param name="guess1"></param>
        public MinDistCurveCurve(ICurve curveAPar,
                ICurve curveBPar,
                double lowBound0,
                double upperBound0, 
                double lowBound1,
                double upperBound1,
                double guess0,
                double guess1) {
            this.curveA = curveAPar;
            this.curveB = curveBPar;
            this.aMin = lowBound0;
            this.bMin = lowBound1;
            this.aMax = upperBound0;
            this.bMax = upperBound1;
            this.aGuess = guess0;
            this.bGuess = guess1;
            this.si = guess0;
            this.ti = guess1;
        }


        //we ignore the mulitplier 2 here fore efficiency reasons
        double Fs {
            get {
                return /*2**/a_b * ad;
            }
        }

        double Fss {
            get {
                return /*2**/(a_b * add + ad * ad);
            }
        }

        double Fst //equals to Fts
        {
            get {
                return - /*2**/bd * ad;
            }
        }

        double Ftt {
            get {
                return /*2**/(-a_b * bdd + bd * bd);
            }
        }


        double Ft {
            get {
                return -/*2**/a_b * bd;
            }
        }



        /// <summary>
        /// xy - the first row
        /// uw - the second row
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="u"></param>
        /// <param name="w"></param>
        internal static double Delta(double x, double y, double u, double w) {
            return x * w - u * y;
        }
        //Fs + Fss*ds + Fst*dt = 0
        //Ft + Fst*ds + Ftt*dt = 0
        internal void Solve() {

            int numberOfBoundaryCrossings = 0;
            const int maxNumberOfBoundaryCrossings = 10;
            int numberOfTotalReps = 0;
            const int maxNumberOfTotalReps = 100;

            bool abort = false;

            InitValues();

            if (curveA is LineSegment && curveB is LineSegment ) {
                Point bd1 = curveB.Derivative(0);
                bd1 /= bd1.Length;
                Point an = (curveA as LineSegment).Normal;

                double del = Math.Abs(an * bd1);


                if (Math.Abs(del) < ApproximateComparer.DistanceEpsilon || Delta(Fss, Fst, Fst, Ftt) < ApproximateComparer.Tolerance) {
                    status = true;
                    ParallelLineSegLineSegMinDist();
                    return;
                }
            }

            double ds, dt;
            do {

                //hopefully it will be inlined by the compiler
                double delta = Delta(Fss, Fst, Fst, Ftt);
                if (Math.Abs(delta) < ApproximateComparer.Tolerance) {
                    status = false;
                    abort = true;
                    break;
                }

                ds = Delta(-Fs, Fst, -Ft, Ftt) / delta;
                dt = Delta(Fss, -Fs, Fst, -Ft) / delta;


                double nsi = si + ds;
                double nti = ti + dt;

                bool bc;

                if (nsi > aMax + ApproximateComparer.DistanceEpsilon || nsi < aMin - ApproximateComparer.DistanceEpsilon || nti > bMax + ApproximateComparer.DistanceEpsilon || nti < bMin - ApproximateComparer.DistanceEpsilon) {
                    numberOfBoundaryCrossings++;
                    ChopDsDt(ref ds, ref dt);
                    si += ds;
                    ti += dt;
                    bc = true;
                } else {
                    bc = false;
                    si = nsi;
                    ti = nti;
                    if (si > aMax)
                        si = aMax;
                    else if (si < aMin)
                        si = aMin;

                    if (ti > bMax)
                        ti = bMax;
                    else if (ti < bMin)
                        ti = bMin;
                }

                InitValues();

                numberOfTotalReps++;

                abort = numberOfBoundaryCrossings >= maxNumberOfBoundaryCrossings ||
                  numberOfTotalReps >= maxNumberOfTotalReps || (ds == 0 && dt == 0 && bc);

            } while ((Math.Abs(ds) >= ApproximateComparer.Tolerance || Math.Abs(dt) >= ApproximateComparer.Tolerance) && !abort);

            if (abort) {
                //may be the initial values were just OK
                Point t = curveA[aGuess] - curveB[bGuess];
                if (t * t < ApproximateComparer.DistanceEpsilon * ApproximateComparer.DistanceEpsilon) {
                    aSolution = aGuess;
                    bSolution = bGuess;
                    aPoint = curveA[aGuess];
                    bPoint = curveB[bGuess];
                    status = true;
                    return;

                }
            }


            aSolution = si;
            bSolution = ti;
            aPoint = a;
            bPoint = b;
            status = !abort;

        }



        void ChopDsDt(ref double ds, ref double dt) {
            if (ds != 0 && dt != 0) {
                double k1 = 1; //we are looking for a chopped vector of the form k(ds,dt)

                if (si + ds > aMax)  //we have si+k*ds=aMax           
                    k1 = (aMax - si) / ds;
                else if (si + ds < aMin)
                    k1 = (aMin - si) / ds;

                double k2 = 1;

                if (ti + dt > bMax)  //we need to have ti+k*dt=bMax  or ti+k*dt=bMin 
                    k2 = (bMax - ti) / dt;
                else if (ti + dt < bMin)
                    k2 = (bMin - ti) / dt;

                double k = Math.Min(k1, k2);
                ds *= k;
                dt *= k;

            } else if (ds == 0) {
                if (ti + dt > bMax)
                    dt = bMax - ti;
                else if (ti + dt < bMin)
                    dt = bMin - ti;
            } else {   //dt==0)
                if (si + ds > aMax)
                    ds = aMax - si;
                else if (si + ds < aMin)
                    ds = aMin - si;
            }
        }

        void ParallelLineSegLineSegMinDist() {
            LineSegment l0 = curveA as LineSegment;
            LineSegment l1 = curveB as LineSegment;

            Point v0 = l0.Start;
            Point v1 = l0.End;
            Point v2 = l1.Start;
            Point v3 = l1.End;

            Point d0 = v1 - v0;

            double nd0 = d0.Length;

            double r0 = 0, r1, r2, r3;

            if (nd0 > ApproximateComparer.DistanceEpsilon) {
                //v0 becomes the zero point
                d0 /= nd0;
                r1 = d0 * (v1 - v0);
                r2 = d0 * (v2 - v0);
                r3 = d0 * (v3 - v0);

                bool swapped = false;
                if (r2 > r3) {
                    swapped = true;
                    double t = r2;
                    r2 = r3;
                    r3 = t;
                }

                if (r3 < r0) {
                    aSolution = 0;
                    bSolution = swapped ? 0 : 1;
                } else if (r2 > r1) {
                    aSolution = 1;
                    bSolution = swapped ? 1 : 0;

                } else {
                    double r = Math.Min(r1, r3);
                    aSolution = r / (r1 - r0);
                    bSolution = (r - r2) / (r3 - r2);
                    if (swapped)
                        bSolution = 1 - bSolution;

                }
            } else {
                Point d1 = v3 - v2;
                double nd1 = d1.Length;
                if (nd1 > ApproximateComparer.DistanceEpsilon) {
                    //v2 becomes the zero point
                    d1 /= nd1;
                    r0 = 0; //v2 position
                    r1 = d1 * (v3 - v2);//v3 position
                    r2 = d1 * (v0 - v2);//v0 position - here v0 and v1 are indistinguishable


                    if (r2 < r0) {
                        bSolution = 0;
                        aSolution = 1;
                    } else if (r2 > r1) {
                        bSolution = 1;
                        aSolution = 0;

                    } else {
                        double r = Math.Min(r1, r2);
                        bSolution = r / (r1 - r0);
                        aSolution = 0;
                    }

                } else {
                    aSolution = 0;
                    bSolution = 0;
                }
            }
            aPoint = curveA[aSolution];
            bPoint = curveB[bSolution];
        }
    }

}
