using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Forms.Integration;
using MsaglDrawing = Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace FlowForge
{
    public partial class MainWindow : Window
    {
        private GViewer? _gViewer;
        private FlowGraph? _flowGraph;
        private FordFulkersonAlgorithm? _fordFulkerson;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGraph();
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
            DisplayGraph();
        }

        private void InitializeGraph()
        {
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
            var msaglGraph = GraphVisualizer.ConvertToMsaglGraph(_flowGraph);

            // Farbe für Knoten 1 und 4 festlegen
            var node1 = msaglGraph.FindNode("1");
            if (node1 != null)
            {
                node1.Attr.FillColor = MsaglDrawing.Color.Green; // Knoten 1 wird grün
            }

            var node4 = msaglGraph.FindNode("4");
            if (node4 != null)
            {
                node4.Attr.FillColor = MsaglDrawing.Color.Red; // Knoten 4 wird rot
            }

            foreach (var node in msaglGraph.Nodes)
            {
                node.Attr.Shape = MsaglDrawing.Shape.Circle;     // Knoten als Kreis
            }
            
            foreach (var edge in msaglGraph.Edges)
            {
                if (edge.Label != null)
                {
                    edge.Label.FontSize = 4; // Setze die Schriftgröße des Kantenlabels auf 8
                }
            }
            
            // Assign MSAGL graph to viewer for display
            if (_gViewer != null)
            {
                _gViewer.Graph = msaglGraph;
            }
        }


        // Event handler for "Run Ford-Fulkerson" button
        private void RunAlgorithmButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fordFulkerson == null)
            {
                MessageBox.Show("Please initialize the graph first by clicking the 'Initialize Graph' button!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double maxFlow = _fordFulkerson.Run("1", "4");

            Console.WriteLine($"Maximaler Fluss von Quelle zu Senke: {maxFlow}");
            
            /*
            // Dictionary to store the flow along the augmenting path
            var pathFlow = new Dictionary<FlowEdge, double>();

            // Try to find an augmenting path from source (1) to target (4)
            bool pathFound = _fordFulkerson.FindAugmentingPath(_flowGraph.GetVertexById("1"), _flowGraph.GetVertexById("4"), pathFlow);

            if (pathFound)
            {
                // Find the start and end nodes
                var source = _flowGraph.GetVertexById("1");
                var target = _flowGraph.GetVertexById("4");

                Console.WriteLine("Augmenting path found:");
    
                // To print in correct order, traverse the path from target to source
                var pathInOrder = new List<FlowEdge>();
                var current = target;
    
                // We trace back from the target to the source using the edges stored in pathFlow
                while (current != source)
                {
                    var edge = pathFlow.Keys.FirstOrDefault(e => e.Target == current);
                    if (edge == null) continue;
                    
                    pathInOrder.Insert(0, edge); // Insert the edge at the beginning to reverse the order
                    current = edge.Source; // Move to the previous node
                }

                // Now we print the path from source to target in order
                foreach (var edge in pathInOrder)
                {
                    Console.WriteLine($"{edge.Source.Id} -> {edge.Target.Id}, Remaining Flow: {pathFlow[edge]}");
                }
            }
            else
            {
                Console.WriteLine("No augmenting path found.");
            }
*/

            // Run the Ford-Fulkerson algorithm
            //double maxFlow = _fordFulkerson.Run("1", "4");
            //MessageBox.Show($"Calculated Maximum Flow from Source to Sink: {maxFlow}");

            // Update the visualization (optional)
            DisplayGraph();
        }
    }
}