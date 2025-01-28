using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowForge;

public class WorstCaseSearch
{
    public static bool FindAugmentingPathWithEdge(FlowGraph flowGraph, FlowNode source, FlowNode target, FlowEdge requiredEdge, Dictionary<FlowEdge, double> pathFlow)
    {
        // Determine if the required edge is valid in either direction
        FlowEdge? validEdge;

        if (requiredEdge.Residual > 0)
        {
            validEdge = requiredEdge;
            Console.WriteLine($"Searching for augmenting path that includes edge {requiredEdge.Source.Id} -> {requiredEdge.Target.Id}");
        }
        else if (flowGraph.Graph.TryGetEdge(requiredEdge.Target, requiredEdge.Source, out validEdge))
        {
            if (validEdge.Residual > 0)
            {
                Console.WriteLine($"Searching for augmenting path that includes edge {requiredEdge.Target.Id} -> {requiredEdge.Source.Id}");
            }
            else
            {
                validEdge = null;
            }
        }
        else
        {
            validEdge = null;
        }

        if (validEdge == null)
        {
            Console.WriteLine("No valid residual capacity on the required edge in either direction.");
            return FindAugmentingPath(flowGraph, source, target, pathFlow);
        }

        // Phase 1: Find a path from source to the starting node of the valid edge
        var phase1Path = new Dictionary<FlowEdge, double>();
        bool phase1Success = FindAugmentingPath(flowGraph, source, validEdge.Source, phase1Path);

        if (!phase1Success)
        {
            Console.WriteLine("No path found from source to the required edge.");
            return FindAugmentingPath(flowGraph, source, target, pathFlow);
        }
        
        // Print the Phase 1 path
        Console.WriteLine("Phase 1 Path:");
        foreach (var edge in phase1Path)
        {
            Console.WriteLine($"{edge.Key.Source.Id} -> {edge.Key.Target.Id}, Flow: {edge.Value}");
        }
        
        // Create a copy of the graph and update it
        var updatedGraph = flowGraph.Clone();
        RemoveEdgesFromGraph(updatedGraph, phase1Path);

        // Phase 2: Find a path from the ending node of the valid edge to the target
        var phase2Path = new Dictionary<FlowEdge, double>();
        var phase2Success = FindAugmentingPath(updatedGraph, validEdge.Target, target, phase2Path);

        if (!phase2Success)
        {
            Console.WriteLine("No path found from the required edge to the target.");
            return FindAugmentingPath(flowGraph, source, target, pathFlow);
        }

        Console.WriteLine("Phase 1 Path:");
        foreach (var kvp in phase1Path)
        {
            Console.WriteLine($"Edge: {kvp.Key.Source.Id} -> {kvp.Key.Target.Id}, Capacity: {kvp.Value}");
        }

        Console.WriteLine($"Valid Edge: {validEdge.Source.Id} -> {validEdge.Target.Id}, Residual: {validEdge.Residual}");

        Console.WriteLine("Phase 2 Path:");
        foreach (var kvp in phase2Path)
        {
            Console.WriteLine($"Edge: {kvp.Key.Source.Id} -> {kvp.Key.Target.Id}, Capacity: {kvp.Value}");
        }

        
        // Combine the results
        pathFlow.Clear();
        foreach (var kvp in phase1Path)
        {
            pathFlow[kvp.Key] = kvp.Value;
        }
        pathFlow[validEdge] = validEdge.Residual; // Include the required edge
        foreach (var kvp in phase2Path)
        {
            pathFlow[kvp.Key] = kvp.Value;
        }

        Console.WriteLine("Augmenting path including the required edge found!");
        return true;
    }
    
    private static void RemoveEdgesFromGraph(FlowGraph graph, Dictionary<FlowEdge, double> path)
    {
        foreach (var edge in path.Keys)
        {
            var source = graph.GetVertexById(edge.Source.Id);
            var target = graph.GetVertexById(edge.Target.Id);

            if (source != null && target != null)
            {
                graph.RemoveEdge(source.Id, target.Id);
            }
        }
    }

    private static bool FindAugmentingPath(FlowGraph flowGraph, FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow)
    {
        Console.WriteLine($"Searching for sub-augmenting path from {source.Id} -> {target.Id}");

        if (source.Equals(target))
        {
            return true;
        }
        // parent map to walk back path
        var parentMap = new Dictionary<FlowNode, FlowEdge>();
        
        Queue<FlowNode> nodesToVisit = new Queue<FlowNode>();
        nodesToVisit.Enqueue(source);

        // map to store visited nodes
        var visited = new HashSet<FlowNode> {source};

        while (nodesToVisit.Any())
        {
            var current = nodesToVisit.Dequeue();

            Console.WriteLine("Current Node: " + current);
            var outEdges = flowGraph.Graph.OutEdges(current).ToList();

            List<FlowEdge> sortedOutEdges = outEdges.OrderBy(edge => int.Parse(edge.Target.Id)).ToList();
            
            Console.WriteLine("Sorted out edges for node:");
            foreach (FlowEdge edge in sortedOutEdges)
            {
                Console.WriteLine($"{edge.Source.Id} -> {edge.Target.Id}, Current Flow: {edge.CurrentFlow}");
            }
            
            Console.WriteLine("Looping through:");

            List<FlowNode> nodesToAdd = new List<FlowNode>();
            
            // go through all outgoing edges
            foreach (FlowEdge currentEdge in sortedOutEdges)
            {
                
                if (currentEdge.Residual <= 0 || visited.Contains(currentEdge.Target)) continue;
                
                Console.WriteLine($"{currentEdge.Source.Id} -> {currentEdge.Target.Id}, Current Flow: {currentEdge.CurrentFlow}");
                
                visited.Add(currentEdge.Target);
                parentMap.Add(currentEdge.Target, currentEdge);
                
                // if we reached the target node
                if (currentEdge.Target.Equals(target))
                {
                    ISearchStrategy.BuildAugmentingPath(source, target, parentMap, pathFlow);
                    return true;
                }
                
                nodesToAdd.Add(currentEdge.Target);
            }
            
            foreach (var node in nodesToAdd.OrderBy(n => int.Parse(n.Id)))
            {
                nodesToVisit.Enqueue(node);
            }
        }
        
        return false;
    }
}