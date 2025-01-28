using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowForge;

public class FordFulkersonAlgorithm
{
    public enum SearchStrategy
    {
        BreadthFirstSearch,
        DepthFirstSearch
    }

    public readonly string SourceId;
    public readonly string TargetId;
    private readonly FlowGraph _flowGraph;
    private readonly List<FlowGraph> _graphStates = new();
    
    public IReadOnlyList<FlowGraph> GraphStates => _graphStates;

    public FordFulkersonAlgorithm(FlowGraph flowGraph, string sourceId, string targetId)
    {
        _flowGraph = flowGraph;
        this.SourceId = sourceId;
        this.TargetId = targetId;
    }

    public double Run(ISearchStrategy strategy, FlowEdge? specialEdge = null)
    {
        // get source and target nodes
        FlowNode? source = _flowGraph.GetVertexById(SourceId);
        FlowNode? target = _flowGraph.GetVertexById(TargetId);

        if (source == null || target == null)
        {
            throw new ArgumentException("Invalid source or target node!");
        }
        Console.WriteLine("Start of Ford-Fulkerson Algorithm...");
        
        if (specialEdge != null)
        {
            Console.WriteLine("Using worst case scenario for edge selection with special edge " + specialEdge);
        }

        double maxFlow = 0.0;
        var pathFlow = new Dictionary<FlowEdge, double>();

        _flowGraph.ResetFlow();
        _graphStates.Clear();
        
        SaveGraphState();

        // execute as long as there is an augmenting path
        while (strategy.FindAugmentingPath(_flowGraph, source, target, pathFlow, specialEdge))
        {
            PrintOrderedPath(pathFlow);
            
            double pathMinFlow = FindBottleneckInPath(pathFlow);

            // add forward flow along the augmenting path
            foreach (var edge in pathFlow.Keys)
            {
                edge.CurrentFlow += pathMinFlow;
                Console.WriteLine($"Updating forward edge {edge.Source.Id} -> {edge.Target.Id}, New Flow: {edge.CurrentFlow}");
            }
            
            // Convert pathFlow to a list of edges for highlighting
            SaveGraphState(pathFlow.Keys.ToList());
            
            // change residual where necessary
            foreach (var edge in pathFlow.Keys.Where(edge => edge.IsBackwards))
            {
                if (!_flowGraph.Graph.TryGetEdge(edge.Target, edge.Source, out var forwardEdge)) continue;
                
                forwardEdge.CurrentFlow -= edge.CurrentFlow;
                edge.CurrentFlow = 0;

                if (forwardEdge.CurrentFlow != 0) continue;
                _flowGraph.Graph.RemoveEdge(edge);
                pathFlow.Remove(edge);
            }
            
            List<FlowEdge> residualChanges = new List<FlowEdge>();
            
            // create residual flow along the augmenting path
            foreach (var edge in pathFlow.Keys)
            {
                // check if backwards edge exists
                if (!_flowGraph.Graph.TryGetEdge(edge.Target, edge.Source, out var reverseEdge))
                {
                    // create a backwards edge if it doesn't exist
                    reverseEdge = new FlowEdge(edge.Target, edge.Source, pathMinFlow);

                    reverseEdge.CurrentFlow = 0;
                    reverseEdge.IsBackwards = true;
                    
                    _flowGraph.Graph.AddEdge(reverseEdge);
                    residualChanges.Add(reverseEdge);
                    
                    Console.WriteLine($"Creating reverse edge {reverseEdge.Source.Id} -> {reverseEdge.Target.Id}");
                }
                else if (reverseEdge != null)
                {
                    reverseEdge.MaxFlow += pathMinFlow;
                    residualChanges.Add(reverseEdge);
                    
                    Console.WriteLine($"Updating reverse edge {reverseEdge.Source.Id} -> {reverseEdge.Target.Id}, New Residual: {reverseEdge.Residual}");
                }
            }

            // add to total flow
            maxFlow += pathMinFlow;
            Console.WriteLine($"Current max flow: {maxFlow}\n");
            
            SaveGraphState(residualChanges);
        }

        Console.WriteLine($"No Augmenting Path found anymore!\n Max flow found: {maxFlow}\n");
        
        _flowGraph.RemoveAllBackwardsEdges();
        SaveGraphState(_flowGraph.GetAllEdgesWithFlow());

        // return maximum flow after no augmenting paths were found anymore
        return maxFlow;
    }

    private static double FindBottleneckInPath(Dictionary<FlowEdge, double> pathFlow)
    {
        // calculate bottleneck in augmenting path
        double pathMinFlow = double.MaxValue;
        foreach (var edge in pathFlow.Keys)
        {
            pathMinFlow = Math.Min(pathMinFlow, pathFlow[edge]);
        }
        Console.WriteLine($"Bottleneck (minimum flow in path): {pathMinFlow}");
        return pathMinFlow;
    }
    
    private static void PrintOrderedPath(Dictionary<FlowEdge, double> pathFlow)
    {
        Console.WriteLine("Augmenting Path found:");
        
        if (!pathFlow.Any())
        {
            Console.WriteLine("pathFlow is empty.");
            return;
        }
        
        // Step 1: Find the starting edge
        FlowEdge startEdge = pathFlow.Keys
            .First(edge => pathFlow.Keys.All(e => e.Target.Id != edge.Source.Id));

        // Step 2: Follow the path from start to end
        var orderedPath = new List<FlowEdge> { startEdge };
        while (true)
        {
            var currentEdge = orderedPath.Last();
            var nextEdge = pathFlow.Keys.FirstOrDefault(edge => edge.Source.Id == currentEdge.Target.Id);
            
            if (nextEdge == null) break; // End of path reached
            orderedPath.Add(nextEdge);
        }

        // Step 3: Output the ordered path
        foreach (var edge in orderedPath)
        {
            Console.WriteLine($"{edge.Source.Id} -> {edge.Target.Id}, Residual Capacity: {pathFlow[edge]}");
        }
    }
    
    private void SaveGraphState(List<FlowEdge>? augmentingPath = null)
    {
        // Deep copy the current graph state
        var snapshot = _flowGraph.Clone();
    
        // If an augmenting path is provided, highlight it in the snapshot
        if (augmentingPath != null)
        {
            foreach (var edge in augmentingPath)
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