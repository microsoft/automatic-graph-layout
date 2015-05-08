// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasicFileProcessor.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Msagl.UnitTests
{
    /// <summary>
    /// Provides basic wildcard file-processing capability; must be overridden by
    /// a class specialized on a specific file type.
    /// </summary>
    internal abstract class BasicFileProcessor
    {
        protected readonly bool Verbose;
        protected readonly bool Quiet;
        protected readonly Action<string> WriteLineFunc;
        protected readonly Func<string, bool> ErrorFunc;
        internal bool Recursive { get; set; }

        internal int NumberOfFilesProcessed { get; private set; }
        internal List<string> FailedFiles { get; private set; }

        internal BasicFileProcessor(Action<string> writeLineFunc, Func<string, bool> errorFunc, bool verbose, bool quiet)
        {
            this.WriteLineFunc = writeLineFunc;
            this.ErrorFunc = errorFunc;
            this.Verbose = verbose;
            this.Quiet = quiet;
            FailedFiles = new List<string>();
        }

        internal void ProcessFiles(string strPathFileSpec)
        {
            // strPathFileSpec may be with or without directory or wildcards:
            //   x.txt
            //   Test\Data\x.txt
            //   Test\Data\Rand*.txt

            // Break out the directory and filename specification.
            string strFileSpec = Path.GetFileName(strPathFileSpec);
            string strDirectory = Path.GetDirectoryName(strPathFileSpec);
            if (string.IsNullOrEmpty(strDirectory))
            {
                strDirectory = ".";
            }
            strDirectory = Path.GetFullPath(strDirectory);
            ProcessFiles(strDirectory, strFileSpec);
        }

        private void ProcessFiles(string strDirectory, string strFileSpec)
        {
            var di = new DirectoryInfo(strDirectory);
            FileSystemInfo[] fis = di.GetFileSystemInfos(strFileSpec);

            // Get all files at this directory level first.
            foreach (FileSystemInfo fi in fis)
            {
                ++this.NumberOfFilesProcessed;
                if (this.Verbose)
                {
                    // From TestRectilinear, so write a blank line before next test method if there's a bunch of output
                    this.WriteLineFunc(string.Empty);
                }
                this.WriteLineFunc(string.Format("( {0} )", fi.FullName));
                ProcessFile(fi.FullName);
            }

            // Now handle recursion into subdirectories.
            if (Recursive)
            {
                // Recurse into subdirectories of this directory for files of the same spec.
                foreach (string strSubdir in Directory.GetDirectories(strDirectory))
                {
                    ProcessFiles(Path.GetFullPath(strSubdir), strFileSpec);
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is test code.")]
        private void ProcessFile(string fileName)
        {
            try
            {
                this.LoadAndProcessFile(fileName);
            }
            catch (Exception ex)
            {
                var innerEx = ex.InnerException ?? ex;
                if (!this.ErrorFunc(string.Format("*** Exception in File ***   {0}: {1}", fileName, innerEx)))
                {
                    // Caller did not handle the error so rethrow.
                    throw;
                }
                this.FailedFiles.Add(fileName);
            }
        }

        internal abstract void LoadAndProcessFile(string filename);
    }
} // end namespace TestRectilinear
