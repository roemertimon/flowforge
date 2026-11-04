using System.Collections.Generic;
using FlowForge.Model;

namespace FlowForge.Algorithms;

/// <summary>
/// Finds augmenting paths with a depth-first search that explores neighbours in ascending id order. This is the plain
/// Ford-Fulkerson method. If a special edge is given, the paths are routed through it whenever possible (see <see cref="WorstCaseSearch"/>).
/// </summary>
public class DepthFirstSearchStrategy : ISearchStrategy
{
    public bool FindAugmentingPath(FlowGraph flowGraph, FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow, FlowEdge? specialEdge = null)
    {
        if (specialEdge != null)
        {
            return WorstCaseSearch.FindAugmentingPathWithEdge(flowGraph, source, target, specialEdge, pathFlow);
        }

        var parentMap = new Dictionary<FlowNode, FlowEdge>();
        var nodesToVisit = new Stack<FlowNode>();
        nodesToVisit.Push(source);
        var visited = new HashSet<FlowNode>();

        while (nodesToVisit.Count > 0)
        {
            var current = nodesToVisit.Pop();

            if (current.Equals(target))
            {
                PathSearch.BuildAugmentingPath(source, target, parentMap, pathFlow);
                return true;
            }

            // A node can be on the stack more than once; it only has to be expanded the first time.
            if (!visited.Add(current)) continue;

            var neighbours = new List<FlowNode>();
            foreach (var edge in PathSearch.SortedOutEdges(flowGraph, current))
            {
                if (edge.Residual <= 0 || visited.Contains(edge.Target)) continue;

                parentMap[edge.Target] = edge;
                neighbours.Add(edge.Target);
            }

            // push in reverse order so that the neighbour with the smallest id is explored first
            for (int i = neighbours.Count - 1; i >= 0; i--)
            {
                nodesToVisit.Push(neighbours[i]);
            }
        }

        return false;
    }
}
