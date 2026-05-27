using FlowForge.Model;

namespace FlowForge.Tests;

public class FlowNetworkParserTests
{
    [Fact]
    public void ParsesCommaSeparatedEdges()
    {
        var graph = FlowNetworkParser.Parse(new[] { "3", "1,2,10", "2,3,5" });

        Assert.Equal(3, graph.Graph.VertexCount);
        Assert.Equal(10, Capacity(graph, "1", "2"));
        Assert.Equal(5, Capacity(graph, "2", "3"));
    }

    [Fact]
    public void ParsesColonSeparatedEdgesBlankLinesAndWhitespace()
    {
        var graph = FlowNetworkParser.Parse(new[] { "", " 3 ", "1:2:10", "", "  2 : 3 : 5  ", "" });

        Assert.Equal(3, graph.Graph.VertexCount);
        Assert.Equal(2, graph.Graph.EdgeCount);
        Assert.Equal(5, Capacity(graph, "2", "3"));
    }

    [Fact]
    public void ParsesFilesWithWindowsLineEndings()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "2\r\n1,2,7\r\n");

            var graph = FlowNetworkParser.ParseFile(path);

            Assert.Equal(7, Capacity(graph, "1", "2"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ParallelEdgesAreMerged()
    {
        var graph = FlowNetworkParser.Parse(new[] { "2", "1,2,3", "1,2,4" });

        Assert.Equal(7, Capacity(graph, "1", "2"));
        Assert.Equal(1, graph.Graph.EdgeCount);
    }

    [Fact]
    public void AllowsZeroCapacity()
    {
        var graph = FlowNetworkParser.Parse(new[] { "2", "1,2,0" });

        Assert.Equal(0, Capacity(graph, "1", "2"));
    }

    [Theory]
    [InlineData]
    [InlineData("3")]
    [InlineData("", "  ")]
    [InlineData("abc", "1,2,3")]
    [InlineData("1", "1,1,3")]
    [InlineData("0", "1,1,3")]
    [InlineData("3", "1,2")]
    [InlineData("3", "1,2,3,4")]
    [InlineData("3", "1,two,3")]
    [InlineData("3", "1,2,3.5")]
    [InlineData("3", "1,4,3")]
    [InlineData("3", "0,2,3")]
    [InlineData("3", "1,2,-5")]
    public void InvalidInputIsRejected(params string[] lines)
    {
        Assert.Throws<FormatException>(() => FlowNetworkParser.Parse(lines));
    }

    [Fact]
    public void ErrorMessagesNameTheOffendingLine()
    {
        var exception = Assert.Throws<FormatException>(() => FlowNetworkParser.Parse(new[] { "3", "1,2,3", "", "oops" }));

        Assert.Contains("line 4", exception.Message);
    }

    private static double Capacity(FlowGraph graph, string from, string to)
    {
        Assert.True(graph.Graph.TryGetEdge(graph.GetVertexById(from)!, graph.GetVertexById(to)!, out var edge));
        return edge.MaxFlow;
    }
}
