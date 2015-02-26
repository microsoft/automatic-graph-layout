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
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleStopwatch.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for timeout detection without using Stopwatch class which is not supported by Silverlight.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#if SILVERLIGHT

using System;

// Silverlight 4.0 brings a Stopwatch class to Winphone only; hopefully 5.0 will bring it to the main line.

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    internal class SimpleStopwatch
    {
        DateTime startDateTime;
        bool isStarted;

        public void Start()
        {
            this.startDateTime = DateTime.Now;
            this.isStarted = true;
        }

        public void Stop()
        {
            if (this.isStarted)
            {
                TimeSpan span = DateTime.Now - this.startDateTime;
                this.isStarted = false;
                ElapsedTicks += span.Ticks;
            }
        }

        public void Reset()
        {
            ElapsedTicks = 0;
            this.isStarted = false;
        }

        public TimeSpan Elapsed { get { return new TimeSpan(ElapsedTicks); } }

        Int64 elapsedTicks;
        public Int64 ElapsedTicks
        {
            get
            {
                if (this.isStarted)
                {
                    DateTime now = DateTime.Now;
                    TimeSpan span = now - this.startDateTime;
                    this.elapsedTicks += span.Ticks;
                    this.startDateTime = now;
                }
                return this.elapsedTicks;
            }
            private set
            {
                this.elapsedTicks = value;
            }
        }

        public Int64 ElapsedMilliseconds { get { return (Int64)Elapsed.TotalMilliseconds; } }
    }
}
#endif // SILVERLIGHT
