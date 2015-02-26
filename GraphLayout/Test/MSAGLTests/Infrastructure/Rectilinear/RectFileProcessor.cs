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
// <copyright file="RectFileProcessor.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Routing;

namespace Microsoft.Msagl.UnitTests.Rectilinear 
{
    internal class RectFileProcessor : BasicFileProcessor
    {
        private readonly Func<RectFileReader, RectilinearEdgeRouterWrapper> routingFunc;
        private readonly int fileRoundingDigits;

        internal RectilinearEdgeRouterWrapper Router { get; private set; }

        internal RectFileProcessor(Action<string> writeLineFunc,
                            Func<string, bool> errorFunc,
                            Func<RectFileReader, RectilinearEdgeRouterWrapper> routingFunc,
                            int fileRoundingDigits,
                            bool verbose,
                            bool quiet)
            : base(writeLineFunc, errorFunc, verbose, quiet)
        {
            this.routingFunc = routingFunc;
            this.fileRoundingDigits = fileRoundingDigits;
        }

        internal override void LoadAndProcessFile(string fileName)
        {
            // BasicFileProcessor provides exception management.
            using (var reader = new RectFileReader(fileName, fileRoundingDigits))
            {
                Router = routingFunc(reader);
                Verify(Router, reader);
            }
        }

        private static void Verify(RectilinearEdgeRouterWrapper router, RectFileReader reader)
        {
            if (router.WantVerify) {
                reader.VerifyObstaclePaddedPolylines(router.ObstacleTree.GetAllObstacles());
                reader.VerifyClumps(router);
                reader.VerifyConvexHulls(router);
                reader.VerifyVisibilityGraph(router);
                reader.VerifyScanSegments(router);
            }
        }
    }
} // end namespace TestRectilinear
