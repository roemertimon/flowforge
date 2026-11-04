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

    [Fact]
    public void UnknownSourceOrSink_Throws()
    {
        var graph = TestNetworks.Example();
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "99");

        Assert.Throws<ArgumentException>(() => algorithm.Run(new BreadthFirstSearchStrategy()));
    }

}
