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
            Debug.Assert(Vector[block.VectorIndex] == block, "Inconsistent block.VectorIndex");
        }

        internal void Remove(Block block)
        {
            Debug.Assert(Vector[block.VectorIndex] == block, "Inconsistent block.VectorIndex");
            Block swapBlock = Vector[Vector.Count - 1];
            Vector[block.VectorIndex] = swapBlock;
            swapBlock.VectorIndex = block.VectorIndex;
            Vector.RemoveAt(Vector.Count - 1);
            Debug.Assert((0 == Vector.Count) || (block == swapBlock) || (Vector[swapBlock.VectorIndex] == swapBlock),
                    "Inconsistent swapBlock.VectorIndex");
            Debug.Assert((0 == Vector.Count) || (Vector[Vector.Count - 1].VectorIndex == (Vector.Count - 1)),
                    "Inconsistent finalBlock.VectorIndex");
        }

        /// <summary>
        /// </summary>
        public override string ToString()
        {
            return Vector.ToString();
        }
    }
}