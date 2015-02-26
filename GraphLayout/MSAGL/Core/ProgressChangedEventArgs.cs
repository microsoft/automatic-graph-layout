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
using System;

namespace Microsoft.Msagl.Core
{
    /// <summary>
    /// Progress changed event argument class for MSAGL progress changes.
    /// </summary>
    public class ProgressChangedEventArgs : EventArgs
    {
        private string algorithmDescription;
        private double ratioComplete;

        /// <summary>
        /// Constructurs a ProgressChangedEventArgs with the given ratio complete.
        /// </summary>
        /// <param name="ratioComplete">between 0 (not started) and 1 (finished)</param>
        public ProgressChangedEventArgs(double ratioComplete)
            : this(ratioComplete, null)
        {
        }

        /// <summary>
        /// Constructurs a ProgressChangedEventArgs with the given ratio complete and description.
        /// </summary>
        /// <param name="ratioComplete">between 0 (not started) and 1 (finished)</param>
        /// <param name="algorithmDescription">a useful description</param>
        public ProgressChangedEventArgs(double ratioComplete, string algorithmDescription)
        {
            this.ratioComplete = ratioComplete;
            this.algorithmDescription = algorithmDescription;
        }

        /// <summary>
        /// A useful algorithm description: e.g. the stage of layout in progress.
        /// </summary>
        public string AlgorithmDescription
        {
            get { return this.algorithmDescription; }
        }

        /// <summary>
        /// The ratio complete of the current stage: should always be between 0 and 1.
        /// </summary>
        public double RatioComplete
        {
            get { return this.ratioComplete; }
        }
    }
}
