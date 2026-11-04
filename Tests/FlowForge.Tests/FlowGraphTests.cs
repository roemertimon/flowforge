using FlowForge.Algorithms;
using FlowForge.Model;

namespace FlowForge.Tests;

public class FlowGraphTests
{
    [Fact]
    public void ParallelEdges_AreMergedIntoOneEdgeWithSummedCapacity()
    {
        var graph = TestNetworks.Build(3, (1, 2, 4), (1, 2, 3), (2, 3, 10));

        var edge = Assert.Single(graph.GetEdges(), e => e.Source.Id == "1" && e.Target.Id == "2");
        Assert.Equal(7, edge.MaxFlow);
        Assert.Equal(2, graph.GetEdges().Count());
    }

    [Theory]
    [InlineData("dfs")]
    [InlineData("bfs")]
    public void ParallelEdges_CountTowardsTheMaximumFlow(string strategy)
    {
        var graph = TestNetworks.Build(3, (1, 2, 4), (1, 2, 3), (2, 3, 10));
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "3");

        double flow = algorithm.Run(strategy == "dfs" ? new DepthFirstSearchStrategy() : new BreadthFirstSearchStrategy());

        Assert.Equal(7, flow);
    }

    [Fact]
    public void AddingAnExistingVertex_DoesNotCreateADuplicate()
    {
        var graph = new FlowGraph();
        graph.AddVertex("1");
        graph.AddVertex("1");

        Assert.Single(graph.Graph.Vertices);
    }

    [Fact]
    public void AddingAnEdgeBetweenUnknownVertices_IsIgnored()
    {
        var graph = new FlowGraph();
        graph.AddVertex("1");
        graph.AddEdge("1", "2", 5);

        Assert.Empty(graph.GetEdges());
    }

    [Fact]
    public void Clone_IsIndependentOfTheOriginal()
    {
        var graph = TestNetworks.Build(2, (1, 2, 5));
        var clone = graph.Clone();

        graph.GetEdges().Single().CurrentFlow = 3;

        Assert.Equal(0, clone.GetEdges().Single().CurrentFlow);
        Assert.Equal(5, clone.GetEdges().Single().MaxFlow);
        Assert.NotSame(graph.GetVertexById("1"), clone.GetVertexById("1"));
    }
}
