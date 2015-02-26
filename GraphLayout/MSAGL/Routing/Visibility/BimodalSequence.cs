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
    /// For our purposes, it suffices to define a bimodal function as
    /// one for which there is an r in [ 0, n-1] such that f(r), f(r + I), . . . , f(n), f( l), . . . ,
    /// f(r - 1) is unimodal. In our case no three sequential elements have the same value
    /// </summary>
    internal class BimodalSequence {

        internal BimodalSequence(Func<int,double> sequence, int length) {
            this.sequence = sequence;
            this.length = length;
        }

        Func<int,double> sequence;

        /// <summary>
        /// the sequence values
        /// </summary>
        internal Func<int,double> Sequence {
            get { return sequence; }
        }

        int length;
        /// <summary>
        /// the length of the sequence: the sequence starts from 0
        /// </summary>
        internal int Length {
            get { return length; }
        }
        //following Chazelle, Dobkin
        internal int FindMinimum(){
            if (sequence(0) == sequence(Length - 1)) //we have a unimodal function
                return (new UnimodalSequence(sequence,Length)).FindMinimum();

            return (new UnimodalSequence(GetAdjustedSequenceForMinimum(), Length)).FindMinimum();

           
        }

        Func<int,double> GetAdjustedSequenceForMinimum() {
            double leftVal=Sequence(0);
            double rightVal=Sequence(Length-1);
            double k=(rightVal-leftVal)/(Length-1);
            return delegate(int i) { return Math.Min(this.Sequence(i), leftVal + k * i); };
        }

        internal int FindMaximum() {
            if (sequence(0) == sequence(Length - 1)) //we have a unimodal function
                return (new UnimodalSequence(sequence, Length)).FindMaximum();

            return (new UnimodalSequence(GetAdjustedSequenceForMaximum(), Length)).FindMaximum();

        }

        Func<int,double> GetAdjustedSequenceForMaximum() {
            double leftVal = Sequence(0);
            double rightVal = Sequence(Length - 1);
            double k = (rightVal - leftVal) / (Length - 1);
            return delegate(int i) { return Math.Max(this.Sequence(i), leftVal + k * i); };
        }
    }
}
