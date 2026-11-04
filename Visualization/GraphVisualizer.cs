using FlowForge.Model;
using Microsoft.Msagl.Drawing;

namespace FlowForge.Visualization;

public static class GraphVisualizer
{
    public static Graph ConvertToMsaglGraph(FlowGraph flowGraph)
    {
        Graph msaglGraph = new Graph();

        foreach (var vertex in flowGraph.Graph.Vertices)
        {
            var msaglNode = msaglGraph.AddNode(vertex.Id);
            msaglNode.LabelText = vertex.Label; // Use the label from FlowNode
        }

        foreach (var edge in flowGraph.Graph.Edges)
        {
            //if(edge.IsBackwards) continue;
            
            var msaglEdge = msaglGraph.AddEdge(edge.Source.Id, edge.Target.Id);

            if (edge.IsBackwards)
            {
                msaglEdge.LabelText = $"{edge.CurrentFlow}/{edge.MaxFlow}";
                msaglEdge.Attr.Color = Color.Gray;
            }
            else
            {
                msaglEdge.LabelText = $"{edge.CurrentFlow}/{edge.MaxFlow}";

                if (edge.MaxFlow-edge.Residual > 0)
                {
                    msaglEdge.Attr.Color = Color.Orange;
                }
            }
        }

        return msaglGraph;
    }
}
