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
        private bool _isInitialized = false;

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
            // Show a file picker dialog
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Select a Graph Definition File"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true && !string.IsNullOrEmpty(openFileDialog.FileName))
            {
                // Initialize graph from the selected file
                InitializeGraphFromFile(openFileDialog.FileName);
            }
            else
            {
                // If no file is selected, initialize the default graph
                InitializeGraphExample();
            }

            // Display the graph after initialization
            DisplayGraph();
        }
        
        private void InitializeGraphFromFile(string filePath)
        {
            try
            {
                // Clear existing graph data
                if (_gViewer != null) _gViewer.NeedToCalculateLayout = true;
                _nodePositions.Clear();
                _flowGraph = new FlowGraph();

                // Read all lines from the file
                var lines = System.IO.File.ReadAllLines(filePath);

                if (lines.Length < 2)
                {
                    throw new FormatException("The file must contain at least two lines: number of vertices and at least one edge.");
                }

                // Parse the number of vertices from the first line
                if (!int.TryParse(lines[0], out int numVertices) || numVertices <= 1)
                {
                    throw new FormatException("The first line must specify an integer greater than 1 for the number of vertices.");
                }

                // Add vertices (labeled from 1 to n)
                for (int i = 1; i <= numVertices; i++)
                {
                    _flowGraph.AddVertex(i.ToString(), i.ToString());
                }

                // Parse the edges
                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(new[] { ',', ':' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 3 || 
                        !int.TryParse(parts[0], out int from) || 
                        !int.TryParse(parts[1], out int to) || 
                        !int.TryParse(parts[2], out int capacity))
                    {
                        throw new FormatException($"Invalid edge format on line {i + 1}: {lines[i]}");
                    }

                    if (from < 1 || from > numVertices || to < 1 || to > numVertices)
                    {
                        throw new FormatException($"Edge references invalid vertices on line {i + 1}: {lines[i]}");
                    }

                    _flowGraph.AddEdge(from.ToString(), to.ToString(), capacity);
                }

                // Initialize the Ford-Fulkerson algorithm
                _fordFulkerson = new FordFulkersonAlgorithm(_flowGraph, "1", numVertices.ToString());

                MessageBox.Show("Graph successfully initialized from file.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize graph from file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeGraphExample(); // Fallback to default graph if file processing fails
            }
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

        private void InitializeGraphExample()
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

            // Dynamically set the first node (source) to green and the last node (sink) to red
            if (_flowGraph.Graph.Vertices.Any())
            {
                var firstNode = _flowGraph.Graph.Vertices.FirstOrDefault();
                var lastNode = _flowGraph.Graph.Vertices.LastOrDefault();

                if (firstNode != null)
                {
                    var msaglFirstNode = _msaglGraph.FindNode(firstNode.Id);
                    if (msaglFirstNode != null)
                    {
                        msaglFirstNode.Attr.FillColor = MsaglDrawing.Color.Green; // Source node (green)
                    }
                }

                if (lastNode != null)
                {
                    var msaglLastNode = _msaglGraph.FindNode(lastNode.Id);
                    if (msaglLastNode != null)
                    {
                        msaglLastNode.Attr.FillColor = MsaglDrawing.Color.Red; // Sink node (red)
                    }
                }
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

        private void SnapToGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_msaglGraph.Nodes.Any())
            {
                MessageBox.Show("The graph is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Define grid spacing
            const int gridX = 50; // Spacing in the X direction
            const int gridY = 50; // Spacing in the Y direction

            // Iterate through each node and snap it to the nearest grid point
            foreach (var node in _msaglGraph.Nodes)
            {
                var position = node.GeometryNode.Center;

                // Calculate the snapped position
                double snappedX = Math.Round(position.X / gridX) * gridX;
                double snappedY = Math.Round(position.Y / gridY) * gridY;

                // Update the node's position
                node.GeometryNode.Center = new Microsoft.Msagl.Core.Geometry.Point(snappedX, snappedY);
                
                // Reset node attributes to clear selection appearance
                node.Attr.Color = MsaglDrawing.Color.Black; // Default color
                node.Attr.LineWidth = 1; // Default width
            }

            // Update edges to reflect new positions
            if (_gViewer != null)
            {
                _gViewer.Graph = _msaglGraph;

                var sugiyamaSettings = new SugiyamaLayoutSettings();
                sugiyamaSettings.NodeSeparation = 10;
                //sugiyamaSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.RectilinearToCenter;
                sugiyamaSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;
                sugiyamaSettings.EdgeRoutingSettings.Padding = 1;
                sugiyamaSettings.RepetitionCoefficientForOrdering = 100;
                sugiyamaSettings.EdgeRoutingSettings.KeepOriginalSpline = false;

                Microsoft.Msagl.Miscellaneous.LayoutHelpers.RouteAndLabelEdges(_msaglGraph.GeometryGraph,
                    sugiyamaSettings, _msaglGraph.GeometryGraph.Edges, 100, new CancelToken());

                _gViewer.NeedToCalculateLayout = false;
                _gViewer.Graph = _msaglGraph;
            }
        }
    }
}