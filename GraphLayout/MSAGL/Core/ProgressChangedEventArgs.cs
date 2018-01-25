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
