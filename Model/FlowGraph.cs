using System.Collections.Generic;
using System.Linq;
using QuikGraph;

namespace FlowForge.Model;

/// <summary>A flow network: vertices identified by string ids and directed edges carrying a capacity and a current flow.</summary>
public class FlowGraph
{
    public BidirectionalGraph<FlowNode, FlowEdge> Graph { get; }
    private readonly Dictionary<string, FlowNode> _nodeLookup;

    public FlowGraph()
    {
        Graph = new BidirectionalGraph<FlowNode, FlowEdge>();
        _nodeLookup = new Dictionary<string, FlowNode>();
    }

    /// <summary>Adds a vertex. Adding an id that already exists does nothing.</summary>
    public void AddVertex(string id, string? label = null)
    {
        if (_nodeLookup.ContainsKey(id)) return;

        var node = new FlowNode(id, label);
        Graph.AddVertex(node);
        _nodeLookup[id] = node;
    }

    public FlowNode? GetVertexById(string id)
    {
        return _nodeLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Adds an edge between two existing vertices. If an edge with the same direction already exists, its capacity is
    /// increased instead, because parallel edges are equivalent to a single edge with the summed capacity.
    /// </summary>
    public void AddEdge(string sourceId, string targetId, double maxFlow)
    {
        var sourceNode = GetVertexById(sourceId);
        var targetNode = GetVertexById(targetId);

        if (sourceNode == null || targetNode == null) return;

        if (Graph.TryGetEdge(sourceNode, targetNode, out var existingEdge) && !existingEdge.IsBackwards)
        {
            existingEdge.MaxFlow += maxFlow;
            return;
        }

        Graph.AddEdge(new FlowEdge(sourceNode, targetNode, maxFlow));
    }
    
    public void RemoveEdge(string sourceId, string targetId)
    {
        // Get the source and target nodes
        var sourceNode = GetVertexById(sourceId);
        var targetNode = GetVertexById(targetId);

        if (sourceNode == null || targetNode == null)
        {
            return;
        }

        // Find the edge in the graph
        Graph.TryGetEdge(sourceNode, targetNode, out var edge);

        if (edge == null)
        {
            return;
        }

        // Remove the edge from the graph
        Graph.RemoveEdge(edge);
    }

    
    public void AddEdge(FlowEdge edge)
    {
        Graph.AddEdge(edge);
    }

    public void ResetFlow()
    {
        List<FlowEdge> allEdges = GetEdges().ToList();
        foreach (var edge in allEdges)
        {
            edge.CurrentFlow = 0;
        }
    }

    public void RemoveAllBackwardsEdges()
    {
        List<FlowEdge> allEdges = GetEdges().ToList();
        foreach (var edge in allEdges.Where(edge => edge.IsBackwards))
        {
            Graph.RemoveEdge(edge);
        }
    }
    
    public List<FlowEdge> GetAllEdgesWithFlow()
    {
        return GetEdges().Where(edge => edge.CurrentFlow > 0).ToList();
    }

    public IEnumerable<FlowEdge> GetEdges()
    {
        return Graph.Edges;
    }
    
    // Clone method
    public FlowGraph Clone()
    {
        var clonedGraph = new FlowGraph();

        // Clone all vertices
        foreach (var vertex in Graph.Vertices)
        {
            clonedGraph.AddVertex(vertex.Id, vertex.Label);
        }

        // Clone all edges
        foreach (var edge in Graph.Edges)
        {
            var source = clonedGraph.GetVertexById(edge.Source.Id);
            var target = clonedGraph.GetVertexById(edge.Target.Id);

            if (source != null && target != null)
            {
                var clonedEdge = new FlowEdge(source, target, edge.MaxFlow)
                {
                    CurrentFlow = edge.CurrentFlow,
                    IsBackwards = edge.IsBackwards
                };
                clonedGraph.AddEdge(clonedEdge);
            }
        }

        return clonedGraph;
    }
}