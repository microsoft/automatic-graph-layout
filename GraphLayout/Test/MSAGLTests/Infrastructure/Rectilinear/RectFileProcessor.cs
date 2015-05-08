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
