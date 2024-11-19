
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
        private FlowGraph? _flowGraph;
        private FordFulkersonAlgorithm? _fordFulkerson;
        
        private Dictionary<string, Microsoft.Msagl.Core.Geometry.Point> _nodePositions = new Dictionary<string, Microsoft.Msagl.Core.Geometry.Point>();
        private MsaglDrawing.Graph _msaglGraph;

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
            /*
            // Move node "1" to position (100, 200)
            _msaglGraph.FindNode("1").GeometryNode.Center = new Microsoft.Msagl.Core.Geometry.Point(20,700);
            SetStraightLineEdges(_msaglGraph);
            
            _gViewer.NeedToCalculateLayout = false;
            _gViewer.Graph = _msaglGraph;*/
            
            if (_gViewer != null) _gViewer.NeedToCalculateLayout = true;
            SaveNodePositions(_gViewer?.Graph); // Save positions before running the algorithm
            if (_fordFulkerson == null)
            {
                MessageBox.Show("Please initialize the graph first by clicking the 'Initialize Graph' button!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            //SaveNodePositions(_gViewer.Graph); // Save positions before running the algorithm
            
            double maxFlow = _fordFulkerson.Run("1", "4", FordFulkersonAlgorithm.SearchStrategy.DepthFirstSearch);
            
          
            DisplayGraph();
            MessageBox.Show($"Maximum flow from source to sink: {maxFlow}");
        }
        
        private void EdmondsKarpButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fordFulkerson == null)
            {
                MessageBox.Show("Please initialize the graph first by clicking the 'Initialize Graph' button!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_gViewer != null) _gViewer.NeedToCalculateLayout = true;
            SaveNodePositions(_gViewer?.Graph); // Save positions before running the algorithm
            double maxFlow = _fordFulkerson.Run("1", "4", FordFulkersonAlgorithm.SearchStrategy.BreadthFirstSearch);

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
            _flowGraph.AddEdge("1", "2", 10);
            _flowGraph.AddEdge("2", "3", 5);
            _flowGraph.AddEdge("3", "4", 15);
            _flowGraph.AddEdge("2", "4", 5);
            _flowGraph.AddEdge("2", "5", 100);
            _flowGraph.AddEdge("5", "4", 2);
            _flowGraph.AddEdge("1", "5", 200);

            // Initialize Ford-Fulkerson algorithm with the flow graph
            _fordFulkerson = new FordFulkersonAlgorithm(_flowGraph);
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

            var node4 = _msaglGraph.FindNode("4");
            if (node4 != null)
            {
                node4.Attr.FillColor = MsaglDrawing.Color.Red; // Knoten 4 wird rot
            }

            foreach (var node in _msaglGraph.Nodes)
            {
                node.Attr.Shape = MsaglDrawing.Shape.Circle;     // Knoten als Kreis
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
                
                // Set the graph and check which layout settings are applied by default
                _gViewer.Graph = _msaglGraph;

            // Print layout information to debug output
                if (_msaglGraph.LayoutAlgorithmSettings is SugiyamaLayoutSettings)
                {
                    Console.WriteLine("Using Sugiyama Layout");
                }
                else if (_msaglGraph.LayoutAlgorithmSettings is MdsLayoutSettings)
                {
                    Console.WriteLine("Using MDS Layout");
                }
                else
                {
                    Console.WriteLine("Using another layout algorithm");
                }

                
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
                    
                    Microsoft.Msagl.Miscellaneous.LayoutHelpers.RouteAndLabelEdges(_msaglGraph.GeometryGraph, sugiyamaSettings, _msaglGraph.GeometryGraph.Edges, 100, new CancelToken());
                    
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
        
        private string GenerateTikzCode()
        {
            if (_flowGraph == null)
                throw new InvalidOperationException("Flow graph is not initialized.");

            var tikzBuilder = new System.Text.StringBuilder();
            tikzBuilder.AppendLine("\\documentclass[tikz,border=3mm]{standalone}");
            tikzBuilder.AppendLine("\\usetikzlibrary{arrows.meta,positioning}");
            tikzBuilder.AppendLine("\\usepackage{amsmath}");
            tikzBuilder.AppendLine("\\begin{document}");
    
            // Global scaling and styles for smaller nodes and text
            tikzBuilder.AppendLine("\\begin{tikzpicture}[scale=3,->,>=stealth,shorten >=1pt,auto,node distance=2cm,thick,");
            tikzBuilder.AppendLine("main node/.style={circle,draw,minimum size=1mm,inner sep=1pt,font=\\scriptsize}]");

            // Nodes
            foreach (var vertex in _flowGraph.Graph.Vertices)
            {
                if (_nodePositions.ContainsKey(vertex.Id))
                {
                    var position = _nodePositions[vertex.Id];
                    // Format x and y as floating-point values with two decimal places
                    string formattedX = (position.X / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    string formattedY = (position.Y / 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

                    tikzBuilder.AppendLine($"\\node[main node] ({vertex.Id}) at ({formattedX},{formattedY}) {{{vertex.Id}}};");
                }
            }

            // Edges
            foreach (var edge in _flowGraph.GetEdges())
            {
                string label = $"{edge.Residual}/{edge.MaxFlow}";
                tikzBuilder.AppendLine($"\\path[every node/.style={{font=\\tiny}}] ({edge.Source.Id}) edge node {{\\texttt{{{label}}}}} ({edge.Target.Id});");
            }

            tikzBuilder.AppendLine("\\end{tikzpicture}");
            tikzBuilder.AppendLine("\\end{document}");

            return tikzBuilder.ToString();
        }



        
        private void ExportGraphToLatex(string filePath)
        {
            try
            {
                string tikzCode = GenerateTikzCode();
                System.IO.File.WriteAllText(filePath, tikzCode);
                MessageBox.Show($"Graph successfully exported to LaTeX file:\n{filePath}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting graph: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ExportToLatexButton_Click(object sender, RoutedEventArgs e)
        {
            if (_flowGraph == null)
            {
                MessageBox.Show("Please run an algorithm first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "TeX files (*.tex)|*.tex",
                Title = "Export Graph to LaTeX"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                ExportGraphToLatex(saveFileDialog.FileName);
            }
        }
    }
}