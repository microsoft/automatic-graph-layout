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
