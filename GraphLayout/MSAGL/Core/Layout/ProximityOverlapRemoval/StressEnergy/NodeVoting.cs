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
using System.Collections.Generic;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy {
    /// <summary>
    /// Collection of voting blocks for the node with index VotedNodeIndex.
    /// </summary>
    public class NodeVoting
    {
        /// <summary>
        /// Index of the voted node.
        /// </summary>
        readonly public int VotedNodeIndex;

        /// <summary>
        /// List of Blocks. Each Block has a set of votes for this node.
        /// </summary>
        public List<VoteBlock> VotingBlocks { get; set;}

        /// <summary>
        /// Constructor which initializes a single empty block with BlockWeight 1.
        /// </summary>
        /// <param name="votedNodeIndex"></param>
        public NodeVoting(int votedNodeIndex) {
            VotedNodeIndex = votedNodeIndex;
            VotingBlocks=new List<VoteBlock>();

            var voteBlock = new VoteBlock(new List<Vote>(), 1); 
            VotingBlocks.Add(voteBlock);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="votedNodeIndex"></param>
        /// <param name="votingBlocks"></param>
        public NodeVoting(int votedNodeIndex, List<VoteBlock> votingBlocks)
        {
            VotedNodeIndex = votedNodeIndex;
            VotingBlocks = votingBlocks;
        }
    }
}