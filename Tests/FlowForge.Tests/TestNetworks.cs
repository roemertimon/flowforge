using FlowForge.Model;

namespace FlowForge.Tests;

/// <summary>Helpers for building small flow networks and for checking results independently of the production code.</summary>
internal static class TestNetworks
{
    /// <summary>Builds a network with vertices "1".."n" and the given (from, to, capacity) edges.</summary>
    public static FlowGraph Build(int vertexCount, params (int From, int To, double Capacity)[] edges)
    {
        var graph = new FlowGraph();
        for (int i = 1; i <= vertexCount; i++)
        {
            graph.AddVertex(i.ToString());
        }

        foreach (var (from, to, capacity) in edges)
        {
            graph.AddEdge(from.ToString(), to.ToString(), capacity);
        }

        return graph;
    }

    /// <summary>The five-node example network shown by the application (maximum flow 40).</summary>
    public static FlowGraph Example() => Build(5,
        (1, 2, 20), (1, 3, 20), (2, 3, 1), (2, 4, 20), (3, 4, 20), (4, 5, 40));

    /// <summary>Reference maximum flow (Edmonds-Karp on a capacity matrix) used to cross-check the implementation.</summary>
    public static double ReferenceMaxFlow(int vertexCount, IEnumerable<(int From, int To, double Capacity)> edges)
    {
        var capacity = new double[vertexCount + 1, vertexCount + 1];
        foreach (var (from, to, cap) in edges)
        {
            capacity[from, to] += cap;
        }

        int source = 1, sink = vertexCount;
        double total = 0;

        while (true)
        {
            var parent = new int[vertexCount + 1];
            Array.Fill(parent, -1);
            parent[source] = source;
            var queue = new Queue<int>();
            queue.Enqueue(source);

            while (queue.Count > 0 && parent[sink] < 0)
            {
                int u = queue.Dequeue();
                for (int v = 1; v <= vertexCount; v++)
                {
                    if (parent[v] < 0 && capacity[u, v] > 0)
                    {
                        parent[v] = u;
                        queue.Enqueue(v);
                    }
                }
            }

            if (parent[sink] < 0) return total;

            double bottleneck = double.MaxValue;
            for (int v = sink; v != source; v = parent[v])
            {
                bottleneck = Math.Min(bottleneck, capacity[parent[v], v]);
            }

            for (int v = sink; v != source; v = parent[v])
            {
                capacity[parent[v], v] -= bottleneck;
                capacity[v, parent[v]] += bottleneck;
            }

            total += bottleneck;
        }
    }

    /// <summary>Asserts that the flow stored on the edges respects the original capacities and flow conservation.</summary>
    public static void AssertValidFlow(FlowGraph graph, IReadOnlyDictionary<(string, string), double> capacities,
        string sourceId, string sinkId, double expectedValue)
    {
        var balance = new Dictionary<string, double>();

        foreach (var edge in graph.GetEdges())
        {
            Assert.False(edge.IsBackwards, "Residual edges must be removed after the run.");
            Assert.True(capacities.TryGetValue((edge.Source.Id, edge.Target.Id), out double capacity),
                $"Unexpected edge {edge.Source.Id} -> {edge.Target.Id}");
            Assert.Equal(capacity, edge.MaxFlow);
            Assert.InRange(edge.CurrentFlow, 0, capacity);

            balance[edge.Source.Id] = balance.GetValueOrDefault(edge.Source.Id) - edge.CurrentFlow;
            balance[edge.Target.Id] = balance.GetValueOrDefault(edge.Target.Id) + edge.CurrentFlow;
        }

        foreach (var (vertex, value) in balance)
        {
            if (vertex == sourceId)
            {
                Assert.Equal(-expectedValue, value, 9);
            }
            else if (vertex == sinkId)
            {
                Assert.Equal(expectedValue, value, 9);
            }
            else
            {
                Assert.Equal(0, value, 9);
            }
        }
    }

    /// <summary>Captures the capacity of every edge so that it can be compared after an algorithm run.</summary>
    public static Dictionary<(string, string), double> CaptureCapacities(FlowGraph graph) =>
        graph.GetEdges().ToDictionary(edge => (edge.Source.Id, edge.Target.Id), edge => edge.MaxFlow);
}
