using System;
using System.Collections.Generic;
using System.Linq;
using FlowForge.Model;

namespace FlowForge.Algorithms;

/// <summary>
/// Computes a maximum flow with the Ford-Fulkerson method. The way augmenting paths are found is delegated to an
/// <see cref="ISearchStrategy"/>: depth-first search gives plain Ford-Fulkerson, breadth-first search gives Edmonds-Karp.
/// </summary>
/// <remarks>
/// The residual network is stored in the graph itself: for every edge that carries flow a backwards edge with
/// <see cref="FlowEdge.IsBackwards"/> set is added, whose capacity equals the flow on the forward edge. All backwards
/// edges are removed again when the run has finished. After every step a snapshot of the graph is recorded in
/// <see cref="GraphStates"/> so that the run can be replayed (for example as a TikZ animation).
/// </remarks>
public class FordFulkersonAlgorithm
{
    public readonly string SourceId;
    public readonly string TargetId;
    private readonly FlowGraph _flowGraph;
    private readonly List<FlowGraph> _graphStates = new();

    /// <summary>Snapshots recorded during the last run: the initial graph, two per augmentation and the final flow.</summary>
    public IReadOnlyList<FlowGraph> GraphStates => _graphStates;

    /// <summary>The number of augmenting paths that were needed in the last run.</summary>
    public int AugmentationCount { get; private set; }

    public FordFulkersonAlgorithm(FlowGraph flowGraph, string sourceId, string targetId)
    {
        _flowGraph = flowGraph;
        SourceId = sourceId;
        TargetId = targetId;
    }

    /// <summary>Runs the algorithm on the graph, leaving the resulting flow on its edges.</summary>
    /// <param name="strategy">Determines how augmenting paths are searched.</param>
    /// <param name="specialEdge">Optional edge that depth-first search should try to route every path through (worst case demonstration).</param>
    /// <returns>The value of the maximum flow.</returns>
    public double Run(ISearchStrategy strategy, FlowEdge? specialEdge = null)
    {
        FlowNode? source = _flowGraph.GetVertexById(SourceId);
        FlowNode? target = _flowGraph.GetVertexById(TargetId);

        if (source == null || target == null)
        {
            throw new ArgumentException("Invalid source or target node!");
        }

        _flowGraph.RemoveAllBackwardsEdges();
        _flowGraph.ResetFlow();
        _graphStates.Clear();
        AugmentationCount = 0;

        var originalCapacities = _flowGraph.GetEdges().ToDictionary(edge => edge, edge => edge.MaxFlow);

        double maxFlow = 0.0;
        var pathFlow = new Dictionary<FlowEdge, double>();

        SaveGraphState();

        while (strategy.FindAugmentingPath(_flowGraph, source, target, pathFlow, specialEdge))
        {
            double bottleneck = pathFlow.Values.Min();
            var path = pathFlow.Keys.ToList();

            foreach (var edge in path)
            {
                edge.CurrentFlow += bottleneck;
            }

            SaveGraphState(path);

            var residualChanges = new List<FlowEdge>();
            foreach (var edge in path)
            {
                if (edge.IsBackwards)
                {
                    CancelFlow(edge, bottleneck, residualChanges);
                }
                else
                {
                    AddResidualCapacity(edge, bottleneck, residualChanges);
                }
            }

            maxFlow += bottleneck;
            AugmentationCount++;
            SaveGraphState(residualChanges);
        }

        _flowGraph.RemoveAllBackwardsEdges();
        RestoreCapacities(originalCapacities);
        SaveGraphState(_flowGraph.GetAllEdgesWithFlow());

        return maxFlow;
    }

    /// <summary>Makes sure that the reverse direction of an augmented edge offers as much residual capacity as the edge now carries.</summary>
    private void AddResidualCapacity(FlowEdge edge, double amount, List<FlowEdge> residualChanges)
    {
        if (!_flowGraph.Graph.TryGetEdge(edge.Target, edge.Source, out var reverseEdge))
        {
            reverseEdge = new FlowEdge(edge.Target, edge.Source, edge.CurrentFlow) { IsBackwards = true };
            _flowGraph.Graph.AddEdge(reverseEdge);
        }
        else if (reverseEdge.IsBackwards)
        {
            reverseEdge.MaxFlow = edge.CurrentFlow;
        }
        else
        {
            // Both directions are real input edges: raise the capacity of the opposite one temporarily so that its
            // residual reflects the net flow. The original capacities are restored when the run has finished.
            reverseEdge.MaxFlow += amount;
        }

        residualChanges.Add(reverseEdge);
    }

    /// <summary>Takes back <paramref name="amount"/> of flow from the forward edge that belongs to a backwards edge.</summary>
    private void CancelFlow(FlowEdge backwardsEdge, double amount, List<FlowEdge> residualChanges)
    {
        backwardsEdge.CurrentFlow = 0;

        if (!_flowGraph.Graph.TryGetEdge(backwardsEdge.Target, backwardsEdge.Source, out var forwardEdge))
        {
            return;
        }

        forwardEdge.CurrentFlow -= amount;
        residualChanges.Add(forwardEdge);

        if (forwardEdge.CurrentFlow > 0)
        {
            backwardsEdge.MaxFlow = forwardEdge.CurrentFlow;
            residualChanges.Add(backwardsEdge);
        }
        else
        {
            _flowGraph.Graph.RemoveEdge(backwardsEdge);
        }
    }

    /// <summary>Undoes temporary capacity changes and cancels flow that runs in both directions of an input edge pair.</summary>
    private void RestoreCapacities(Dictionary<FlowEdge, double> originalCapacities)
    {
        foreach (var (edge, capacity) in originalCapacities)
        {
            edge.MaxFlow = capacity;
        }

        foreach (var edge in originalCapacities.Keys)
        {
            if (!_flowGraph.Graph.TryGetEdge(edge.Target, edge.Source, out var opposite) || ReferenceEquals(opposite, edge))
            {
                continue;
            }

            double cancelled = Math.Min(edge.CurrentFlow, opposite.CurrentFlow);
            edge.CurrentFlow -= cancelled;
            opposite.CurrentFlow -= cancelled;
        }
    }

    private void SaveGraphState(List<FlowEdge>? highlightedEdges = null)
    {
        var snapshot = _flowGraph.Clone();

        if (highlightedEdges != null)
        {
            foreach (var edge in highlightedEdges)
            {
                var edgeInSnapshot = snapshot.Graph.Edges.FirstOrDefault(e => e.Source.Id == edge.Source.Id && e.Target.Id == edge.Target.Id);
                if (edgeInSnapshot != null)
                {
                    edgeInSnapshot.IsHighlighted = true;
                }
            }
        }

        _graphStates.Add(snapshot);
    }
}
