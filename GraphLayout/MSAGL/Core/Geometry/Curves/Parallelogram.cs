// #region Using directives

using System;
using System.Collections.Generic;
using System.Globalization;
//#endregion

namespace Microsoft.Msagl.Core.Geometry.Curves {

    /// <summary>
    /// Parallelogram vertex type. 
    /// The clockwise order of vertices is Corner, A, OtherCorner,B
    /// </summary>
 

    public enum VertexId {
        /// <summary>
        /// the basic corner vertex
        /// </summary>
        Corner = 0,
        /// <summary>
        /// a vertex adjacent to the basic corner
        /// </summary>
        VertexA,
        /// <summary>
        /// the corner opposite to the basic corner
        /// </summary>           
        OtherCorner,
        /// <summary>
        /// another vertex adjacent to the basic corner
        /// </summary>
        VertexB
    }

    /// <summary>
    /// It is a parallelogram with the vertices corner, corner+coeff,corner+coeff+side1 and corner+side1.
    /// Parallelograms are used by GLEE in curve intersections routines.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")
#if TEST_MSAGL
    , Serializable
#endif
    ] 
    //parallelograms are not stored in dictionaries
    public struct Parallelogram {

        bool isSeg;

        Point corner;
        Point a;//a side exiting from the corner
        Point b;//another side exiting from the corner
        Point otherCorner;
        Point aPlusCorner;
        Point bPlusCorner;

#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=371
        // SharpKit/Colin: this to support the lack of int casting to enums
        private IEnumerable<Point> AllVertices
        {
            get
            {
                yield return corner;
                yield return aPlusCorner;
                yield return otherCorner;
                yield return bPlusCorner;
            }
        }

        private IEnumerable<Point> OtherVertices
        {
            get
            {
                yield return aPlusCorner;
                yield return otherCorner;
                yield return bPlusCorner;
            }
        }
#endif

        internal Point Corner {
            get { return corner; }
        }

        internal Point OtherCorner {
            get { return otherCorner; }
        }

        Point aRot;//a rotated on 90 degrees towards b
        Point bRot; //b rotated on 90 degrees towards a

        double abRot; //the scalar product a*bRot 
        double baRot; //the scalar product b*aRot;
/// <summary>
/// to string 
/// </summary>
/// <returns></returns>
        override public string ToString() {
            return String.Format(CultureInfo.InvariantCulture, "({0},{1},{2},{3})", Vertex(0), Vertex(VertexId.VertexA),
                                 Vertex(VertexId.OtherCorner), Vertex(VertexId.VertexB));
        }

        /// <summary>
        /// Constructs the parallelogram by the corner and two sides.
        /// </summary>
        /// <param name="corner">the corner</param>
        /// <param name="sideA">a side</param>
        /// <param name="sideB">another side</param>
        public Parallelogram(Point corner, Point sideA, Point sideB) {

            this.corner = corner;
            this.a = sideA;
            this.b = sideB;

            this.aRot = new Point(-sideA.Y, sideA.X);
            if (aRot.Length > 0.5)
                aRot = aRot.Normalize();

            this.bRot = new Point(-sideB.Y, sideB.X);
            if (bRot.Length > 0.5)
                bRot = bRot.Normalize();

            abRot = sideA * bRot;

            baRot = sideB * aRot;


            if (abRot < 0) {
                abRot = -abRot;
                bRot = -bRot;
            }

            if (baRot < 0) {
                baRot = -baRot;
                aRot = -aRot;
            }


            isSeg = (sideA - sideB).Length < ApproximateComparer.DistanceEpsilon;

            aPlusCorner = sideA + corner;
            otherCorner =  sideB +aPlusCorner;
            bPlusCorner = sideB + corner;
        }
        /// <summary>
        /// Return true if the parallelogram contains the point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Point point) {
            Point g = point - corner;
            double e = ApproximateComparer.DistanceEpsilon;

            double gbRot = g * bRot;
            if (gbRot > abRot + e || gbRot < -e)
                return false;

            double gaRot = g * aRot;
            return gaRot <= baRot + e && gaRot >= -e;
        }


/// <summary>
/// return the area of the parallelogram
/// </summary>
        public double Area { get { return Math.Abs(a.X * b.Y - a.Y * b.X); } }
     
        /// <summary>
        /// Return the correspoingin vertex of the parallelogram
        /// </summary>
        /// <param name="vertexPar">vertex kind</param>
        /// <returns>vertex value</returns>
        public Point Vertex(VertexId vertexPar) {
            switch (vertexPar) {
                case VertexId.Corner: return corner;
                case VertexId.VertexA: return aPlusCorner;
                case VertexId.OtherCorner: return OtherCorner;
                case VertexId.VertexB: return bPlusCorner;
                default:
                    throw new InvalidOperationException();
            }
        }

