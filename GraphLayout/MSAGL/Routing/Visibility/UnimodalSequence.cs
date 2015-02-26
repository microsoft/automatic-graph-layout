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
﻿using System;

namespace Microsoft.Msagl.Routing.Visibility {
    /// <summary>
    /// A real functionf defined on
    /// the integers 0, 1, . . . , n-1 is said to be unimodal if there exists an integer m such that f is strictly increasing (respectively, decreasing) on [ 0, m] and
    /// decreasing (respectively, increasing) on [m + 1, n-1]
    /// No three sequential elements have the same value
    /// </summary>
    internal class UnimodalSequence {

        Func<int,double> sequence;

        /// <summary>
        /// the sequence values
        /// </summary>
        internal Func<int,double> Sequence {
            get { return sequence; }
            set { sequence = value; }
        }

        int length;
        /// <summary>
        /// the length of the sequence: the sequence starts from 0
        /// </summary>
        internal int Length {
            get { return length; }
            set { length = value; }
        }

        internal UnimodalSequence(Func<int,double> sequenceDelegate, int length) {
            this.Sequence = sequenceDelegate;
            this.Length = length;
        }

        internal enum Behavior {
            Increasing, Decreasing, Extremum,
        }

        internal int FindMinimum() {
            //find out first that the minimum is inside of the domain
            int a = 0;
            int b = Length - 1;
            int m = a + (b - a) / 2;
            double valAtM = Sequence(m);
            if (valAtM >= Sequence(0) && valAtM >= Sequence(Length - 1))
                return Sequence(0) < Sequence(Length - 1) ? 0 : Length - 1;

          
            while (b - a > 1) {
                m = a + (b - a) / 2;
                switch (BehaviourAtIndex(m)) {
                    case Behavior.Decreasing:
                        a = m;
                        break;
                    case Behavior.Increasing:
                        b = m;
                        break;
                    case Behavior.Extremum:
                        return m;
                }
            }
            if (a == b)
                return a;
            return Sequence(a) <= Sequence(b) ? a : b;
        }

        private Behavior BehaviourAtIndex(int m) {
            double seqAtM=Sequence(m);
            if (m == 0) {
                double seqAt1 = Sequence(1);
                if (seqAt1 == seqAtM)
                    return Behavior.Extremum;
                return seqAt1 > seqAtM ? Behavior.Increasing : Behavior.Decreasing;
            }
            if (m == Length-1) {
                double seqAt1 = Sequence(Length-2);
                if (seqAt1 == seqAtM)
                    return Behavior.Extremum;
                return seqAt1 > seqAtM ? Behavior.Decreasing : Behavior.Increasing;
            }

            double delLeft = seqAtM - Sequence(m - 1);
            double delRight = Sequence(m + 1) - seqAtM;
            if (delLeft * delRight <= 0)
                return Behavior.Extremum;

            return delLeft > 0 ? Behavior.Increasing : Behavior.Decreasing;
        }

        internal int FindMaximum() {
            //find out first that the maximum is inside of the domain
            int a = 0;
            int b = Length - 1;
            int m = a + (b - a) / 2;
            double valAtM = Sequence(m);
            if (valAtM <= Sequence(0) && valAtM <= Sequence(Length - 1))
                return Sequence(0) > Sequence(Length - 1) ? 0 : Length - 1;


            while (b - a > 1) {
                m = a + (b - a) / 2;
                switch (BehaviourAtIndex(m)) {
                    case Behavior.Decreasing:
                        b = m;
                        break;
                    case Behavior.Increasing:
                        a = m;
                        break;
                    case Behavior.Extremum:
                        return m;
                }
            }
            if (a == b)
                return a;
            return Sequence(a) >= Sequence(b) ? a : b;
            
        }
    }
}
