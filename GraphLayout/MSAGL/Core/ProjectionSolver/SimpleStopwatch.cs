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
