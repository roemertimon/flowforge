using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FlowForge.Model;

namespace FlowForge.Algorithms;

/// <summary>Search helpers shared by the augmenting path strategies.</summary>
internal static class PathSearch
{
    /// <summary>Outgoing edges of a node ordered by the numeric id of their target, which makes every search deterministic.</summary>
    public static List<FlowEdge> SortedOutEdges(FlowGraph flowGraph, FlowNode node)
    {
        return flowGraph.Graph.OutEdges(node)
            .OrderBy(edge => int.Parse(edge.Target.Id, CultureInfo.InvariantCulture))
            .ToList();
    }

    /// <summary>
    /// Breadth-first search for a path with residual capacity from <paramref name="source"/> to <paramref name="target"/>.
    /// Neighbours are visited in ascending id order. Edges in <paramref name="excludedEdges"/> are not used.
    /// </summary>
    /// <returns>True if a path was found; it is then stored in <paramref name="pathFlow"/> together with the residual of each edge.</returns>
    public static bool BreadthFirst(FlowGraph flowGraph, FlowNode source, FlowNode target,
        Dictionary<FlowEdge, double> pathFlow, ISet<FlowEdge>? excludedEdges = null)
    {
        var parentMap = new Dictionary<FlowNode, FlowEdge>();
        var nodesToVisit = new Queue<FlowNode>();
        nodesToVisit.Enqueue(source);
        var visited = new HashSet<FlowNode> { source };

        while (nodesToVisit.Count > 0)
        {
            var current = nodesToVisit.Dequeue();

            foreach (var edge in SortedOutEdges(flowGraph, current))
            {
                if (edge.Residual <= 0 || visited.Contains(edge.Target)) continue;
                if (excludedEdges != null && excludedEdges.Contains(edge)) continue;

                visited.Add(edge.Target);
                parentMap.Add(edge.Target, edge);

                if (edge.Target.Equals(target))
                {
                    BuildAugmentingPath(source, target, parentMap, pathFlow);
                    return true;
                }

                nodesToVisit.Enqueue(edge.Target);
            }
        }

        return false;
    }

    /// <summary>Walks the parent map back from the target to the source and stores the edges of the path with their residuals.</summary>
    public static void BuildAugmentingPath(FlowNode source, FlowNode target,
        Dictionary<FlowNode, FlowEdge> parentMap, Dictionary<FlowEdge, double> pathFlow)
    {
        pathFlow.Clear();

        var currentNode = target;
        while (currentNode != source)
        {
            var pathEdge = parentMap[currentNode];
            pathFlow[pathEdge] = pathEdge.Residual;
            currentNode = pathEdge.Source;
        }
    }
}
