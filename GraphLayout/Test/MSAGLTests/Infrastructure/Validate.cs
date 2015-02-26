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
﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Validate.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   Initially copied from Tuvalu's ValidateArg class for IsNotNull's ValidatedNotNull value attribute.
//   Subsequently modified to allow interactive mode.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows.Forms;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Msagl.UnitTests 
{
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    public static class Validate 
    {
        /// <summary>
        /// If true, the test runner is an interactive application so use Debug.Assert and let the user 
        /// do Abort/Retry/Ignore; otherwise use Assert.Fail which throws an exception.
        /// </summary>
        internal static bool InteractiveMode { get; set; }

        /// <summary>
        /// This lets StyleCop in the calling method know that a called method validates the attributed
        /// parameter as non-null.
        /// </summary>
        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
        private sealed class ValidatedNotNullAttribute : Attribute 
        {
        }

        /// <summary>
        /// Raises an assert dialog with the exception text if in interactive mode, else returns false so the caller rethrows.
        /// </summary>
        /// <param name="ex">The exception detected.</param>
        /// <returns>true if in interactive mode, else false</returns>
        private static bool RaiseInteractiveAssert(Exception ex)
        {
            if (InteractiveMode)
            {
                var exceptionToUse = ex.InnerException ?? ex;
                RaiseInteractiveAssert(exceptionToUse.ToString());
            }
            return InteractiveMode;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "This is test code.")]
        internal static void RaiseInteractiveAssert(string message)
        {
            // Debug.Assert puts a second callstack in the error dialog so do the Abort/Retry/Ignore handling manually.
            var button = MessageBox.Show(
                message,
                Process.GetCurrentProcess().ProcessName,
                MessageBoxButtons.AbortRetryIgnore,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button3);
            if (DialogResult.Retry == button)
            {
                Debugger.Break();
            }
            else if (DialogResult.Abort == button)
            {
                Environment.Exit(-1);
            }
        }

        [DebuggerStepThrough]
        internal static void AreEqual<T>(T expected, T actual, string message)
        {
            try
            {
                Assert.AreEqual(expected, actual, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void AreEqual<T>(double expected, double actual, double delta, string message)
        {
            try
            {
                Assert.AreEqual(expected, actual, delta, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void AreEqual<T>(string expected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            try
            {
                Assert.AreEqual(expected, actual, ignoreCase, culture, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void AreNotEqual<T>(T notExpected, T actual, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void AreNotEqual<T>(double notExpected, double actual, double delta, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, delta, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void AreNotEqual<T>(string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            try
            {
                Assert.AreNotEqual(notExpected, actual, ignoreCase, culture, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void AreNotSame(object notExpected, object actual, string message)
        {
            try
            {
                Assert.AreNotSame(notExpected, actual, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void AreSame(object expected, object actual, string message)
        {
            try
            {
                Assert.AreSame(expected, actual, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }
        
        [DebuggerStepThrough]
        internal static void Fail(string message)
        {
            if (InteractiveMode)
            {
                Debug.Assert(false, message);
            }
            else
            {
                Assert.Fail(message);
            }
        }

        [DebuggerStepThrough]
        internal static void Inconclusive(string message)
        {
            if (InteractiveMode)
            {
                Debug.Assert(false, "Inconclusive Test result: " + message);
            }
            else
            {
                Assert.Inconclusive(message);
            }
        }

        [DebuggerStepThrough]
        internal static void IsFalse(bool condition, string message)
        {
            if (InteractiveMode)
            {
                Debug.Assert(!condition, message);
            }
            else
            {
                Assert.IsFalse(condition, message);
            }
        }

        [DebuggerStepThrough]
        internal static void IsInstanceOfType(object value, Type expectedType, string message)
        {
            try
            {
                Assert.IsInstanceOfType(value, expectedType, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void IsNotInstanceOfType(object value, Type wrongType, string message)
        {
            try
            {
                Assert.IsNotInstanceOfType(value, wrongType, message);
            }
            catch (UnitTestAssertException ex)
            {
                if (!RaiseInteractiveAssert(ex))
                {
                    throw;
                }
            }
        }

        [DebuggerStepThrough]
        internal static void IsNotNull([ValidatedNotNull] object value, string message)
        {
            if (InteractiveMode)
            {
                Debug.Assert(null != value, message);
            }
            else
            {
                Assert.IsNotNull(value, message);
            }
        }

        [DebuggerStepThrough]
        internal static void IsNull([ValidatedNotNull] object value, string message)
        {
            if (InteractiveMode)
            {
                Debug.Assert(null == value, message);
            }
            else
            {
                Assert.IsNull(value, message);
            }
        }

        [DebuggerStepThrough]
        internal static void IsTrue(bool condition, string message)
        {
            if (InteractiveMode)
            {
                Debug.Assert(condition, message);
            }
            else
            {
                Assert.IsTrue(condition, message);
            }
        }

    }
}
