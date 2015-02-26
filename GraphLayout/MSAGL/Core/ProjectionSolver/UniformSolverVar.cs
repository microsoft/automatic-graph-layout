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
using System.Diagnostics;

namespace Microsoft.Msagl.Core.ProjectionSolver{
    internal class UniformSolverVar{
        double lowBound = double.NegativeInfinity;
        double upperBound = double.PositiveInfinity;
        internal bool IsFixed;
        double position;

        internal double Width { get; set; }

        internal double Position{
            get { return position; }
            set {
                if (value < lowBound)
                    position = lowBound;
                else if (value > upperBound)
                    position = upperBound;
                else
                    position = value;
            }
        }

        internal double LowBound {
            get { return lowBound; }
            set {
                Debug.Assert(value<=upperBound);
                lowBound = value;
            }
        }

        internal double UpperBound {
            get { return upperBound; }
            set {
                Debug.Assert(value>=LowBound);
                upperBound = value;
            }
        }

        

#if TEST_MSAGL
        public override string ToString() {
            return lowBound + " " + Position + " " + upperBound;
        }
#endif
    }
}