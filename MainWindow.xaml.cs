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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GViewer? _gViewer;

        public MainWindow()
        {
            InitializeComponent();
            SetupGraphViewer();
            DisplayGraph();
        }

        private void SetupGraphViewer()
        {
            // GViewer initialisieren
            _gViewer = new GViewer();

            // WindowsFormsHost erstellen
            var host = new WindowsFormsHost();

            // GViewer dem Host hinzufügen
            host.Child = _gViewer;

            // Host dem Grid hinzufügen
            MainGrid.Children.Add(host);
        }

        private void DisplayGraph()
        {
            // Graph erstellen
            MsaglDrawing.Graph graph = new MsaglDrawing.Graph();

            // Kanten hinzufügen und Beschriftungen setzen
            var edge1 = graph.AddEdge("1", "2");
            edge1.LabelText = "10/10";

            var edge2 = graph.AddEdge("2", "3");
            edge2.LabelText = "5/10";

            var edge3 = graph.AddEdge("3", "4");
            edge3.LabelText = "5/20";

            var edge4 = graph.AddEdge("2", "4");
            edge4.LabelText = "5/20";

            // Knoten Attribute setzen
            MsaglDrawing.Node node1 = graph.FindNode("1");
            if (node1 != null)
            {
                node1.Attr.FillColor = MsaglDrawing.Color.Green; // Node 1 grün färben
            }

            MsaglDrawing.Node node4 = graph.FindNode("4");
            if (node4 != null)
            {
                node4.Attr.FillColor = MsaglDrawing.Color.Red; // Node 4 rot färben
            }

            // Graph dem GViewer zuweisen
            if (_gViewer != null)
            {
                _gViewer.Graph = graph;
            }
        }
    }
}
