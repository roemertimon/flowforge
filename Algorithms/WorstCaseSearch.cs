using System.Collections.Generic;
using FlowForge.Model;

namespace FlowForge.Algorithms;

/// <summary>
/// Chooses augmenting paths that run through a given edge. With a depth-first Ford-Fulkerson this reproduces the classic
/// worst case: when the edge in the middle of the network has a tiny capacity, every augmentation moves only that
/// capacity forwards and then takes it back again through the residual edge, so the number of augmentations grows with
/// the capacities instead of the size of the graph.
/// </summary>
public static class WorstCaseSearch
{
    /// <summary>
    /// Finds an augmenting path that uses <paramref name="requiredEdge"/>, or its residual edge if the edge itself is saturated.
    /// The path leading to the edge and the path leaving it are shortest paths that do not share any edge.
    /// If no such path exists, a plain breadth-first path is returned instead.
    /// </summary>
    public static bool FindAugmentingPathWithEdge(FlowGraph flowGraph, FlowNode source, FlowNode target, FlowEdge requiredEdge, Dictionary<FlowEdge, double> pathFlow)
    {
        var usableEdge = FindUsableEdge(flowGraph, requiredEdge);

        if (usableEdge != null && TryFindPathThrough(flowGraph, source, target, usableEdge, pathFlow))
        {
            return true;
        }

        return PathSearch.BreadthFirst(flowGraph, source, target, pathFlow);
    }

    /// <summary>Returns the required edge or, if it is saturated, the opposite edge, whichever still has residual capacity.</summary>
    private static FlowEdge? FindUsableEdge(FlowGraph flowGraph, FlowEdge requiredEdge)
    {
        if (flowGraph.Graph.TryGetEdge(requiredEdge.Source, requiredEdge.Target, out var forwardEdge) && forwardEdge.Residual > 0)
        {
            return forwardEdge;
        }

        if (flowGraph.Graph.TryGetEdge(requiredEdge.Target, requiredEdge.Source, out var oppositeEdge) && oppositeEdge.Residual > 0)
        {
            return oppositeEdge;
        }

        return null;
    }

    private static bool TryFindPathThrough(FlowGraph flowGraph, FlowNode source, FlowNode target, FlowEdge viaEdge, Dictionary<FlowEdge, double> pathFlow)
    {
        var pathToEdge = new Dictionary<FlowEdge, double>();
        if (!source.Equals(viaEdge.Source) && !PathSearch.BreadthFirst(flowGraph, source, viaEdge.Source, pathToEdge))
        {
            return false;
        }

        var usedEdges = new HashSet<FlowEdge>(pathToEdge.Keys) { viaEdge };

        var pathFromEdge = new Dictionary<FlowEdge, double>();
        if (!viaEdge.Target.Equals(target) && !PathSearch.BreadthFirst(flowGraph, viaEdge.Target, target, pathFromEdge, usedEdges))
        {
            return false;
        }

        pathFlow.Clear();
        foreach (var (edge, residual) in pathToEdge)
        {
            pathFlow[edge] = residual;
        }

        pathFlow[viaEdge] = viaEdge.Residual;

        foreach (var (edge, residual) in pathFromEdge)
        {
            pathFlow[edge] = residual;
        }

        return true;
    }
}
