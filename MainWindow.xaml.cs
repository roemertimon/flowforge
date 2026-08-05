using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms.Integration;
using FlowForge.Algorithms;
using FlowForge.Export;
using FlowForge.Model;
using FlowForge.Visualization;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using MsaglDrawing = Microsoft.Msagl.Drawing;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;

namespace FlowForge;

public partial class MainWindow : Window
{
    private const double GridSpacing = 50;

    private readonly GViewer _viewer = new();
    private readonly Dictionary<string, MsaglPoint> _nodePositions = new();

    private FlowGraph _flowGraph = new();
    private FordFulkersonAlgorithm? _algorithm;
    private MsaglDrawing.Graph _msaglGraph = new();
    private bool _hasResult;

    public MainWindow()
    {
        InitializeComponent();

        GraphViewerGrid.Children.Add(new WindowsFormsHost { Child = _viewer });
        UpdateControlStates();
    }

    private void OpenGraphButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Open graph definition file"
        };

        if (dialog.ShowDialog(this) != true) return;

        FlowGraph graph;
        try
        {
            graph = FlowNetworkParser.ParseFile(dialog.FileName);
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Failed to load the graph:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // By convention the source is the first vertex and the sink is the last one.
        LoadGraph(graph, "1", graph.Graph.VertexCount.ToString(), $"Loaded {Path.GetFileName(dialog.FileName)}");
    }

    private void LoadExampleButton_Click(object sender, RoutedEventArgs e)
    {
        LoadGraph(ExampleNetwork.Create(), ExampleNetwork.SourceId, ExampleNetwork.SinkId, "Loaded the example network");
    }

    private void LoadGraph(FlowGraph graph, string sourceId, string sinkId, string origin)
    {
        _flowGraph = graph;
        _algorithm = new FordFulkersonAlgorithm(graph, sourceId, sinkId);
        _nodePositions.Clear();
        _hasResult = false;

        EmptyStatePanel.Visibility = Visibility.Collapsed;
        GraphViewerGrid.Visibility = Visibility.Visible;

        DisplayGraph();
        SnapNodesToGrid();

        SetStatus($"{origin}: {graph.Graph.VertexCount} vertices, {graph.Graph.EdgeCount} edges, source {sourceId}, sink {sinkId}.");
        UpdateControlStates();
    }

    private void FordFulkersonButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSpecialEdge(out var specialEdge)) return;

        RunAlgorithm("Ford-Fulkerson (DFS)", new DepthFirstSearchStrategy(), specialEdge);
    }

    private void EdmondsKarpButton_Click(object sender, RoutedEventArgs e)
    {
        RunAlgorithm("Edmonds–Karp (BFS)", new BreadthFirstSearchStrategy());
    }

    private void RunAlgorithm(string name, ISearchStrategy strategy, FlowEdge? specialEdge = null)
    {
        if (_algorithm == null) return;

        // Remember where the user has dragged the vertices so that the new drawing keeps the layout.
        SaveNodePositions();

        double maxFlow = _algorithm.Run(strategy, specialEdge);
        _hasResult = true;

        DisplayGraph();
        UpdateControlStates();

        int augmentations = _algorithm.AugmentationCount;
        SetStatus($"{name}: maximum flow {maxFlow}, found with {augmentations} augmenting path{(augmentations == 1 ? "" : "s")}.");
    }

    /// <summary>
    /// Reads the special edge for the worst-case demonstration. Succeeds with <c>null</c> if the option is switched off
    /// and fails (after telling the user) if it is switched on but the input does not describe an existing edge.
    /// </summary>
    private bool TryGetSpecialEdge(out FlowEdge? specialEdge)
    {
        specialEdge = null;
        if (WorstCaseCheckBox.IsChecked != true) return true;

        string sourceId = SpecialEdgeSourceInput.Text.Trim();
        string targetId = SpecialEdgeTargetInput.Text.Trim();
        var source = _flowGraph.GetVertexById(sourceId);
        var target = _flowGraph.GetVertexById(targetId);

        if (source != null && target != null && _flowGraph.Graph.TryGetEdge(source, target, out var edge))
        {
            specialEdge = edge;
            return true;
        }

        MessageBox.Show(this,
            $"The graph has no edge from '{sourceId}' to '{targetId}'.\n\nEnter the vertex ids of an existing edge or untick \"Force worst case\".",
            "Invalid special edge", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void DisplayGraph()
    {
        _msaglGraph = GraphVisualizer.ConvertToMsaglGraph(_flowGraph, _algorithm?.SourceId, _algorithm?.TargetId);

        // Let MSAGL lay the graph out; if positions are known, move the vertices back and only re-route the edges.
        _viewer.NeedToCalculateLayout = true;
        _viewer.Graph = _msaglGraph;

        if (_nodePositions.Count == 0) return;

        foreach (var node in _msaglGraph.Nodes)
        {
            if (_nodePositions.TryGetValue(node.Id, out var position))
            {
                node.GeometryNode.Center = position;
            }
        }

        RouteEdges();
    }

    /// <summary>Re-routes all edges around the current vertex positions and refreshes the viewer without a new layout.</summary>
    private void RouteEdges()
    {
        var settings = new SugiyamaLayoutSettings
        {
            NodeSeparation = 10,
            RepetitionCoefficientForOrdering = 100
        };
        settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;
        settings.EdgeRoutingSettings.Padding = 1;
        settings.EdgeRoutingSettings.KeepOriginalSpline = false;

        LayoutHelpers.RouteAndLabelEdges(_msaglGraph.GeometryGraph, settings, _msaglGraph.GeometryGraph.Edges, 100, new CancelToken());

        _viewer.NeedToCalculateLayout = false;
        _viewer.Graph = _msaglGraph;
    }

    private void SaveNodePositions()
    {
        _nodePositions.Clear();
        foreach (var node in _msaglGraph.Nodes)
        {
            _nodePositions[node.Id] = node.GeometryNode.Center;
        }
    }

    private void SnapToGridButton_Click(object sender, RoutedEventArgs e) => SnapNodesToGrid();

    private void SnapNodesToGrid()
    {
        foreach (var node in _msaglGraph.Nodes)
        {
            var center = node.GeometryNode.Center;
            node.GeometryNode.Center = new MsaglPoint(
                Math.Round(center.X / GridSpacing) * GridSpacing,
                Math.Round(center.Y / GridSpacing) * GridSpacing);

            // Clear the selection appearance of vertices that were clicked in the viewer.
            node.Attr.Color = MsaglDrawing.Color.Black;
            node.Attr.LineWidth = 1;
        }

        RouteEdges();
    }

    private void ExportToLatexButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasResult || _algorithm == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "TeX files (*.tex)|*.tex",
            Title = "Export graph to LaTeX",
            DefaultExt = ".tex",
            FileName = "flowforge.tex"
        };

        if (dialog.ShowDialog(this) != true) return;

        bool includeAnimation = IncludeAnimationCheckBox.IsChecked == true;
        bool standalone = StandaloneCheckBox.IsChecked == true;

        try
        {
            // Vertices may have been dragged since the last run; export what is on screen.
            SaveNodePositions();

            var generator = new TikzCodeGenerator(_flowGraph, _nodePositions, _algorithm);
            File.WriteAllText(dialog.FileName, generator.GenerateTikzCode(includeAnimation, standalone));

            string document = standalone ? "standalone document" : "TikZ code only";
            string animation = includeAnimation ? "with animation" : "without animation";
            SetStatus($"Exported {Path.GetFileName(dialog.FileName)} ({document}, {animation}).");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(this, $"Error exporting graph:\n{ex.Message}", "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateControlStates()
    {
        bool hasGraph = _algorithm != null;

        SnapToGridButton.IsEnabled = hasGraph;
        FordFulkersonButton.IsEnabled = hasGraph;
        EdmondsKarpButton.IsEnabled = hasGraph;
        ExportButton.IsEnabled = _hasResult;
    }

    private void SetStatus(string message) => StatusText.Text = message;
}
