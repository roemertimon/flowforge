using FlowForge.Algorithms;
using FlowForge.Model;

namespace FlowForge.Tests;

public class FordFulkersonAlgorithmTests
{
    public static TheoryData<string> Strategies => new() { "dfs", "bfs" };

    private static ISearchStrategy Create(string name) =>
        name == "dfs" ? new DepthFirstSearchStrategy() : new BreadthFirstSearchStrategy();

    /// <summary>Runs the algorithm from vertex "1" to the highest vertex and verifies the resulting flow.</summary>
    private static double RunAndVerify(FlowGraph graph, int vertexCount, string strategy)
    {
        var capacities = TestNetworks.CaptureCapacities(graph);
        var algorithm = new FordFulkersonAlgorithm(graph, "1", vertexCount.ToString());

        double flow = algorithm.Run(Create(strategy));

        TestNetworks.AssertValidFlow(graph, capacities, "1", vertexCount.ToString(), flow);
        return flow;
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void ExampleNetwork_HasMaximumFlowOf40(string strategy)
    {
        Assert.Equal(40, RunAndVerify(TestNetworks.Example(), 5, strategy));
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void DisconnectedNetwork_HasZeroFlow(string strategy)
    {
        var graph = TestNetworks.Build(4, (1, 2, 5), (3, 4, 5));

        Assert.Equal(0, RunAndVerify(graph, 4, strategy));
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void ParallelRoutes_AddUp(string strategy)
    {
        var graph = TestNetworks.Build(4, (1, 2, 3), (1, 3, 2), (2, 4, 2), (3, 4, 3));

        Assert.Equal(4, RunAndVerify(graph, 4, strategy));
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void FlowIsRedirectedThroughResidualEdges(string strategy)
    {
        // The first augmenting path 1-2-3-4 has to be partially undone to reach the maximum of 2.
        var graph = TestNetworks.Build(4, (1, 2, 1), (1, 3, 1), (2, 3, 1), (2, 4, 1), (3, 4, 1));

        Assert.Equal(2, RunAndVerify(graph, 4, strategy));
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void RunningTwice_GivesTheSameResult(string strategy)
    {
        var graph = TestNetworks.Example();
        var capacities = TestNetworks.CaptureCapacities(graph);
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "5");

        double first = algorithm.Run(Create(strategy));
        double second = algorithm.Run(Create(strategy));

        Assert.Equal(first, second);
        TestNetworks.AssertValidFlow(graph, capacities, "1", "5", second);
    }

    [Fact]
    public void GraphStates_StartWithAnEmptyFlowAndEndWithTheFinalFlow()
    {
        var graph = TestNetworks.Example();
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "5");

        algorithm.Run(new BreadthFirstSearchStrategy());

        var states = algorithm.GraphStates;
        Assert.True(states.Count >= 3);
        Assert.All(states[0].GetEdges(), edge => Assert.Equal(0, edge.CurrentFlow));
        Assert.DoesNotContain(states[^1].GetEdges(), edge => edge.IsBackwards);
        Assert.Equal(40, states[^1].GetEdges().Where(edge => edge.Target.Id == "5").Sum(edge => edge.CurrentFlow));
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void PartiallyCancelledFlow_KeepsResidualCapacitiesConsistent(string strategy)
    {
        // Found by the random cross-check: augmenting along a backwards edge only partially cancels its flow.
        var graph = TestNetworks.Build(7,
            (1, 2, 8), (7, 4, 6), (4, 3, 10), (1, 5, 1), (2, 5, 7), (6, 1, 5), (3, 1, 11),
            (5, 3, 9), (3, 6, 8), (3, 2, 2), (2, 7, 7), (7, 5, 8), (4, 2, 6), (6, 7, 10));

        Assert.Equal(9, RunAndVerify(graph, 7, strategy));
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void AntiparallelEdges_KeepTheirCapacitiesAndNeverCarryFlowBothWays(string strategy)
    {
        var graph = TestNetworks.Build(3, (3, 1, 10), (3, 2, 2), (2, 3, 9), (2, 1, 8), (1, 2, 6), (1, 3, 1));

        Assert.Equal(7, RunAndVerify(graph, 3, strategy));
        Assert.DoesNotContain(graph.GetEdges(), edge =>
            edge.CurrentFlow > 0 && graph.Graph.TryGetEdge(edge.Target, edge.Source, out var opposite) && opposite.CurrentFlow > 0);
    }

    [Fact]
    public void UnknownSourceOrSink_Throws()
    {
        var graph = TestNetworks.Example();
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "99");

        Assert.Throws<ArgumentException>(() => algorithm.Run(new BreadthFirstSearchStrategy()));
    }

    public static TheoryData<string, bool> RandomNetworkCases => new()
    {
        { "dfs", false }, { "bfs", false }, { "dfs", true }, { "bfs", true },
    };

    [Theory]
    [MemberData(nameof(RandomNetworkCases))]
    public void RandomNetworks_MatchReferenceImplementation(string strategy, bool allowAntiparallelEdges)
    {
        var random = new Random(12345);

        for (int round = 0; round < 1000; round++)
        {
            int vertexCount = random.Next(2, 10);
            var edges = new List<(int From, int To, double Capacity)>();
            var used = new HashSet<(int, int)>();

            for (int i = 0; i < vertexCount * 3; i++)
            {
                int from = random.Next(1, vertexCount + 1);
                int to = random.Next(1, vertexCount + 1);
                if (from == to || used.Contains((from, to))) continue;
                if (!allowAntiparallelEdges && used.Contains((to, from))) continue;
                used.Add((from, to));
                edges.Add((from, to, random.Next(1, 12)));
            }

            var graph = TestNetworks.Build(vertexCount, edges.ToArray());
            string description = $"round {round} ({strategy}), edges: {string.Join(", ", edges)}";

            double flow;
            try
            {
                flow = RunAndVerify(graph, vertexCount, strategy);
            }
            catch (Exception ex)
            {
                throw new Exception($"Run failed in {description}", ex);
            }

            Assert.True(TestNetworks.ReferenceMaxFlow(vertexCount, edges) == flow,
                $"Wrong maximum flow {flow} in {description}");
        }
    }
}
