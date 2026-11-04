using System.Collections.Generic;
using FlowForge.Model;

namespace FlowForge.Algorithms;

/// <summary>Finds shortest augmenting paths (fewest edges). Together with Ford-Fulkerson this is the Edmonds-Karp algorithm.</summary>
public class BreadthFirstSearchStrategy : ISearchStrategy
{
    public bool FindAugmentingPath(FlowGraph flowGraph, FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow, FlowEdge? specialEdge = null)
    {
        return PathSearch.BreadthFirst(flowGraph, source, target, pathFlow);
    }
}
