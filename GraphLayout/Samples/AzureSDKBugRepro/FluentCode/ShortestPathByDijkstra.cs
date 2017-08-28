using FluentVisualizer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentVisualizer.Core
{
    public class ShortestPathByDijkstra
    {
        public static List<string> GetPath(List<FluentNode> fluentNodes, string startId, string finishId)
        {
            var previous = new Dictionary<string, string>();
            var distances = new Dictionary<string, int>();
            var nodes = new List<string>();
            var path = new List<string>();

            foreach (var node in fluentNodes)
            {
                nodes.Add(node.Id);
                if(node.Id.Equals(startId, StringComparison.OrdinalIgnoreCase))
                {
                    distances[node.Id] = 0;
                }
                else
                {
                    distances[node.Id] = int.MaxValue;
                }
            }

            while (nodes.Count != 0)
            {
                nodes.Sort((x, y) => distances[x] - distances[y]);

                var smallest = nodes[0];
                nodes.Remove(smallest);

                if (smallest.Equals(finishId, StringComparison.OrdinalIgnoreCase))
                {
                    while (previous.ContainsKey(smallest))
                    {
                        path.Add(smallest);
                        smallest = previous[smallest];
                    }
                    break;
                }

                if (distances[smallest] == int.MaxValue)
                {
                    break;
                }

                foreach (var childId in fluentNodes.First( n => n.Id.Equals(smallest, StringComparison.OrdinalIgnoreCase)).ChildrenIds)
                {
                    var alt = distances[smallest] + 1;
                    if (alt < distances[childId])
                    {
                        distances[childId] = alt;
                        previous[childId] = smallest;
                    }
                }
            }

            if(path.Count != 0)
            {
                path.Add(startId);
                path.Reverse();
            }

            return path;
        }
    }
}
