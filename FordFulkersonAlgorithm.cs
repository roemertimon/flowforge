using System;
using System.Collections.Generic;

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

        // execute as long as there is an augmenting path
        while (FindAugmentingPath(source, target, pathFlow))
        {
            // calculate bottleneck in augmenting path
            double pathMinFlow = double.MaxValue;
            foreach (var edge in pathFlow.Keys)
            {
                pathMinFlow = Math.Min(pathMinFlow, pathFlow[edge]);
            }

            // add flow along the augmenting path
            foreach (var edge in pathFlow.Keys)
            {
                edge.Residual -= pathMinFlow;

                // check if backwards edge exists
                FlowEdge? reverseEdge;
                if (!_flowGraph.Graph.TryGetEdge(edge.Target, edge.Source, out reverseEdge))
                {
                    // create a backwards edge if it doesn't exist
                    reverseEdge = new FlowEdge(edge.Target, edge.Source, edge.MaxFlow);

                    reverseEdge.Residual = 0;
                    reverseEdge.IsBackwards = true;
                    
                    _flowGraph.Graph.AddEdge(reverseEdge);
                }
                
                if (reverseEdge != null) reverseEdge.Residual += pathMinFlow;
            }

            // add to total flow
            maxFlow += pathMinFlow;
        }

        // return maximum flow after no augmenting paths were found anymore
        return maxFlow;
    }


    public bool FindAugmentingPath(FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow)
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
            
            var outEdges = _flowGraph.Graph.OutEdges(current);

            // go through all outgoing edges
            foreach (FlowEdge currentEdge in outEdges)
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
}

