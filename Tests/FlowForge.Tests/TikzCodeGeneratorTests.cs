using System.Globalization;
using FlowForge.Algorithms;
using FlowForge.Export;
using FlowForge.Model;
using Microsoft.Msagl.Core.Geometry;

namespace FlowForge.Tests;

public class TikzCodeGeneratorTests
{
    private static readonly Dictionary<string, Point> ExamplePositions = new()
    {
        ["1"] = new Point(0, 300), ["2"] = new Point(-100, 200), ["3"] = new Point(100, 200),
        ["4"] = new Point(0, 100), ["5"] = new Point(0, 0),
    };

    private static (TikzCodeGenerator Generator, FordFulkersonAlgorithm Algorithm) CreateExample(bool run = true)
    {
        var graph = TestNetworks.Example();
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "5");
        if (run)
        {
            algorithm.Run(new BreadthFirstSearchStrategy());
        }

        return (new TikzCodeGenerator(graph, ExamplePositions, algorithm), algorithm);
    }

    [Fact]
    public void StandaloneDocument_IsAWholeBeamerDocument()
    {
        var code = CreateExample().Generator.GenerateTikzCode(includeAnimations: false, isStandalone: true);

        Assert.StartsWith("\\documentclass{beamer}", code);
        Assert.Contains("\\begin{document}", code);
        Assert.Contains("\\begin{frame}", code);
        Assert.EndsWith("\\end{document}" + Environment.NewLine, code);
    }

    [Fact]
    public void NonStandalone_ContainsOnlyTheTikzPicture()
    {
        var code = CreateExample().Generator.GenerateTikzCode(includeAnimations: false, isStandalone: false);

        Assert.StartsWith("\\begin{tikzpicture}", code);
        Assert.EndsWith("\\end{tikzpicture}", code.TrimEnd());
        Assert.DoesNotContain("\\documentclass", code);
        Assert.DoesNotContain("\\begin{document}", code);
    }

    [Fact]
    public void Nodes_UseTheViewerPositionsAndHighlightSourceAndSink()
    {
        var code = CreateExample().Generator.GenerateTikzCode(false, false);

        Assert.Contains("\\node[main node, circle,draw,fill=sourceColor] (1) at (0.00,3.00) {1};", code);
        Assert.Contains("\\node[main node, circle,draw] (2) at (-1.00,2.00) {2};", code);
        Assert.Contains("\\node[main node, circle,draw,fill=targetColor] (5) at (0.00,0.00) {5};", code);
    }

    [Fact]
    public void Edges_ShowFlowAndCapacity()
    {
        var code = CreateExample().Generator.GenerateTikzCode(false, false);

        Assert.Contains("(4) edge [pos=0.5,sloped] node {\\texttt{40/40}} (5)", code);
        Assert.DoesNotContain("dashed", code);
        Assert.Equal(6, code.Split("\\path[").Length - 1);
    }

    [Fact]
    public void Numbers_UseInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var code = CreateExample().Generator.GenerateTikzCode(false, false);

            Assert.Contains("at (-1.00,2.00)", code);
            Assert.DoesNotContain("-1,00", code);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Animation_HasOneOverlayPerRecordedStep()
    {
        var (generator, algorithm) = CreateExample();

        var code = generator.GenerateTikzCode(includeAnimations: true, isStandalone: true);

        int frames = code.Split("\\visible<").Length - 1;
        Assert.Equal(algorithm.GraphStates.Count, frames);
        Assert.Contains("\\visible<1>{", code);
        Assert.Contains($"\\visible<{algorithm.GraphStates.Count}>{{", code);
        Assert.Contains("\\begin{frame}{Graph Animation}", code);
    }

    [Fact]
    public void Animation_ShowsResidualEdgesAndHighlightsTheCurrentPath()
    {
        var graph = TestNetworks.Build(4, (1, 2, 1), (1, 3, 1), (2, 3, 1), (2, 4, 1), (3, 4, 1));
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "4");
        algorithm.Run(new DepthFirstSearchStrategy());
        var positions = new Dictionary<string, Point>
        {
            ["1"] = new Point(0, 200), ["2"] = new Point(-100, 100), ["3"] = new Point(100, 100), ["4"] = new Point(0, 0),
        };

        var code = new TikzCodeGenerator(graph, positions, algorithm).GenerateTikzCode(true, false);

        Assert.Contains("bend right,dashed,black", code);
        Assert.Contains(",red]", code);
    }

    [Fact]
    public void AnimationWithoutARun_Throws()
    {
        var (generator, _) = CreateExample(run: false);

        Assert.Throws<InvalidOperationException>(() => generator.GenerateTikzCode(true, true));
    }

    [Fact]
    public void OppositeInputEdges_AreBentSoThatTheyDoNotOverlap()
    {
        var graph = TestNetworks.Build(3, (1, 2, 5), (2, 1, 3), (2, 3, 5));
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "3");
        algorithm.Run(new BreadthFirstSearchStrategy());
        var positions = new Dictionary<string, Point> { ["1"] = new Point(0, 0), ["2"] = new Point(100, 0), ["3"] = new Point(200, 0) };

        var code = new TikzCodeGenerator(graph, positions, algorithm).GenerateTikzCode(false, false);

        Assert.Contains("bend right] (1) edge", code);
        Assert.Contains("bend right] (2) edge [pos=0.5,sloped] node {\\texttt{0/3}} (1)", code);
        Assert.DoesNotContain("bend right] (2) edge [pos=0.5,sloped] node {\\texttt{5/5}} (3)", code);
    }

    [Fact]
    public void VerticesWithoutAPosition_AreStillEmitted()
    {
        var graph = TestNetworks.Build(2, (1, 2, 5));
        var algorithm = new FordFulkersonAlgorithm(graph, "1", "2");
        algorithm.Run(new BreadthFirstSearchStrategy());

        var code = new TikzCodeGenerator(graph, new Dictionary<string, Point>(), algorithm).GenerateTikzCode(false, false);

        Assert.Contains("(1) at (0.00,0.00)", code);
        Assert.Contains("(2) at (1.00,0.00)", code);
    }
}
