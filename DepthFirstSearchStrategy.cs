using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowForge;

public class DepthFirstSearchStrategy : ISearchStrategy
{
    public bool FindAugmentingPath(FlowGraph flowGraph, FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow, FlowEdge? specialEdge = null)
    {
        try
        {
            if (specialEdge != null)
            {
                // Use the worst-case search to find the augmenting path
                WorstCaseSearch worstCaseSearch = new WorstCaseSearch();
                return WorstCaseSearch.FindAugmentingPathWithEdge(flowGraph, source, target, specialEdge, pathFlow);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("An error occurred during the worst-case search. Defaulting to normal!");
        }
        
        // parent map to walk back path
        var parentMap = new Dictionary<FlowNode, FlowEdge>();
        
        Stack<FlowNode> nodesToVisit = new Stack<FlowNode>();
        nodesToVisit.Push(source);

        // map to store visited nodes
        var visited = new HashSet<FlowNode>();

        while (nodesToVisit.Any())
        {
            var current = nodesToVisit.Pop();
            Console.WriteLine("Current Node: " + current);
            
            // if we reached the target node
            if (current.Equals(target))
            {
                ISearchStrategy.BuildAugmentingPath(source, target, parentMap, pathFlow);
                return true;
            }
            
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
                
                parentMap[currentEdge.Target] = currentEdge;
                nodesToAdd.Add(currentEdge.Target);
            }
            
            foreach (var node in nodesToAdd.OrderByDescending(n => int.Parse(n.Id)))
            {
                nodesToVisit.Push(node);
            }
            
            visited.Add(current);
        }
        
        return false;
    }
}
