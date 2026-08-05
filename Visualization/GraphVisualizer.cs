using FlowForge.Model;
using Microsoft.Msagl.Drawing;

namespace FlowForge.Visualization;

public static class GraphVisualizer
{
    private static readonly Color SourceFill = new(129, 199, 132);
    private static readonly Color SinkFill = new(229, 115, 115);

    /// <summary>
    /// Converts a flow network into an MSAGL graph. Edges are labelled with their flow, edges that carry flow are drawn
    /// in orange and residual edges in grey. The source and sink are filled with the colours shown in the legend.
    /// </summary>
    public static Graph ConvertToMsaglGraph(FlowGraph flowGraph, string? sourceId = null, string? sinkId = null)
    {
        Graph msaglGraph = new Graph();
        msaglGraph.Attr.LayerDirection = LayerDirection.TB;

        foreach (var vertex in flowGraph.Graph.Vertices)
        {
            var msaglNode = msaglGraph.AddNode(vertex.Id);
            msaglNode.LabelText = vertex.Label;
            msaglNode.Attr.Shape = Shape.Circle;

            if (vertex.Id == sourceId)
            {
                msaglNode.Attr.FillColor = SourceFill;
            }
            else if (vertex.Id == sinkId)
            {
                msaglNode.Attr.FillColor = SinkFill;
            }
        }

        foreach (var edge in flowGraph.Graph.Edges)
        {
            var msaglEdge = msaglGraph.AddEdge(edge.Source.Id, edge.Target.Id);
            msaglEdge.LabelText = edge.FlowLabel;
            msaglEdge.Label.FontSize = 8;

            if (edge.IsBackwards)
            {
                msaglEdge.Attr.Color = Color.Gray;
            }
            else if (edge.CurrentFlow > 0)
            {
                msaglEdge.Attr.Color = Color.Orange;
            }
        }

        return msaglGraph;
    }
}
