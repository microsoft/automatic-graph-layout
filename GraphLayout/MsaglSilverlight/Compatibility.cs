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
using System.Linq;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    // Partial Silverlight implementation of the Parallel class.
    public static class Parallel
    {
        private static TaskFactory Factory { get; set; }

        static Parallel()
        {
            Factory = new TaskFactory();
        }

        public static void ForEach<T>(IEnumerable<T> source, ParallelOptions parallelOptions, Action<T> body)
        {
            Task.WaitAll(source.Select(obj => Factory.StartNew(o => body((T)o), obj)).ToArray());
        }
    }

    public class ParallelOptions
    {
        public ParallelOptions()
        {
        }
    }
}
