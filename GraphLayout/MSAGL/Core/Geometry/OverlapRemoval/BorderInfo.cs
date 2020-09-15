// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlapRemovalClusterBorderInfo.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Cluster border information.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Msagl.Core.Geometry
{
    using System.Diagnostics;

    
    /// <summary>
    /// Specifies information for one of the four borders of a rectangular cluster.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public struct BorderInfo
    {
        /// <summary>
        /// Space between the border and any nodes or clusters within it, in addition
        /// to any internode padding specified for the ConstraintGenerator.  This effectively
        /// sets the outer border margin to the outermost node's outer border plus this
        /// InnerMargin (unless fixed, in which case the FixedPosition is the outer border
        /// and it is assumed to include space for InnerMargin).
        /// Does not apply to nodes or clusters outside the cluster.
        /// </summary>
        public double InnerMargin { get; set; }

        /// <summary>
        /// A fixed border position calculated by the application; nodes and clusters
        /// inside or outside the border will move in relation to it but the border
        /// remains stationary.  This is the axis coordinate of the outer border edge
        /// of the cluster on this side.  This value is NoFixedPosition if not set, and
        /// may be set to NoFixedPosition to clear it.  In order for the position to
        /// remain "fixed", set Weight to some large value, such as DefaultFixedWeight.
        /// </summary>
        public double FixedPosition { get; set; }

        /// <summary>
        /// Border weight; set high to enforce FixedPosition.  By default it is low
        /// to allow the border to move freely, sizing the cluster according to the
        /// movement of its contained nodes and clusters.
        /// </summary>
        public double Weight
        {
            get { return this.borderWeight; }
            set
            {
                if (value <= 0.0)
                {
                    throw new ArgumentOutOfRangeException("value"
#if TEST_MSAGL
                                                          , @"Weight must be greater than zero"
#endif // TEST_MSAGL
                        );
                }
                this.borderWeight = value;
            }
        }
         double borderWeight;

        /// <summary>
        /// Returns whether FixedPosition has been set.
        /// </summary>
        public bool IsFixedPosition
        {
            get { return !Double.IsNaN(FixedPosition) && (Weight > 0.0); }
        }

        /// <summary>
        /// Default weight for an unfixed border's Weight property; the property may be overridden
        /// by the application.
        /// </summary>
        public static double DefaultFreeWeight
        {
            get { return OverlapRemovalGlobalConfiguration.ClusterDefaultFreeWeight; }
        }
        /// <summary>
        /// Default weight for a fixed border's Weight property; the property may be overridden
        /// by the application.
        /// </summary>
        public static double DefaultFixedWeight
        {
            get { return OverlapRemovalGlobalConfiguration.ClusterDefaultFixedWeight; }
        }

        /// <summary>
        /// Value gotten from or set to FixedPosition indicating that it's not set.
        /// </summary>
        public static double NoFixedPosition
        {
            get { return Double.NaN; }
        }

        /// <summary>
        /// Sets the border to fixed (resistant to movement).
        /// </summary>
        /// <param name="position">desired position</param>
        /// <param name="weight">coefficient of allowed movement relative to other terms; higher
        ///         weight is more resistant to movement.  High-weight borders can still move each other
        ///         due to constraint satisfaction of intervening clusters/variables.</param>
        public void SetFixed(double position, double weight)
        {
            this.FixedPosition = position;
            this.Weight = weight;
        }

        /// <summary>
        /// Sets the border to unfixed (freely moving).
        /// </summary>
        public void SetUnfixed()
        {
            this.FixedPosition = NoFixedPosition;
            this.Weight = DefaultFreeWeight;
        }

        /// <summary>
        /// Constructor taking only a margin-width value.
        /// </summary>
        ///<param name="innerMargin"></param>
        public BorderInfo(double innerMargin) : this(innerMargin, NoFixedPosition, DefaultFreeWeight) { }

        /// <summary>
        /// Constructor taking values for margin, fixed position, and weight.
        /// </summary>
        /// <param name="innerMargin"></param>
        /// <param name="fixedPosition"></param>
        /// <param name="weight"></param>
        public BorderInfo(double innerMargin, double fixedPosition, double weight)
            : this()
        {
            this.InnerMargin = innerMargin;
            this.FixedPosition = fixedPosition;
            this.Weight = weight;
        }

        internal void EnsureWeight()
        {
            // Weight must be > 0.0 (we'll divide by this later); use DefaultFreeWeight or DefaultFixedWeight,
            // or call one of the parameterized ctors or SetFixed/SetUnfixed.  Assert this for TEST_MSAGL builds
            // but handle the default case for release.
            Debug.Assert(this.Weight > 0.0, "BorderInfo.Weight must be > 0.0; use DefaultFreeWeight or DefaultFixedWeight or a parameterized ctor");
            if (0.0 == this.Weight)
            {
                // This is probably the default ctor that we can't override in a struct so default to unfixed.
                this.Weight = DefaultFreeWeight;
                if (0.0 == this.FixedPosition)
                {
                    this.FixedPosition = NoFixedPosition;
                }
            }
        }

        /// <summary>
        /// Generate a string representation of the BorderInfo.
        /// </summary>
        /// <returns>A string representation of the BorderInfo.</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture
                                 , "m {0:F5} p {1:F5} w {2:F5}"
                                 , this.InnerMargin, this.FixedPosition, this.Weight);
        }

        #region RequiredOverridesForStruct
        // These probably aren't compared but it is not expensive to provide the overrides,
        // especially given the Reflection issue if they are compared without these, as
        // described in FxCop rule CA1815.

        // Omitting getHashCode violates rule: OverrideGetHashCodeOnOverridingEquals.
        /// <summary>
        /// Return a hashcode based upon data members.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return InnerMargin.GetHashCode() ^ FixedPosition.GetHashCode() ^ Weight.GetHashCode();
        }

        // Omitting any of the following violates rule: OverrideEqualsAndOperatorEqualsOnValueTypes
        /// <summary>
        /// Compare objects based upon data members.
        /// </summary>
        /// <param name="obj">Object to be compared to the current object.</param>
        /// <returns></returns>
        public override bool Equals(Object obj)
        {
            if (!(obj is BorderInfo))
                return false;
            var bi = (BorderInfo)obj;
            return (bi.FixedPosition == this.FixedPosition)
                   && (bi.Weight == this.Weight)
                   && (bi.InnerMargin == this.InnerMargin);
        }
        /// <summary>
        /// Compare two BorderInfo objects for equality of data members.
        /// </summary>
        /// <returns></returns>
        public static bool operator ==(BorderInfo left, BorderInfo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two BorderInfo objects for inequality of data members.
        /// </summary>
        /// <returns></returns>
        public static bool operator !=(BorderInfo left, BorderInfo right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Compare two BorderInfo objects for ordering based upon data members.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static bool operator <(BorderInfo left, BorderInfo right)
        {
            if (left.FixedPosition < right.FixedPosition)
            {
                return true;
            }
            if (left.FixedPosition > right.FixedPosition)
            {
                return false;
            }
            if (left.Weight < right.Weight)
            {
                return true;
            }
            if (left.Weight > right.Weight)
            {
                return false;
            }
            return (left.InnerMargin < right.InnerMargin);
        }

        /// Compare two BorderInfo objects for ordering based upon data members.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static bool operator >(BorderInfo left, BorderInfo right)
        {
            return right < left;
        }
        #endregion // RequiredOverridesForStruct
    }
}