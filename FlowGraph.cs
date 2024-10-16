using System.Collections.Generic;
using QuikGraph;

namespace FlowForge;

public class FlowGraph
{
    public BidirectionalGraph<FlowNode, FlowEdge> Graph { get; private set; }
    private readonly Dictionary<string, FlowNode> _nodeLookup;

    public FlowGraph()
    {
        Graph = new BidirectionalGraph<FlowNode, FlowEdge>();
        _nodeLookup = new Dictionary<string, FlowNode>();
    }

    public void AddVertex(string id, string? label = null)
    {
        var node = new FlowNode(id, label);
        Graph.AddVertex(node);
        _nodeLookup[id] = node;
    }

    public FlowNode? GetVertexById(string id)
    {
        return _nodeLookup.GetValueOrDefault(id);
    }

    public void AddEdge(string sourceId, string targetId, double maxFlow)
    {
        var sourceNode = GetVertexById(sourceId);
        var targetNode = GetVertexById(targetId);

        if (sourceNode == null || targetNode == null) return;
        
        var edge = new FlowEdge(sourceNode, targetNode, maxFlow);
        Graph.AddEdge(edge);
    }

    public IEnumerable<FlowEdge> GetEdges()
    {
        return Graph.Edges;
    }
}