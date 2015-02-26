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
// <copyright file="BlockVector.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// MSAGL class for Block vector management.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Msagl.Core.ProjectionSolver
{
    /// <summary>
    /// </summary>
    class BlockVector
    {
        internal List<Block> Vector { get; private set; }
        internal bool IsEmpty { get { return 0 == Vector.Count; } }
        internal int Count { get { return Vector.Count; } }
        internal Block this[int index] { get { return Vector[index]; } }

        internal BlockVector()
        {
            Vector = new List<Block>();
        }

        internal void Add(Block block)
        {
            block.VectorIndex = Vector.Count;
            Vector.Add(block);
            //Debug_AssertConsistency();
            Debug.Assert(Vector[block.VectorIndex] == block, "Inconsistent block.VectorIndex");
        }

        internal void Remove(Block block)
        {
            Debug.Assert(Vector[block.VectorIndex] == block, "Inconsistent block.VectorIndex");
            Block swapBlock = Vector[Vector.Count - 1];
            Vector[block.VectorIndex] = swapBlock;
            swapBlock.VectorIndex = block.VectorIndex;
            Vector.RemoveAt(Vector.Count - 1);
            //Debug_AssertConsistency();
            Debug.Assert((0 == Vector.Count) || (block == swapBlock) || (Vector[swapBlock.VectorIndex] == swapBlock),
                    "Inconsistent swapBlock.VectorIndex");
            Debug.Assert((0 == Vector.Count) || (Vector[Vector.Count - 1].VectorIndex == (Vector.Count - 1)),
                    "Inconsistent finalBlock.VectorIndex");
        }

        [Conditional("DEBUG")]
        private void Debug_AssertConsistency()
        {
            for (int ii = 0; ii < Vector.Count; ++ii)
            {
                Debug.Assert(ii == Vector[ii].VectorIndex, "Inconsistent Vector[ii].VectorIndex");
            }
        }

        /// <summary>
        /// </summary>
        public override string ToString()
        {
            return Vector.ToString();
        }
    }
}