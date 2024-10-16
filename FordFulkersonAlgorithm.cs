using System;
using System.Collections.Generic;

namespace FlowForge;

public class FordFulkersonAlgorithm
{
    private FlowGraph _flowGraph;

    public FordFulkersonAlgorithm(FlowGraph flowGraph)
    {
        _flowGraph = flowGraph;
    }

    public double Run(string sourceId, string targetId)
    {
        FlowNode? source = _flowGraph.GetVertexById(sourceId);
        FlowNode? target = _flowGraph.GetVertexById(targetId);

        if (source == null || target == null)
            throw new ArgumentException("Invalid source or target node.");

        double maxFlow = 0.0;
        // Implement the Ford-Fulkerson algorithm here.
        // Update the CurrentFlow in each FlowEdge as necessary.
        return maxFlow;
    }

    private bool FindAugmentingPath(FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow)
    {
        // Implement BFS/DFS to find an augmenting path and return true if found.
        // This will be used to determine if we can push more flow through the graph.
        return false;
    }
}

