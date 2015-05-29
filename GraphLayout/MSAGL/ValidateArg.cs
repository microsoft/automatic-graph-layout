using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Core
{
    /// <summary>
    /// Helper class for validating parameter arguments.
    /// </summary>
    internal static class ValidateArg
    {
        /// <summary>
        /// Throws ArgumentNullException if the argument is null.
        /// </summary>
        /// <param name="arg">The argument to check.</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        /// <remarks>
        /// The ValidatedNotNullAttribute lets FxCop know that this method null-checks the argument.
        /// </remarks>
        [DebuggerStepThrough]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared source. This method may not be called in the current assembly.")]
        public static void IsNotNull([ValidatedNotNull] object arg, string parameterName)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException if the argument is less than 0.
        /// </summary>
        /// <param name="value">The argument to check.</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        /// <remarks>
        /// The ValidatedNotNullAttribute lets FxCop know that this method null-checks the argument.
        /// </remarks>
        [DebuggerStepThrough]
        public static void IsPositive(double value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "The argument must be positive");
            }
        }

        /// <summary>
        /// throws ArgumentException if the enumerable is empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="parameterName"></param>
        public static void IsNotEmpty<T>(IEnumerable<T> enumerable, string parameterName)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (!enumerable.Any())
            {
                throw new ArgumentException("The argument enumerable must not be empty", parameterName);
            }
        }

        /// <summary>
        /// Marks the parameter as being validated by the method.
        /// </summary>
        /// <remarks>
        /// StyleCop uses this to determine whether the calling method has validated a parameter.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
        private sealed class ValidatedNotNullAttribute : Attribute { }
    }
}