/// <summary>
/// returns true if parallelograms intersect
/// </summary>
        /// <param name="parallelogram0"></param>
        /// <param name="parallelogram1"></param>
/// <returns></returns>
        static public bool Intersect(Parallelogram parallelogram0, Parallelogram parallelogram1) {
            //my hunch is that p0 and p1 do not intersect if and only if
            //they can be separated with one of the parallelogram sides in case when at least one of them


            bool ret = !(separByA(ref parallelogram0, ref parallelogram1) || separByA(ref parallelogram1, ref parallelogram0) ||
                       separByB(ref parallelogram0, ref parallelogram1) || separByB(ref parallelogram1, ref parallelogram0));

            if (ret == false)
                return false;

            if (!(parallelogram0.isSeg && parallelogram1.isSeg))
                return true;


            if (!Point.ParallelWithinEpsilon(parallelogram0.OtherCorner - parallelogram0.Corner,
                                             parallelogram1.OtherCorner - parallelogram1.Corner, 1.0E-5))
                return true;

            //here we know that the segs are parallel
            return ParallelSegsIntersect(ref parallelogram1, ref parallelogram0);

        }


        static bool ParallelSegsIntersect(ref Parallelogram p0, ref Parallelogram p1) {

            Point v0 = p0.Corner;
            Point v1 = p0.OtherCorner;

            Point v2 = p1.Corner;
            Point v3 = p1.OtherCorner;


            Point d = v1 - v0;

            //let us imagine that v0 is at zero

            double r0 = 0; // position of v0

            //offset of v1
            double r1 = d * d;

            //offset of v2
            double r2 = (v2 - v0) * d;

            //offset of v3
            double r3 = (v3 - v0) * d;

            // we need to check if [r0,r1] intersects [r2,r3]

            if (r2 > r3) {
                double t = r2;
                r2 = r3;
                r3 = t;
            }

            return !(r3 < r0 - ApproximateComparer.DistanceEpsilon || r2 > r1 + ApproximateComparer.DistanceEpsilon);

        }

        static double mult(ref Point a, ref Point b) {
            return a.X * b.X + a.Y * b.Y;
        }

         static bool separByA(ref Parallelogram p0, ref Parallelogram p1) {

            double eps = ApproximateComparer.DistanceEpsilon;
            Point t = new Point(p1.corner.X - p0.corner.X, p1.corner.Y - p0.corner.Y);
            double p1a = mult(ref t , ref p0.aRot);

            if (p1a > p0.baRot + eps) {
                t.X = p1.aPlusCorner.X - p0.corner.X;
                t.Y = p1.aPlusCorner.Y - p0.corner.Y;
                if (mult(ref t, ref p0.aRot) <= p0.baRot + eps)
                    return false;

                t.X = p1.bPlusCorner.X - p0.corner.X;
                t.Y = p1.bPlusCorner.Y - p0.corner.Y;
                if (mult(ref t, ref p0.aRot) <= p0.baRot + eps)
                    return false;

                t.X = p1.otherCorner.X - p0.corner.X;
                t.Y = p1.otherCorner.Y - p0.corner.Y;
                if (mult(ref t, ref p0.aRot) <= p0.baRot + eps)
                    return false;

                return true;
            } else if (p1a < -eps) {
                t.X = p1.aPlusCorner.X - p0.corner.X;
                t.Y = p1.aPlusCorner.Y - p0.corner.Y;
                if (mult(ref t, ref p0.aRot) >= - eps)
                    return false;

                t.X = p1.bPlusCorner.X - p0.corner.X;
                t.Y = p1.bPlusCorner.Y - p0.corner.Y;
                if (mult(ref t, ref p0.aRot) >= -eps)
                    return false;

                t.X = p1.otherCorner.X - p0.corner.X;
                t.Y = p1.otherCorner.Y - p0.corner.Y;
                if (mult(ref t, ref p0.aRot) >= -eps)
                    return false;

                return true;
            }

            return false;/*
            double eps = Curve.DistEps;
            
            double p1a = (p1.Corner - p0.Corner) * p0.aRot;

            if (p1a > p0.baRot + eps) {
                for (int i = 1; i < 4; i++) {
                    if ((p1.Vertex((Parallelogram.VertexId)i) - p0.corner) * p0.aRot <= p0.baRot + eps)
                        return false;
                }
                return true;
            } else if (p1a < -eps) {
                for (int i = 1; i < 4; i++) {

                    double delta = (p1.Vertex((Parallelogram.VertexId)i) - p0.corner) * p0.aRot;

                    if (delta >= -eps) {
                        return false;
                    }
                }
                return true;
            }

            return false;*/
        }


         static bool separByB(ref Parallelogram p0, ref Parallelogram p1) {
            double eps = ApproximateComparer.DistanceEpsilon;
            double p1a = (p1.Vertex(0) - p0.corner) * p0.bRot;

            if (p1a > p0.abRot + eps) {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=371
                //SharpKit/Colin - can't cast ints to enum
                foreach (var i in p1.OtherVertices)
                    if ((i - p0.corner)*p0.bRot <= p0.abRot + eps)
                        return false;
#else
                for (int i = 1; i < 4; i++) {
                    if ((p1.Vertex((VertexId)i) - p0.corner) * p0.bRot <= p0.abRot + eps)
                        return false;
                }
#endif

                return true;
            } else if (p1a < -eps) {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=371
                //SharpKit/Colin - can't cast ints to enum
                foreach (var i in p1.OtherVertices)
                    if ((i - p0.corner) * p0.bRot >= -eps)
                        return false;
#else
                for (int i = 1; i < 4; i++) {
                    if ((p1.Vertex((VertexId)i) - p0.corner) * p0.bRot >= -eps)
                        return false;
                }
#endif
                return true;
            }
            return false;
        }
        /// <summary>
        /// create a Parallelogram over a group
        /// </summary>
        /// <param name="boxes">group of boxes</param>
        /// <returns>xy box</returns>
        internal static Parallelogram GetParallelogramOfAGroup(List<Parallelogram> boxes) {

            double minX = 0, maxX = 0, minY = 0, maxY = 0;//have to int - getting compiler error - bug
            bool firstTime = true;
            foreach (Parallelogram b in boxes) {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=371
                // SharpKit/Colin: can't cast ints to enums
                foreach (Point v in b.AllVertices) {
#else
                for (int i = 0; i < 4; i++) {
                    Point v = b.Vertex((VertexId)i);
#endif
                    double x = v.X;
                    double y = v.Y;
                    if (firstTime) {
                        firstTime = false;
                        minX = maxX = x;
                        minY = maxY = y;
                    } else {
                        if (x < minX) {
                            minX = x;
                        } else if (x > maxX) {
                            maxX = x;
                        }
                        if (y < minY) {
                            minY = y;
                        } else if (y > maxY) {
                            maxY = y;
                        }
                    }
                }

            }
            return new Parallelogram(new Point(minX, minY), new Point(0, maxY - minY), new Point(maxX - minX, 0));
        }

        internal Parallelogram(Parallelogram box0, Parallelogram box1) {
            Point v = box0.Corner;
            double minX, maxX, minY, maxY;
            minX = maxX = v.X;
            minY = maxY = v.Y;


            PumpMinMax(ref minX, ref maxX, ref minY, ref maxY, ref box0.aPlusCorner);
            PumpMinMax(ref minX, ref maxX, ref minY, ref maxY, ref box0.otherCorner);
            PumpMinMax(ref minX, ref maxX, ref minY, ref maxY, ref box0.bPlusCorner);

            PumpMinMax(ref minX, ref maxX, ref minY, ref maxY, ref box1.corner);
            PumpMinMax(ref minX, ref maxX, ref minY, ref maxY, ref box1.aPlusCorner);
            PumpMinMax(ref minX, ref maxX, ref minY, ref maxY, ref box1.otherCorner);
            PumpMinMax(ref minX, ref maxX, ref minY, ref maxY, ref box1.bPlusCorner);

            this.corner = new Point(minX, minY);
            this.a = new Point(0, maxY - minY);
            this.b = new Point(maxX - minX, 0);

            aPlusCorner = a + corner;
            otherCorner = b + aPlusCorner;
            bPlusCorner = b + corner;

            this.aRot = new Point(-this.a.Y, this.a.X);
            if (aRot.Length > 0.5)
                aRot = aRot.Normalize();

            this.bRot = new Point(-this.b.Y, this.b.X);
            if (bRot.Length > 0.5)
                bRot = bRot.Normalize();

            abRot = this.a * bRot;
            baRot = this.b * aRot;


            if (abRot < 0) {
                abRot = -abRot;
                bRot = -bRot;
            }

            if (baRot < 0) {
                baRot = -baRot;
                aRot = -aRot;
            }

            isSeg = (this.a - this.b).Length < ApproximateComparer.DistanceEpsilon;
        }

         static void PumpMinMax(ref double minX, ref double maxX, ref double minY, ref double maxY, 
            ref Point p) {
            if (p.X < minX) {
                minX = p.X;
            } else if (p.X > maxX) {
                maxX = p.X;
            }
            if (p.Y < minY) {
                minY = p.Y;
            } else if (p.Y > maxY) {
                maxY = p.Y;
            }
        }
    }
}
