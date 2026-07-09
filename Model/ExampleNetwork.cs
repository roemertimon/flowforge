namespace FlowForge.Model;

/// <summary>The small demonstration network that is offered when no graph file is opened.</summary>
public static class ExampleNetwork
{
    /// <summary>Source and sink ids of the example network.</summary>
    public const string SourceId = "1";
    public const string SinkId = "5";

    /// <summary>
    /// Creates a network with five vertices whose maximum flow is 40. The edge 2 → 3 has a tiny capacity, which makes it a
    /// good special edge for demonstrating the worst case of depth-first Ford-Fulkerson.
    /// </summary>
    public static FlowGraph Create()
    {
        var graph = new FlowGraph();

        for (int id = 1; id <= 5; id++)
        {
            graph.AddVertex(id.ToString());
        }

        graph.AddEdge("1", "2", 20);
        graph.AddEdge("1", "3", 20);
        graph.AddEdge("2", "3", 1);
        graph.AddEdge("2", "4", 20);
        graph.AddEdge("3", "4", 20);
        graph.AddEdge("4", "5", 40);

        return graph;
    }
}
