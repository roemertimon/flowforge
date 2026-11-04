using System.Collections.Generic;
using FlowForge.Model;

namespace FlowForge.Algorithms;

public interface ISearchStrategy
{
    bool FindAugmentingPath(FlowGraph flowGraph, FlowNode source, FlowNode target, Dictionary<FlowEdge, double> pathFlow, FlowEdge? specialEdge = null);
    
    public static void BuildAugmentingPath(FlowNode source, FlowNode target, Dictionary<FlowNode, FlowEdge> parentMap, Dictionary<FlowEdge, double> pathFlow)
    {
        FlowNode currentNode = target;
        pathFlow.Clear();

        while (currentNode != source)
        {
            FlowEdge pathEdge = parentMap[currentNode];
            pathFlow[pathEdge] = pathEdge.Residual;
            currentNode = pathEdge.Source;
        }
    }

}
