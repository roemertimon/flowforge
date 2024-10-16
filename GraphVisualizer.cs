using Microsoft.Msagl.Drawing;

namespace FlowForge;

public static class GraphVisualizer
{
    public static Graph ConvertToMsaglGraph(FlowGraph? flowGraph)
    {
        Graph msaglGraph = new Graph();

        foreach (var vertex in flowGraph.Graph.Vertices)
        {
            var msaglNode = msaglGraph.AddNode(vertex.Id);
            msaglNode.LabelText = vertex.Label; // Use the label from FlowNode
        }

        foreach (var edge in flowGraph.Graph.Edges)
        {
            var msaglEdge = msaglGraph.AddEdge(edge.Source.Id, edge.Target.Id);

            if (edge.IsBackwards)
            {
                msaglEdge.LabelText = $"{0}/{edge.Residual}";
            }
            else
            {
                msaglEdge.LabelText = $"{edge.MaxFlow-edge.Residual}/{edge.MaxFlow}";
            }
        }

        return msaglGraph;
    }
}
