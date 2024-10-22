using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowForge;

public class FordFulkersonAlgorithm
{
    private readonly FlowGraph _flowGraph;

    public FordFulkersonAlgorithm(FlowGraph flowGraph)
    {
        _flowGraph = flowGraph;
    }

    public double Run(string sourceId, string targetId)
    {
        // get source and target nodes
        FlowNode? source = _flowGraph.GetVertexById(sourceId);
        FlowNode? target = _flowGraph.GetVertexById(targetId);

        if (source == null || target == null)
            throw new ArgumentException("Invalid source or target node!");

        double maxFlow = 0.0;
        var pathFlow = new Dictionary<FlowEdge, double>();


        var allEdges = _flowGraph.GetEdges();
        foreach (var edge in allEdges)
        {
            edge.Residual = edge.MaxFlow;
        }
        
        Console.WriteLine("Start of Ford-Fulkerson Algorithm...");

        // execute as long as there is an augmenting path
        while (FindAugmentingPathBfs(source, target, pathFlow))
        {
            Console.WriteLine("Augmenting Path found:");
            foreach (var edge in pathFlow.Keys)
            {
                Console.WriteLine($"{edge.Source.Id} -> {edge.Target.Id}, Residual Capacity: {pathFlow[edge]}");
            }
            
            // calculate bottleneck in augmenting path
            double pathMinFlow = double.MaxValue;
            foreach (var edge in pathFlow.Keys)
            {
                pathMinFlow = Math.Min(pathMinFlow, pathFlow[edge]);
            }
            Console.WriteLine($"Bottleneck (minimum flow in path): {pathMinFlow}");

            // add flow along the augmenting path
            foreach (var edge in pathFlow.Keys)
            {
                edge.Residual -= pathMinFlow;
                Console.WriteLine($"Updating forward edge {edge.Source.Id} -> {edge.Target.Id}, New Residual: {edge.Residual}");


                // check if backwards edge exists
                FlowEdge? reverseEdge;
                if (!_flowGraph.Graph.TryGetEdge(edge.Target, edge.Source, out reverseEdge))
                {
                    // create a backwards edge if it doesn't exist
                    reverseEdge = new FlowEdge(edge.Target, edge.Source, edge.MaxFlow);

                    reverseEdge.Residual = 0;
                    reverseEdge.IsBackwards = true;
                    
                    _flowGraph.Graph.AddEdge(reverseEdge);
                    
                    Console.WriteLine($"Creating reverse edge {reverseEdge.Source.Id} -> {reverseEdge.Target.Id}");
                }

                if (reverseEdge != null)
                {
                    reverseEdge.Residual += pathMinFlow;
                    
                    Console.WriteLine($"Updating reverse edge {reverseEdge.Source.Id} -> {reverseEdge.Target.Id}, New Residual: {reverseEdge.Residual}");
                }
            }

            // add to total flow
            maxFlow += pathMinFlow;
            Console.WriteLine($"Current max flow: {maxFlow}\n");
        }

        Console.WriteLine("No Augmenting Path found anymore!\n");

        Console.WriteLine($"Max flow found: {maxFlow}\n");
        // return maximum flow after no augmenting paths were found anymore
        return maxFlow;
    }


    public bool FindAugmentingPathBfs(FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow)
    {
        // parent map to walk back path
        var parentMap = new Dictionary<FlowNode, FlowEdge>();
        
        // queue for breath first search
        var queue = new Queue<FlowNode>();
        queue.Enqueue(source);
        
        // map to store visited nodes
        var visited = new HashSet<FlowNode> { source };

        while (queue.Count > 0)
        {
            FlowNode current = queue.Dequeue();
            
            var outEdges = _flowGraph.Graph.OutEdges(current).ToList();
            
            var sortedOutEdges = outEdges.OrderBy(edge => edge.Target.Id);

            // go through all outgoing edges
            foreach (FlowEdge currentEdge in sortedOutEdges)
            {
                if (currentEdge.Residual <= 0 || visited.Contains(currentEdge.Target)) continue;
                
                visited.Add(currentEdge.Target);
                parentMap.Add(currentEdge.Target, currentEdge);
                
                // if we reached the target node
                if (currentEdge.Target.Equals(target))
                {
                    FlowNode currentNode = target;
                    pathFlow.Clear();
                    
                    while (currentNode != source)
                    {
                        FlowEdge pathEdge = parentMap[currentNode];
                        pathFlow.Add(pathEdge, pathEdge.Residual);
                        currentNode = pathEdge.Source;  
                    }
                        
                    // return the augmenting path found
                    return true;
                }
                
                // search further
                queue.Enqueue(currentEdge.Target);
            }
        }
        
        return false;
    }
    
    public bool FindAugmentingPathDfs(FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow)
    {
        // parent map to walk back path
        var parentMap = new Dictionary<FlowNode, FlowEdge>();
        
        // queue for breath first search
        var stack = new Stack<FlowNode>();
        stack.Push(source);
        
        // map to store visited nodes
        var visited = new HashSet<FlowNode> { source };

        while (stack.Count > 0)
        {
            FlowNode current = stack.Pop();
            
            var outEdges = _flowGraph.Graph.OutEdges(current).ToList();
            
            var sortedOutEdges = outEdges.OrderBy(edge => edge.Target.Id);

            // go through all outgoing edges
            foreach (FlowEdge currentEdge in sortedOutEdges)
            {
                if (currentEdge.Residual <= 0 || visited.Contains(currentEdge.Target)) continue;
                
                visited.Add(currentEdge.Target);
                parentMap.Add(currentEdge.Target, currentEdge);
                
                // if we reached the target node
                if (currentEdge.Target.Equals(target))
                {
                    FlowNode currentNode = target;
                    pathFlow.Clear();
                    
                    while (currentNode != source)
                    {
                        FlowEdge pathEdge = parentMap[currentNode];
                        pathFlow.Add(pathEdge, pathEdge.Residual);
                        currentNode = pathEdge.Source;  
                    }
                        
                    // return the augmenting path found
                    return true;
                }
                
                // search further
                stack.Push(currentEdge.Target);
            }
        }
        
        return false;
    }
}

