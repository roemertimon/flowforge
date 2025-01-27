using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowForge;

public class DepthFirstSearchStrategy : ISearchStrategy
{
    public bool FindAugmentingPath(FlowGraph flowGraph, FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow, bool forceWorstCase = false)
    {
        try
        {
            if (forceWorstCase)
            {
                // Attempt to retrieve the nodes by ID
                FlowNode? node2 = flowGraph.GetVertexById("2");
                FlowNode? node3 = flowGraph.GetVertexById("3");

                // Validate that the nodes were found
                if (node2 == null || node3 == null)
                {
                    throw new InvalidOperationException("One or more required vertices (2 or 3) could not be found in the graph.");
                }

                // Attempt to find the edge between node2 and node3
                if (!flowGraph.Graph.TryGetEdge(node2, node3, out var edgeFrom2To3) || edgeFrom2To3 == null)
                {
                    throw new InvalidOperationException("The edge between node2 and node3 does not exist.");
                }

                // Use the worst-case search to find the augmenting path
                WorstCaseSearch worstCaseSearch = new WorstCaseSearch();
                return worstCaseSearch.FindAugmentingPathWithEdge(flowGraph, source, target, edgeFrom2To3, pathFlow);
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
