using FlowForge.Algorithms;
using FlowForge.Model;

namespace FlowForge.Tests;

public class SearchStrategyTests
{
    private static HashSet<string> FindPath(ISearchStrategy strategy, FlowGraph graph, string source, string target, FlowEdge? specialEdge = null)
    {
        var pathFlow = new Dictionary<FlowEdge, double>();
        bool found = strategy.FindAugmentingPath(graph, graph.GetVertexById(source)!, graph.GetVertexById(target)!, pathFlow, specialEdge);

        Assert.True(found);
        return pathFlow.Keys.Select(edge => $"{edge.Source.Id}-{edge.Target.Id}").ToHashSet();
    }

    private static FlowEdge Edge(FlowGraph graph, string from, string to)
    {
        Assert.True(graph.Graph.TryGetEdge(graph.GetVertexById(from)!, graph.GetVertexById(to)!, out var edge));
        return edge;
    }

    [Fact]
    public void BreadthFirst_PrefersTheShortestPath()
    {
        var graph = TestNetworks.Build(4, (1, 2, 5), (2, 3, 5), (3, 4, 5), (1, 4, 1));

        Assert.Equal(new HashSet<string> { "1-4" }, FindPath(new BreadthFirstSearchStrategy(), graph, "1", "4"));
    }

    [Fact]
    public void DepthFirst_ExploresTheSmallestIdFirst()
    {
        var graph = TestNetworks.Build(4, (1, 2, 5), (2, 3, 5), (3, 4, 5), (1, 4, 1));

        Assert.Equal(new HashSet<string> { "1-2", "2-3", "3-4" }, FindPath(new DepthFirstSearchStrategy(), graph, "1", "4"));
    }

    [Theory]
    [InlineData("dfs")]
    [InlineData("bfs")]
    public void EdgesWithoutResidualCapacityAreNotUsed(string strategy)
    {
        var graph = TestNetworks.Build(3, (1, 2, 5), (2, 3, 5), (1, 3, 5));
        Edge(graph, "1", "3").CurrentFlow = 5;
        ISearchStrategy search = strategy == "dfs" ? new DepthFirstSearchStrategy() : new BreadthFirstSearchStrategy();

        Assert.Equal(new HashSet<string> { "1-2", "2-3" }, FindPath(search, graph, "1", "3"));
    }

    [Fact]
    public void NoPathWhenTheTargetIsUnreachable()
    {
        var graph = TestNetworks.Build(3, (1, 2, 5));
        var pathFlow = new Dictionary<FlowEdge, double>();
        var source = graph.GetVertexById("1")!;
        var target = graph.GetVertexById("3")!;

        Assert.False(new DepthFirstSearchStrategy().FindAugmentingPath(graph, source, target, pathFlow));
        Assert.False(new BreadthFirstSearchStrategy().FindAugmentingPath(graph, source, target, pathFlow));
    }

    [Theory]
    [InlineData("dfs")]
    [InlineData("bfs")]
    public void SelfLoopsAreIgnored(string strategy)
    {
        var graph = TestNetworks.Build(3, (1, 2, 5), (2, 2, 7), (2, 3, 4));
        var capacities = TestNetworks.CaptureCapacities(graph);
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "3");

        double flow = algorithm.Run(strategy == "dfs" ? new DepthFirstSearchStrategy() : new BreadthFirstSearchStrategy());

        Assert.Equal(4, flow);
        TestNetworks.AssertValidFlow(graph, capacities, "1", "3", flow);
    }

    [Fact]
    public void WorstCase_RoutesEveryAugmentationThroughTheSpecialEdge()
    {
        var graph = TestNetworks.Example();
        var specialEdge = Edge(graph, "2", "3");
        var capacities = TestNetworks.CaptureCapacities(graph);
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "5");

        double flow = algorithm.Run(new DepthFirstSearchStrategy(), specialEdge);

        Assert.Equal(40, flow);
        TestNetworks.AssertValidFlow(graph, capacities, "1", "5", flow);

        // The edge in the middle only has capacity 1, so each of the 40 units needs its own augmentation.
        // Two snapshots are recorded per augmentation, plus the initial and the final state.
        Assert.Equal(1 + 2 * 40 + 1, algorithm.GraphStates.Count);
    }

    [Fact]
    public void WorstCase_UsesTheResidualEdgeOnceTheSpecialEdgeIsSaturated()
    {
        var graph = TestNetworks.Example();
        var specialEdge = Edge(graph, "2", "3");
        var strategy = new DepthFirstSearchStrategy();
        var pathFlow = new Dictionary<FlowEdge, double>();
        var source = graph.GetVertexById("1")!;
        var target = graph.GetVertexById("5")!;

        Assert.True(strategy.FindAugmentingPath(graph, source, target, pathFlow, specialEdge));
        Assert.Contains(specialEdge, pathFlow.Keys);

        specialEdge.CurrentFlow = specialEdge.MaxFlow;
        var residualEdge = new FlowEdge(specialEdge.Target, specialEdge.Source, specialEdge.MaxFlow) { IsBackwards = true };
        graph.AddEdge(residualEdge);

        Assert.True(strategy.FindAugmentingPath(graph, source, target, pathFlow, specialEdge));
        Assert.Contains(residualEdge, pathFlow.Keys);
        Assert.DoesNotContain(specialEdge, pathFlow.Keys);
    }

    [Fact]
    public void WorstCase_FallsBackToAnyPathWhenTheSpecialEdgeCannotBeUsed()
    {
        var graph = TestNetworks.Build(4, (1, 2, 5), (2, 4, 5), (3, 4, 5));
        var unreachableEdge = Edge(graph, "3", "4");

        Assert.Equal(new HashSet<string> { "1-2", "2-4" },
            FindPath(new DepthFirstSearchStrategy(), graph, "1", "4", unreachableEdge));
    }
}
