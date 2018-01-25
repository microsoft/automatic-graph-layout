using System.Collections.Generic;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy
{
    /// <summary>
    /// Votes are separated into block, so that their weight (BlockWeight) can easily be adjusted
    /// for different force types. 
    /// </summary>
    public class VoteBlock
    {
        /// <summary>
        /// Set of votes from different nodes for the node to which this block belongs.
        /// </summary>
        public List<Vote> Votings { get; set; }
        /// <summary>
        /// Defines how strong this block will be considered in the optimization process.
        /// </summary>
        public double BlockWeight { get; set; }
   

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="votings"></param>
        /// <param name="blockWeight"></param>
        public VoteBlock(List<Vote> votings, double blockWeight){
            Votings = votings;
            BlockWeight = blockWeight;
        }
    }
}