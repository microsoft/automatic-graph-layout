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
namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.StressEnergy
{
    /// <summary>
    /// A vote for a certain distance from the node with voterIndex.
    /// </summary>
    public class Vote
    {
        /// <summary>
        /// Index of the node from which this vote is coming.
        /// </summary>
        readonly public int VoterIndex;
        /// <summary>
        /// Desired distance.
        /// </summary>
        public double Distance;
        /// <summary>
        /// Weight for this vote: usually 1/(distance*distance)" 
        /// </summary>
        readonly public double Weight;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="voterIndex"></param>
        /// <param name="distance"></param>
        /// <param name="weight"></param>
        public Vote(int voterIndex,double distance, double weight)
        {
            VoterIndex = voterIndex;
            Distance = distance;
            Weight = weight;
        }

        /// <summary>
        /// Constructor which sets the default weight for the distance.
        /// </summary>
        /// <param name="voterIndex"></param>
        /// <param name="distance"></param>
        public Vote(int voterIndex, double distance) {
            VoterIndex = voterIndex;
            Distance = distance;
            Weight = 1/distance/distance;
        }
    }
}