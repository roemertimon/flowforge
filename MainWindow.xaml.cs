using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms.Integration;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using MsaglDrawing = Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;


namespace FlowForge
{
    public partial class MainWindow : Window
    {
        private GViewer? _gViewer;
        private FlowGraph _flowGraph = new FlowGraph();
        private FordFulkersonAlgorithm? _fordFulkerson;

        private readonly Dictionary<string, Microsoft.Msagl.Core.Geometry.Point> _nodePositions = new Dictionary<string, Microsoft.Msagl.Core.Geometry.Point>();

        private MsaglDrawing.Graph _msaglGraph = new MsaglDrawing.Graph();
        bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();

            SetupGraphViewer();
        }

        private void SetupGraphViewer()
        {
            // GViewer initialization
            _gViewer = new GViewer();

            // WindowsFormsHost creation
            var host = new WindowsFormsHost
            {
                Child = _gViewer // Attach GViewer to host
            };

            // Add the host to the specific Grid designed for graph viewer
            GraphViewerGrid.Children.Add(host);
        }

        // Event handler for "Initialize and Display Graph" button
        private void InitializeGraphButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeGraph();
            DisplayGraph();
        }

        // Event handler for "Run Ford-Fulkerson" button
        private void FordFulkersonButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gViewer != null) _gViewer.NeedToCalculateLayout = true;
            SaveNodePositions(_gViewer?.Graph); // Save positions before running the algorithm
            if (_fordFulkerson == null)
            {
                MessageBox.Show("Please initialize the graph first by clicking the 'Initialize Graph' button!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //SaveNodePositions(_gViewer.Graph); // Save positions before running the algorithm

            double maxFlow = _fordFulkerson.Run(FordFulkersonAlgorithm.SearchStrategy.DepthFirstSearch);

            _isInitialized = true;
            DisplayGraph();
            MessageBox.Show($"Maximum flow from source to sink: {maxFlow}");
        }

        private void EdmondsKarpButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fordFulkerson == null)
            {
                MessageBox.Show("Please initialize the graph first by clicking the 'Initialize Graph' button!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_gViewer != null) _gViewer.NeedToCalculateLayout = true;
            SaveNodePositions(_gViewer?.Graph); // Save positions before running the algorithm
            double maxFlow = _fordFulkerson.Run(FordFulkersonAlgorithm.SearchStrategy.BreadthFirstSearch);

            _isInitialized = true;
            DisplayGraph();
            MessageBox.Show($"Maximum flow from source to sink: {maxFlow}");
        }

        private void InitializeGraph()
        {
            if (_gViewer != null) _gViewer.NeedToCalculateLayout = true;
            _nodePositions.Clear();

            // Initialize a FlowGraph
            _flowGraph = new FlowGraph();

            // Adding vertices to the graph
            _flowGraph.AddVertex("1", "1");
            _flowGraph.AddVertex("2", "2");
            _flowGraph.AddVertex("3", "3");
            _flowGraph.AddVertex("4", "4");
            _flowGraph.AddVertex("5", "5");

            // Adding edges with flow capacities
            _flowGraph.AddEdge("1", "2", 20);
            _flowGraph.AddEdge("1", "3", 20);
            _flowGraph.AddEdge("2", "3", 1);
            _flowGraph.AddEdge("2", "4", 20);
            _flowGraph.AddEdge("3", "4", 20);
            _flowGraph.AddEdge("4", "5", 40);


            // Initialize Ford-Fulkerson algorithm with the flow graph
            _fordFulkerson = new FordFulkersonAlgorithm(_flowGraph, "1", "5");
        }

        private void DisplayGraph()
        {
            // Convert QuickGraph to MSAGL Graph
            _msaglGraph = GraphVisualizer.ConvertToMsaglGraph(_flowGraph);
            _msaglGraph.Attr.LayerDirection = MsaglDrawing.LayerDirection.TB;

            // Farbe für Knoten 1 und 4 festlegen
            var node1 = _msaglGraph.FindNode("1");
            if (node1 != null)
            {
                node1.Attr.FillColor = MsaglDrawing.Color.Green; // Knoten 1 wird grün
            }

            var node4 = _msaglGraph.FindNode("5");
            if (node4 != null)
            {
                node4.Attr.FillColor = MsaglDrawing.Color.Red; // Knoten 4 wird rot
            }

            foreach (var node in _msaglGraph.Nodes)
            {
                node.Attr.Shape = MsaglDrawing.Shape.Circle; // Knoten als Kreis
            }

            foreach (var edge in _msaglGraph.Edges)
            {
                if (edge.Label != null)
                {
                    edge.Label.FontSize = 4; // Setze die Schriftgröße des Kantenlabels auf 4
                }
            }

            // Assign MSAGL graph to viewer for display
            if (_gViewer != null)
            {
                _gViewer.Graph = _msaglGraph;

                if (_nodePositions.Any())
                {
                    ApplyNodePositions(_msaglGraph);

                    var sugiyamaSettings = new SugiyamaLayoutSettings();
                    sugiyamaSettings.NodeSeparation = 10;
                    //sugiyamaSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.RectilinearToCenter;
                    sugiyamaSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;
                    sugiyamaSettings.EdgeRoutingSettings.Padding = 1;
                    sugiyamaSettings.RepetitionCoefficientForOrdering = 100;
                    sugiyamaSettings.EdgeRoutingSettings.KeepOriginalSpline = false;

                    Microsoft.Msagl.Miscellaneous.LayoutHelpers.RouteAndLabelEdges(_msaglGraph.GeometryGraph,
                        sugiyamaSettings, _msaglGraph.GeometryGraph.Edges, 100, new CancelToken());

                    /*
                    // Step 3: Apply layout settings to the graph
                    _msaglGraph.LayoutAlgorithmSettings = sugiyamaSettings;

                    _msaglGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.RouteMultiEdgesAsBundles = false;

                    var router = new SplineRouter(
                        _msaglGraph.GeometryGraph,
                        _msaglGraph.LayoutAlgorithmSettings.EdgeRoutingSettings
                    );
                    router.Run();

                    foreach (var edge in _msaglGraph.Edges)
                    {
                        if (edge.GeometryEdge != null && edge.Label != null)
                        {
                            // Set the label to the midpoint of the edge path
                            edge.Label.GeometryLabel.Center = edge.GeometryEdge.Curve.BoundingBox.Center;
                        }
                    }*/

                    //SetStraightLineEdges(_msaglGraph);

                    //_gViewer.Invalidate();
                    _gViewer.NeedToCalculateLayout = false;
                    _gViewer.Graph = _msaglGraph;
                }
            }
        }

        private void SaveNodePositions(MsaglDrawing.Graph? msaglGraph)
        {
            _nodePositions.Clear();
            if (msaglGraph == null) return;
            foreach (var node in msaglGraph.Nodes)
            {
                var position = node.GeometryNode.Center;
                _nodePositions[node.Id] = new Microsoft.Msagl.Core.Geometry.Point(position.X, position.Y);
            }
        }

        private void ApplyNodePositions(MsaglDrawing.Graph msaglGraph)
        {
            foreach (var node in msaglGraph.Nodes)
            {
                if (_nodePositions.TryGetValue(node.Id, out var position))
                {
                    node.GeometryNode.Center = new Microsoft.Msagl.Core.Geometry.Point(position.X, position.Y);
                }
            }
        }

        private void SetStraightLineEdges(MsaglDrawing.Graph msaglGraph)
        {
            var geometryGraph = msaglGraph.GeometryGraph;

            foreach (var edge in geometryGraph.Edges)
            {
                var sourceCenter = edge.Source.Center;
                var targetCenter = edge.Target.Center;

                // Create a straight line from source to target
                edge.Curve = new LineSegment(sourceCenter, targetCenter);
                edge.LineWidth = 2;
            }

            foreach (var edge in msaglGraph.Edges)
            {
                edge.Attr.ArrowheadAtTarget = MsaglDrawing.ArrowStyle.Normal; // Ensure arrowhead is displayed
                edge.Attr.ArrowheadLength = 1;
            }
        }

        private void ExportToLatexButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Please run an algorithm first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_fordFulkerson == null) return;
            TikzCodeGenerator tikzCodeGenerator = new TikzCodeGenerator(_flowGraph, _nodePositions, _fordFulkerson);
            tikzCodeGenerator.ExportGraphToLatex(IncludeAnimationCheckbox.IsChecked, StandaloneCheckbox.IsChecked);
        }
    }
}