using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowForge;

public class BreadthFirstSearchStrategy : ISearchStrategy
{
    public bool FindAugmentingPath(FlowGraph flowGraph, FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow, bool forceWorstCase = false)
    {
        Console.WriteLine("Breadth Depth First Search...");
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

            List<FlowEdge> sortedOutEdges = forceWorstCase ? outEdges.OrderBy(edge => edge.Residual).ToList() : outEdges.OrderBy(edge => int.Parse(edge.Target.Id)).ToList();
            
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
