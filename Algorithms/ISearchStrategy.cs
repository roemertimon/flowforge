using System.Collections.Generic;
using FlowForge.Model;

namespace FlowForge.Algorithms;

/// <summary>Strategy that finds an augmenting path with residual capacity in the current residual network.</summary>
public interface ISearchStrategy
{
    /// <summary>Searches an augmenting path from <paramref name="source"/> to <paramref name="target"/>.</summary>
    /// <param name="flowGraph">The graph including the residual (backwards) edges.</param>
    /// <param name="pathFlow">Receives the edges of the path with their residual capacity when a path was found.</param>
    /// <param name="specialEdge">Optional edge the path should be routed through (only used by the depth-first strategy).</param>
    /// <returns>True if an augmenting path exists.</returns>
    bool FindAugmentingPath(FlowGraph flowGraph, FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow, FlowEdge? specialEdge = null);
}
